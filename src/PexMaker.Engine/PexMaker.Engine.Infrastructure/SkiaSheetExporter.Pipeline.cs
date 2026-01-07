using System.Threading.Channels;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Infrastructure;

internal sealed partial class SkiaSheetExporter
{
    internal readonly record struct PageJob(int PageIndex, bool IsBackSide, string OutputPath, LayoutPage PageLayout);

    internal sealed record EncodedPage(int PageIndex, bool IsBackSide, string OutputPath, byte[] Bytes);

    private async Task<SheetExportResult> ExportInternalAsync(SheetExportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var maxBufferedPages = Math.Max(1, request.MaxBufferedPages);
        var maxBufferedCards = Math.Max(1, request.MaxBufferedCards);
        var maxCacheItems = Math.Max(1, request.MaxCacheItems);
        var maxEstimatedBytes = request.MaxEstimatedWorkingSetBytes;

        var estimatedBytes = EstimateWorkingSetBytes(request, maxBufferedPages, maxBufferedCards);
        if (estimatedBytes > maxEstimatedBytes)
        {
            var error = new ValidationError(
                EngineErrorCode.WorkingSetTooLarge,
                $"Estimated working set {estimatedBytes:N0} bytes exceeds limit {maxEstimatedBytes:N0} bytes.",
                nameof(SheetExportRequest.MaxEstimatedWorkingSetBytes));
            return new SheetExportResult(Array.Empty<string>(), ProjectValidationResult.Failure(new[] { error }));
        }

        using var caches = new ExportCaches(maxCacheItems);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var linkedToken = linkedSource.Token;
        var totalPages = CountExportPages(request);

        var pageJobs = Channel.CreateBounded<PageJob>(new BoundedChannelOptions(maxBufferedPages)
        {
            SingleWriter = true,
            SingleReader = false,
            FullMode = BoundedChannelFullMode.Wait,
        });

        var encodedPages = Channel.CreateBounded<EncodedPage>(new BoundedChannelOptions(1)
        {
            SingleWriter = false,
            SingleReader = true,
            FullMode = BoundedChannelFullMode.Wait,
        });

        var files = new List<string>();

        var producerTask = ProducePageJobsAsync(request, pageJobs.Writer, linkedToken);
        var rendererTask = RenderPagesAsync(request, pageJobs.Reader, encodedPages.Writer, caches, totalPages, linkedToken);
        var writerTask = WritePagesAsync(request, encodedPages.Reader, files, totalPages, linkedToken);

        try
        {
            await Task.WhenAll(producerTask, rendererTask, writerTask).ConfigureAwait(false);
            return new SheetExportResult(files, ProjectValidationResult.Success());
        }
        catch (OperationCanceledException)
        {
            var error = new ValidationError(EngineErrorCode.ExportCanceled, "Export was canceled.", nameof(SheetExportRequest));
            return new SheetExportResult(files, ProjectValidationResult.Failure(new[] { error }));
        }
        catch (Exception ex)
        {
            var error = new ValidationError(EngineErrorCode.Unknown, $"Export failed: {ex.Message}", nameof(SheetExportRequest));
            return new SheetExportResult(files, ProjectValidationResult.Failure(new[] { error }));
        }
        finally
        {
            linkedSource.Cancel();
            pageJobs.Writer.TryComplete();
            encodedPages.Writer.TryComplete();
        }
    }

    private static int CountExportPages(SheetExportRequest request)
    {
        return request.Layout.Pages.Count(page => (page.Side == SheetSide.Front && request.IncludeFront)
            || (page.Side == SheetSide.Back && request.IncludeBack));
    }

    private static long EstimateWorkingSetBytes(SheetExportRequest request, int maxBufferedPages, int maxBufferedCards)
    {
        var bytesPerPage = (long)request.Layout.PageWidthPx * request.Layout.PageHeightPx * 4;
        var renderWidth = request.CardWidthPx + (request.BleedPx * 2);
        var renderHeight = request.CardHeightPx + (request.BleedPx * 2);
        var bytesPerCard = (long)renderWidth * renderHeight * 4;
        return (bytesPerPage * maxBufferedPages) + (bytesPerCard * maxBufferedCards);
    }

