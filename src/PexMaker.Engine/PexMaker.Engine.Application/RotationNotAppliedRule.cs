using System;
using System.Collections.Generic;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

internal sealed class RotationNotAppliedRule : IEvaluationRule
{
    public void Evaluate(EvaluationContext context, ICollection<EvaluationIssue> issues, ICollection<EvaluationMetric> metrics)
    {
        for (var i = 0; i < context.Project.FrontImages.Count; i++)
        {
            var image = context.Project.FrontImages[i];
            if (Math.Abs(image.RotationDegrees) <= 0.01)
            {
                continue;
            }

            issues.Add(new EvaluationIssue(
                EvaluationCodes.RotationIgnored,
                EvaluationSeverity.Warning,
                "Image rotation is configured but not applied during rendering.",
                $"{nameof(PexProject.FrontImages)}[{i}].{nameof(ImageRef.RotationDegrees)}",
                "Set rotation to 0 or implement rotation handling in the renderer."));
        }

        if (context.Project.BackImage is not null && Math.Abs(context.Project.BackImage.RotationDegrees) > 0.01)
        {
            issues.Add(new EvaluationIssue(
                EvaluationCodes.RotationIgnored,
                EvaluationSeverity.Warning,
                "Back image rotation is configured but not applied during rendering.",
                $"{nameof(PexProject.BackImage)}.{nameof(ImageRef.RotationDegrees)}",
                "Set rotation to 0 or implement rotation handling in the renderer."));
        }
    }
}
