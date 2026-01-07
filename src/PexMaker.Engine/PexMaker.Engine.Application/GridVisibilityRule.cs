using System.Collections.Generic;

namespace PexMaker.Engine.Application;

internal sealed class GridVisibilityRule : IEvaluationRule
{
    private const double NearEdgeThresholdMm = 1.0;
    private const double MinCardSizeMm = 20.0;

    public void Evaluate(EvaluationContext context, ICollection<EvaluationIssue> issues, ICollection<EvaluationMetric> metrics)
    {
        var freeWidth = context.Metrics.UsableWidthMm - context.Metrics.GridWidthMm;
        var freeHeight = context.Metrics.UsableHeightMm - context.Metrics.GridHeightMm;

        if (context.Layout.Alignment == PexMaker.Engine.Domain.GridAlignment.TopLeft
            && (freeWidth <= NearEdgeThresholdMm || freeHeight <= NearEdgeThresholdMm))
        {
            issues.Add(new EvaluationIssue(
                EvaluationCodes.GridNearEdge,
                EvaluationSeverity.Warning,
                "Grid fits tightly within the usable area and may touch page margins.",
                nameof(PexMaker.Engine.Domain.LayoutOptions.Alignment),
                "Consider centering the grid or increasing margins for safer trimming."));
        }

        if (context.Metrics.CardWidthMm < MinCardSizeMm || context.Metrics.CardHeightMm < MinCardSizeMm)
        {
            issues.Add(new EvaluationIssue(
                EvaluationCodes.CardTooSmall,
                EvaluationSeverity.Warning,
                "Card size is very small for print handling.",
                nameof(PexMaker.Engine.Domain.LayoutOptions.CardWidth),
                "Increase card dimensions to improve handling and print fidelity."));
        }
    }
}
