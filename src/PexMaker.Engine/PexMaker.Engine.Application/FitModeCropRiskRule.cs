using System.Collections.Generic;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

internal sealed class FitModeCropRiskRule : IEvaluationRule
{
    private const double AnchorThreshold = 0.1;

    public void Evaluate(EvaluationContext context, ICollection<EvaluationIssue> issues, ICollection<EvaluationMetric> metrics)
    {
        for (var i = 0; i < context.Project.FrontImages.Count; i++)
        {
            var image = context.Project.FrontImages[i];
            if (IsAggressiveAnchor(image))
            {
                issues.Add(new EvaluationIssue(
                    EvaluationCodes.AggressiveCropAnchor,
                    EvaluationSeverity.Warning,
                    "Cover fit with an extreme crop anchor may cut off important content.",
                    $"{nameof(PexProject.FrontImages)}[{i}]",
                    "Use a centered anchor or switch to Contain to reduce crop risk."));
            }
        }

        if (context.Project.BackImage is not null && IsAggressiveAnchor(context.Project.BackImage))
        {
            issues.Add(new EvaluationIssue(
                EvaluationCodes.AggressiveCropAnchor,
                EvaluationSeverity.Warning,
                "Cover fit with an extreme crop anchor may cut off important content.",
                nameof(PexProject.BackImage),
                "Use a centered anchor or switch to Contain to reduce crop risk."));
        }
    }

    private static bool IsAggressiveAnchor(ImageRef image)
    {
        if (image.FitMode != FitMode.Cover)
        {
            return false;
        }

        return image.AnchorX < AnchorThreshold
            || image.AnchorX > 1 - AnchorThreshold
            || image.AnchorY < AnchorThreshold
            || image.AnchorY > 1 - AnchorThreshold;
    }
}
