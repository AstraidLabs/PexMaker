using System;
using System.Linq;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

public sealed partial class PexMakerEngine
{
    /// <summary>
    /// Evaluates a project for layout safety and print quality risks.
    /// </summary>
    public Task<EvaluationReport> EvaluateAsync(PexProject project, ExportRequest? request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(project);
        cancellationToken.ThrowIfCancellationRequested();

        var validation = ValidateProject(project, out var normalizedProject);
        if (!validation.IsValid)
        {
            var validationIssues = validation.Issues
                .Select(issue => new EvaluationIssue(
                    $"Validation.{issue.Code}",
                    issue.Severity == ValidationSeverity.Warning ? EvaluationSeverity.Warning : EvaluationSeverity.Error,
                    issue.Message,
                    issue.Field,
                    "Resolve validation issues before running evaluation."))
                .ToArray();

            var report = new EvaluationReport(validationIssues, Array.Empty<EvaluationMetric>(), false, false);
            return Task.FromResult(report);
        }

        var deck = BuildSequentialDeck(normalizedProject);
        var metrics = LayoutCalculator.Calculate(normalizedProject.Layout);
        var plan = BuildLayoutPlan(normalizedProject, deck, metrics);

        var context = new EvaluationContext(normalizedProject, plan, metrics, request);
        var engine = new EvaluationEngine();
        var result = engine.Evaluate(context);

        return Task.FromResult(result);
    }
}
