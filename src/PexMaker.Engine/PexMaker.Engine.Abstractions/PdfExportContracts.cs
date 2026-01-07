using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Abstractions;

public sealed class PdfExportRequest
{
    public required string OutputPath { get; init; }

    public double PageWidthMm { get; init; }

    public double PageHeightMm { get; init; }

    public required IReadOnlyList<byte[]> Pages { get; init; }

    public IProgress<EngineProgress>? Progress { get; init; }
}

public sealed record PdfExportResult(string File, ProjectValidationResult ValidationResult)
{
    public bool IsSuccess => ValidationResult.IsValid;
}

public interface IPdfDocumentExporter
{
    Task<PdfExportResult> ExportAsync(PdfExportRequest request, CancellationToken cancellationToken);
}
