using System;
using System.Collections.Generic;

namespace PexMaker.Engine.Application;

internal sealed class OutOfBoundsRule : IEvaluationRule
{
    private const double OutsideErrorThreshold = 0.25;

    public void Evaluate(EvaluationContext context, ICollection<EvaluationIssue> issues, ICollection<EvaluationMetric> metrics)
    {
        var pageWidth = context.Plan.PageWidthPx;
        var pageHeight = context.Plan.PageHeightPx;

        foreach (var page in context.RelevantPages)
        {
            for (var index = 0; index < page.Placements.Count; index++)
            {
                var placement = page.Placements[index];
                var path = $"Pages[{page.PageNumber}].{page.Side}.Cards[{index}]";

                if (placement.Width <= 0 || placement.Height <= 0)
                {
                    issues.Add(new EvaluationIssue(
                        EvaluationCodes.InvalidRect,
                        EvaluationSeverity.Error,
                        "Card placement has a non-positive size.",
                        path,
                        "Ensure card dimensions are positive and fit on the page."));
                    continue;
                }

                var left = placement.X;
                var top = placement.Y;
                var right = placement.X + placement.Width;
                var bottom = placement.Y + placement.Height;

                if (right <= 0 || bottom <= 0 || left >= pageWidth || top >= pageHeight)
                {
                    issues.Add(new EvaluationIssue(
                        EvaluationCodes.CardOutsidePage,
                        EvaluationSeverity.Error,
                        "Card placement is fully outside the page bounds.",
                        path,
                        "Adjust margins, grid alignment, or card size so placements land on the page."));
                    continue;
                }

                var clippedLeft = Math.Max(0, left);
                var clippedTop = Math.Max(0, top);
                var clippedRight = Math.Min(pageWidth, right);
                var clippedBottom = Math.Min(pageHeight, bottom);

                var rectArea = (double)placement.Width * placement.Height;
                var visibleArea = (double)Math.Max(0, clippedRight - clippedLeft) * Math.Max(0, clippedBottom - clippedTop);
                var outsideRatio = rectArea > 0 ? 1.0 - (visibleArea / rectArea) : 1.0;

                if (outsideRatio <= 0)
                {
                    continue;
                }

                if (outsideRatio > OutsideErrorThreshold)
                {
                    issues.Add(new EvaluationIssue(
                        EvaluationCodes.CardOutsidePage,
                        EvaluationSeverity.Error,
                        "Card placement is mostly outside the page bounds.",
                        path,
                        "Reduce card size or increase usable page area so placements fit."));
                }
                else
                {
                    issues.Add(new EvaluationIssue(
                        EvaluationCodes.CardClipped,
                        EvaluationSeverity.Warning,
                        "Card placement is clipped by the page bounds.",
                        path,
                        "Increase margins or adjust grid alignment to avoid clipping."));
                }
            }
        }
    }
}
