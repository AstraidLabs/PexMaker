using System.Collections.Generic;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

internal sealed class DuplexSanityRule : IEvaluationRule
{
    public void Evaluate(EvaluationContext context, ICollection<EvaluationIssue> issues, ICollection<EvaluationMetric> metrics)
    {
        if (!context.IncludeBack)
        {
            return;
        }

        var duplexMode = context.Layout.DuplexMode;
        if (duplexMode == DuplexMode.None && !context.Layout.MirrorBackside)
        {
            issues.Add(new EvaluationIssue(
                EvaluationCodes.DuplexNotConfigured,
                EvaluationSeverity.Warning,
                "Back-side export is enabled but duplex mapping is disabled.",
                nameof(LayoutOptions.DuplexMode),
                "Select a duplex mode to align front and back pages."));
            return;
        }

        var normalized = duplexMode;
        if (normalized == DuplexMode.None && context.Layout.MirrorBackside)
        {
            normalized = DuplexMode.MirrorColumns;
        }

        normalized = LayoutMath.NormalizeDuplexMode(normalized, context.Layout.Orientation);

        if (normalized == DuplexMode.None)
        {
            return;
        }

        var seen = new HashSet<(int Row, int Column)>();
        for (var row = 0; row < context.Plan.Grid.Rows; row++)
        {
            for (var col = 0; col < context.Plan.Grid.Columns; col++)
            {
                var mapped = LayoutMath.MapDuplex(row, col, context.Plan.Grid.Rows, context.Plan.Grid.Columns, normalized);
                if (!seen.Add(mapped))
                {
                    issues.Add(new EvaluationIssue(
                        EvaluationCodes.DuplexMappingCollision,
                        EvaluationSeverity.Error,
                        "Duplex mapping collides: multiple front cells map to the same back cell.",
                        nameof(LayoutOptions.DuplexMode),
                        "Use a duplex mode that produces a one-to-one mapping."));
                    return;
                }
            }
        }
    }
}
