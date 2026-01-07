using System.Collections.Generic;
using System.Linq;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

internal sealed class EvaluationEngine
{
    private static readonly string[] LayoutErrorCodes =
    {
        EvaluationCodes.InvalidRect,
        EvaluationCodes.CardOutsidePage,
        EvaluationCodes.CardOverlap,
        EvaluationCodes.DuplexMappingCollision,
    };

    private static readonly string[] QualityCodes =
    {
        EvaluationCodes.LowCardResolution,
        EvaluationCodes.RotationIgnored,
        EvaluationCodes.AggressiveCropAnchor,
    };

    private readonly IReadOnlyList<IEvaluationRule> _rules;

    public EvaluationEngine()
    {
        _rules =
        [
            new OutOfBoundsRule(),
            new OverlapRule(),
            new GridVisibilityRule(),
            new DuplexSanityRule(),
            new DpiVsCardPixelsRule(),
            new FitModeCropRiskRule(),
        ];
    }

    public EvaluationReport Evaluate(EvaluationContext context)
    {
        var issues = new List<EvaluationIssue>();
        var metrics = new List<EvaluationMetric>();

        AddMetrics(context, metrics);

        foreach (var rule in _rules)
        {
            rule.Evaluate(context, issues, metrics);
        }

        var layoutErrorCodes = new HashSet<string>(LayoutErrorCodes);
        var qualityCodes = new HashSet<string>(QualityCodes);

        var hasLayoutErrors = issues.Any(issue => issue.Severity == EvaluationSeverity.Error && layoutErrorCodes.Contains(issue.Code));
        var qualityErrors = issues.Count(issue => issue.Severity == EvaluationSeverity.Error && qualityCodes.Contains(issue.Code));
        var qualityWarnings = issues.Count(issue => issue.Severity == EvaluationSeverity.Warning && qualityCodes.Contains(issue.Code));

        var isLayoutSafe = !hasLayoutErrors;
        var isQualityOk = qualityErrors == 0 && qualityWarnings <= 3;

        return new EvaluationReport(issues, metrics, isLayoutSafe, isQualityOk);
    }

    private static void AddMetrics(EvaluationContext context, ICollection<EvaluationMetric> metrics)
    {
        var pagesFront = context.Plan.Pages.Count(page => page.Side == SheetSide.Front);
        var pagesBack = context.Plan.Pages.Count(page => page.Side == SheetSide.Back);

        metrics.Add(new EvaluationMetric("TotalPagesFront", pagesFront.ToString()));
        metrics.Add(new EvaluationMetric("TotalPagesBack", pagesBack.ToString()));
        metrics.Add(new EvaluationMetric("CardsPerPage", context.Plan.Grid.PerPage.ToString()));
        metrics.Add(new EvaluationMetric("CardSizeMm", $"{context.Metrics.CardWidthMm:0.##} x {context.Metrics.CardHeightMm:0.##}", "mm"));
        metrics.Add(new EvaluationMetric("CardSizePx", $"{context.CardWidthPx} x {context.CardHeightPx}", "px"));
        metrics.Add(new EvaluationMetric("PageSizeMm", $"{context.Metrics.PageWidthMm:0.##} x {context.Metrics.PageHeightMm:0.##}", "mm"));
        metrics.Add(new EvaluationMetric("PageSizePx", $"{context.Plan.PageWidthPx} x {context.Plan.PageHeightPx}", "px"));
        metrics.Add(new EvaluationMetric("EffectiveDpi", context.Dpi.ToString(), "dpi"));
    }
}

internal static class EvaluationCodes
{
    public const string InvalidRect = "InvalidRect";
    public const string CardOutsidePage = "CardOutsidePage";
    public const string CardClipped = "CardClipped";
    public const string CardOverlap = "CardOverlap";
    public const string CardsTooClose = "CardsTooClose";
    public const string GridNearEdge = "GridNearEdge";
    public const string CardTooSmall = "CardTooSmall";
    public const string DuplexNotConfigured = "DuplexNotConfigured";
    public const string DuplexMappingCollision = "DuplexMappingCollision";
    public const string LowCardResolution = "LowCardResolution";
    public const string RotationIgnored = "RotationIgnored";
    public const string AggressiveCropAnchor = "AggressiveCropAnchor";
}

internal interface IEvaluationRule
{
    void Evaluate(EvaluationContext context, ICollection<EvaluationIssue> issues, ICollection<EvaluationMetric> metrics);
}
