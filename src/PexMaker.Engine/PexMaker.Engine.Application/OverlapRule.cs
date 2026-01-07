using System;
using System.Collections.Generic;

namespace PexMaker.Engine.Application;

internal sealed class OverlapRule : IEvaluationRule
{
    public void Evaluate(EvaluationContext context, ICollection<EvaluationIssue> issues, ICollection<EvaluationMetric> metrics)
    {
        var minSpacing = Math.Max(1, context.GutterPx * 0.25);

        foreach (var page in context.RelevantPages)
        {
            var placements = page.Placements;
            for (var i = 0; i < placements.Count; i++)
            {
                var a = placements[i];
                for (var j = i + 1; j < placements.Count; j++)
                {
                    var b = placements[j];

                    var overlapWidth = Math.Min(a.X + a.Width, b.X + b.Width) - Math.Max(a.X, b.X);
                    var overlapHeight = Math.Min(a.Y + a.Height, b.Y + b.Height) - Math.Max(a.Y, b.Y);

                    if (overlapWidth > 0 && overlapHeight > 0)
                    {
                        issues.Add(new EvaluationIssue(
                            EvaluationCodes.CardOverlap,
                            EvaluationSeverity.Error,
                            "Card placements overlap on the page.",
                            $"Pages[{page.PageNumber}].{page.Side}.Cards[{i}..{j}]",
                            "Increase gutter or reduce card size to eliminate overlap."));
                        continue;
                    }

                    var gapX = Math.Max(0, Math.Max(b.X - (a.X + a.Width), a.X - (b.X + b.Width)));
                    var gapY = Math.Max(0, Math.Max(b.Y - (a.Y + a.Height), a.Y - (b.Y + b.Height)));
                    var distance = Math.Sqrt((gapX * gapX) + (gapY * gapY));

                    if (distance > 0 && distance < minSpacing)
                    {
                        issues.Add(new EvaluationIssue(
                            EvaluationCodes.CardsTooClose,
                            EvaluationSeverity.Warning,
                            "Card placements are very close to each other.",
                            $"Pages[{page.PageNumber}].{page.Side}.Cards[{i}..{j}]",
                            "Increase gutter spacing to reduce cutting risk."));
                    }
                }
            }
        }
    }
}