    private async Task ProducePageJobsAsync(SheetExportRequest request, ChannelWriter<PageJob> writer, CancellationToken cancellationToken)
    {
        try
        {
            var outputDirectory = request.OutputDirectory;
            var frontPrefix = request.NamingPrefixFront;
            var backPrefix = request.NamingPrefixBack;

            foreach (var page in request.Layout.Pages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (page.Side == SheetSide.Front && !request.IncludeFront)
                {
                    continue;
                }

                if (page.Side == SheetSide.Back && !request.IncludeBack)
                {
                    continue;
                }

                var prefix = page.Side == SheetSide.Front ? frontPrefix : backPrefix;
                var fileName = PexMaker.Engine.Application.Naming.PageFileName(prefix, page.PageNumber, request.Format);
                var outputPath = Path.Combine(outputDirectory, fileName);

                var job = new PageJob(page.PageNumber, page.Side == SheetSide.Back, outputPath, page);
                await writer.WriteAsync(job, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private async Task RenderPagesAsync(
        SheetExportRequest request,
        ChannelReader<PageJob> reader,
        ChannelWriter<EncodedPage> writer,
        ExportCaches caches,
        int totalPages,
        CancellationToken cancellationToken)
    {
        try
        {
            var renderedCount = 0;
            await foreach (var job in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                var rasterizerRequest = CreateRasterizerRequest(request, job.PageLayout);
                var rasterized = await _pageRasterizer.RenderPageAsync(rasterizerRequest, caches, cancellationToken).ConfigureAwait(false);
                var encoded = new EncodedPage(job.PageIndex, job.IsBackSide, job.OutputPath, rasterized.PngBytes);
                await writer.WriteAsync(encoded, cancellationToken).ConfigureAwait(false);
                if (request.Progress is not null && totalPages > 0)
                {
                    renderedCount++;
                    request.Progress.Report(new EngineProgress("RenderPage", renderedCount, totalPages));
                }
            }
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private async Task WritePagesAsync(
        SheetExportRequest request,
        ChannelReader<EncodedPage> reader,
        ICollection<string> files,
        int totalPages,
        CancellationToken cancellationToken)
    {
        var writtenCount = 0;
        await foreach (var page in reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var directory = Path.GetDirectoryName(page.OutputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                _fileSystem.CreateDirectory(directory);
            }

            await using var stream = _fileSystem.OpenWrite(page.OutputPath);
            await stream.WriteAsync(page.Bytes, cancellationToken).ConfigureAwait(false);
            files.Add(page.OutputPath);
            if (request.Progress is not null && totalPages > 0)
            {
                writtenCount++;
                request.Progress.Report(new EngineProgress("WritePage", writtenCount, totalPages));
            }
        }
    }

    private static PageRasterizerRequest CreateRasterizerRequest(SheetExportRequest request, LayoutPage page)
    {
        return new PageRasterizerRequest
        {
            Layout = request.Layout,
            Page = page,
            Cards = request.Cards,
            BackImage = request.BackImage,
            IncludeCutMarks = request.IncludeCutMarks,
            CutMarksPerCard = request.CutMarksPerCard,
            CutMarkLengthPx = request.CutMarkLengthPx,
            CutMarkThicknessPx = request.CutMarkThicknessPx,
            CutMarkOffsetPx = request.CutMarkOffsetPx,
            BorderEnabled = request.BorderEnabled,
            BorderThicknessPx = request.BorderThicknessPx,
            CornerRadiusPx = request.CornerRadiusPx,
            CardWidthPx = request.CardWidthPx,
            CardHeightPx = request.CardHeightPx,
            BleedPx = request.BleedPx,
            SafeAreaPx = request.SafeAreaPx,
            ShowSafeAreaOverlay = request.ShowSafeAreaOverlay,
            SafeAreaOverlayThicknessPx = request.SafeAreaOverlayThicknessPx,
            IncludeRegistrationMarks = request.IncludeRegistrationMarks,
            RegistrationMarkPlacement = request.RegistrationMarkPlacement,
            RegistrationMarkSizePx = request.RegistrationMarkSizePx,
            RegistrationMarkThicknessPx = request.RegistrationMarkThicknessPx,
            RegistrationMarkOffsetPx = request.RegistrationMarkOffsetPx,
            EnableParallelism = request.EnableParallelism,
            MaxDegreeOfParallelism = request.MaxDegreeOfParallelism,
            MaxCacheItems = request.MaxCacheItems,
        };
    }
}
