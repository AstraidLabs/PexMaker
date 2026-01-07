using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PexMaker.Engine.Abstractions;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Pdf;

public sealed class PdfSharpDocumentExporter : IPdfDocumentExporter
{
    private readonly IFileSystem _fileSystem;

    public PdfSharpDocumentExporter(IFileSystem fileSystem)
    {
        _fileSystem = Guard.NotNull(fileSystem, nameof(fileSystem));
    }

    public Task<PdfExportResult> ExportAsync(PdfExportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var totalPages = request.Pages.Count;
            using var document = new PdfDocument();

            for (var i = 0; i < totalPages; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var page = document.AddPage();
                page.Width = XUnit.FromMillimeter(request.PageWidthMm);
                page.Height = XUnit.FromMillimeter(request.PageHeightMm);

                using var graphics = XGraphics.FromPdfPage(page);
                using var image = XImage.FromStream(() => new MemoryStream(request.Pages[i]));
                graphics.DrawImage(image, 0, 0, page.Width, page.Height);

                request.Progress?.Report(new EngineProgress("Pdf:AddingPages", i + 1, totalPages));
            }

            request.Progress?.Report(new EngineProgress("Pdf:Saving", 0, 1));

            var directory = Path.GetDirectoryName(request.OutputPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                _fileSystem.CreateDirectory(directory);
            }

            using var stream = _fileSystem.OpenWrite(request.OutputPath);
            document.Save(stream);

            return Task.FromResult(new PdfExportResult(request.OutputPath, ProjectValidationResult.Success()));
        }
        catch (OperationCanceledException)
        {
            var error = new ValidationError(EngineErrorCode.ExportCanceled, "PDF export was canceled.", nameof(PdfExportRequest));
            return Task.FromResult(new PdfExportResult(request.OutputPath, ProjectValidationResult.Failure(new[] { error })));
        }
        catch (Exception ex)
        {
            var error = new ValidationError(EngineErrorCode.Unknown, $"PDF export failed: {ex.Message}", nameof(PdfExportRequest));
            return Task.FromResult(new PdfExportResult(request.OutputPath, ProjectValidationResult.Failure(new[] { error })));
        }
    }
}
