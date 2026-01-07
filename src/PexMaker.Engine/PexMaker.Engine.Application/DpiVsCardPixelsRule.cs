using System.Collections.Generic;

namespace PexMaker.Engine.Application;

internal sealed class DpiVsCardPixelsRule : IEvaluationRule
{
    private const int MinimumCardPixels = 250;

    public void Evaluate(EvaluationContext context, ICollection<EvaluationIssue> issues, ICollection<EvaluationMetric> metrics)
    {
        if (context.CardWidthPx <= 0 || context.CardHeightPx <= 0)
        {
            return;
        }

        if (context.CardWidthPx < MinimumCardPixels || context.CardHeightPx < MinimumCardPixels)
        {
            issues.Add(new EvaluationIssue(
                EvaluationCodes.LowCardResolution,
                EvaluationSeverity.Warning,
                "Card pixel dimensions are low for the selected DPI.",
                nameof(PexMaker.Engine.Domain.PexProject.Dpi),
                "Increase DPI or card dimensions to improve print quality."));
        }
    }
}
