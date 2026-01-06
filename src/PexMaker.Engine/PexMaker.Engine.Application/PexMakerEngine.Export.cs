using System.Linq;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed partial class PexMakerEngine
{
    public async Task<ExportResult> ExportAsync(PexProject project, ExportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var validation = await ValidateAsync(project, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return new ExportResult(false, Array.Empty<string>(), validation);
        }

        if (project.BackImage is null)
        {
            var error = new ValidationError(EngineErrorCode.MissingBackImage, "Back image is required", nameof(project.BackImage));
            var invalidResult = new ProjectValidationResult(new[] { error }, validation.Warnings);
            return new ExportResult(false, Array.Empty<string>(), invalidResult);
        }

        var deck = await BuildDeckAsync(project, request.Seed, cancellationToken).ConfigureAwait(false);
        var layout = BuildLayoutPlan(project, deck);

        var frontPages = (int)Math.Ceiling(deck.CardCount / (double)layout.Grid.PerPage);
        var totalSheets = layout.Pages.Count;

        var safetyErrors = new List<ValidationError>();
        if (frontPages > EngineLimits.MaxPages)
        {
            safetyErrors.Add(new ValidationError(EngineErrorCode.ExceedsPageLimit, $"Page count {frontPages} exceeds limit {EngineLimits.MaxPages}", nameof(project.Layout)));
        }

        if (totalSheets > EngineLimits.MaxOutputFiles)
        {
            safetyErrors.Add(new ValidationError(EngineErrorCode.ExceedsOutputLimit, $"Output file count {totalSheets} exceeds limit {EngineLimits.MaxOutputFiles}", nameof(request.OutputDirectory)));
        }

        if (safetyErrors.Count > 0)
        {
            var invalidResult = new ProjectValidationResult(validation.Errors.Concat(safetyErrors), validation.Warnings);
            return new ExportResult(false, Array.Empty<string>(), invalidResult);
        }

        _fileSystem.CreateDirectory(request.OutputDirectory);

        var buffersToDispose = new List<IAsyncDisposable>();
        var cardWidthPx = Units.MmToPx(project.Layout.CardWidth, project.Dpi);
        var cardHeightPx = Units.MmToPx(project.Layout.CardHeight, project.Dpi);
        var borderPx = Units.MmToPx(project.Layout.BorderThickness, project.Dpi);
        var cornerPx = Units.MmToPx(project.Layout.CornerRadius, project.Dpi);

        try
        {
            var frontCache = new Dictionary<string, IImageBuffer>(StringComparer.OrdinalIgnoreCase);

            foreach (var front in deck.Cards.DistinctBy(f => f.Path))
            {
                await using var stream = _fileSystem.OpenRead(front.Path);
                await using var decoded = await _imageDecoder.DecodeAsync(stream, cancellationToken).ConfigureAwait(false);
                var cardOptions = new CardRenderOptions(
                    cardWidthPx,
                    cardHeightPx,
                    front.FitMode,
                    MathEx.Clamp01(front.AnchorX),
                    MathEx.Clamp01(front.AnchorY),
                    project.Layout.BorderEnabled,
                    borderPx,
                    cornerPx);

                var rendered = await _imageProcessor.RenderCardAsync(decoded, cardOptions, cancellationToken).ConfigureAwait(false);
                buffersToDispose.Add(rendered);
                frontCache[front.Path] = rendered;
            }

            await using var backStream = _fileSystem.OpenRead(project.BackImage.Path);
            await using var backDecoded = await _imageDecoder.DecodeAsync(backStream, cancellationToken).ConfigureAwait(false);
            var backOptions = new CardRenderOptions(
                cardWidthPx,
                cardHeightPx,
                project.BackImage.FitMode,
                MathEx.Clamp01(project.BackImage.AnchorX),
                MathEx.Clamp01(project.BackImage.AnchorY),
                project.Layout.BorderEnabled,
                borderPx,
                cornerPx);
            var backRendered = await _imageProcessor.RenderCardAsync(backDecoded, backOptions, cancellationToken).ConfigureAwait(false);
            buffersToDispose.Add(backRendered);

            var sheetEntries = new List<SheetExportEntry>();
            foreach (var page in layout.Pages)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var placements = new List<PagePlacement>();
                foreach (var placement in page.Placements)
                {
                    var cardImage = page.Side == SheetSide.Front
                        ? frontCache[deck.Cards[placement.DeckIndex].Path]
                        : backRendered;

                    placements.Add(new PagePlacement(cardImage, placement.X, placement.Y, placement.Width, placement.Height));
                }

                var renderRequest = new PageRenderRequest(layout.PageWidthPx, layout.PageHeightPx, placements, project.Layout.CutMarks);
                var renderedPage = await _pageRenderer.ComposePageAsync(renderRequest, cancellationToken).ConfigureAwait(false);
                buffersToDispose.Add(renderedPage);

                var prefix = page.Side == SheetSide.Front ? request.FrontPrefix : request.BackPrefix;
                var fileName = Naming.PageFileName(prefix, page.PageNumber, request.Format);
                sheetEntries.Add(new SheetExportEntry(fileName, renderedPage, page.Side));
            }

            var exportRequest = new SheetExportRequest(request.OutputDirectory, request.Format, sheetEntries);
            var files = await _sheetExporter.ExportAsync(exportRequest, cancellationToken).ConfigureAwait(false);
            return new ExportResult(true, files, validation);
        }
        finally
        {
            foreach (var disposable in buffersToDispose)
            {
                await disposable.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
