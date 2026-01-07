using System.Collections.Generic;
using System.Linq;
using PexMaker.Engine.Domain;
using PexMaker.Engine.Domain.Units;

namespace PexMaker.Engine.Application;

internal sealed class EvaluationContext
{
    public EvaluationContext(PexProject project, LayoutPlan plan, LayoutMetrics metrics, ExportRequest? request)
    {
        Project = project;
        Plan = plan;
        Metrics = metrics;
        Request = request;
        Layout = project.Layout;
        Dpi = project.Dpi.Value;
        Resolution = ResolutionDpi.Create(Dpi);
        CardWidthPx = UnitConverter.ToPixels((decimal)metrics.CardWidthMm, Unit.Millimeter, Resolution, PxRounding.Round);
        CardHeightPx = UnitConverter.ToPixels((decimal)metrics.CardHeightMm, Unit.Millimeter, Resolution, PxRounding.Round);
        GutterPx = UnitConverter.ToPixels((decimal)Layout.Gutter.Value, Unit.Millimeter, Resolution, PxRounding.Round);
        BleedPx = UnitConverter.ToPixels((decimal)Layout.Bleed.Value, Unit.Millimeter, Resolution, PxRounding.Round);
        IncludeFront = request?.IncludeFront ?? true;
        IncludeBack = request?.IncludeBack ?? true;
    }

    public PexProject Project { get; }

    public LayoutPlan Plan { get; }

    public LayoutMetrics Metrics { get; }

    public ExportRequest? Request { get; }

    public LayoutOptions Layout { get; }

    public int Dpi { get; }

    public ResolutionDpi Resolution { get; }

    public int CardWidthPx { get; }

    public int CardHeightPx { get; }

    public int GutterPx { get; }

    public int BleedPx { get; }

    public bool IncludeFront { get; }

    public bool IncludeBack { get; }

    public IEnumerable<LayoutPage> RelevantPages => Plan.Pages.Where(page =>
        (page.Side == SheetSide.Front && IncludeFront)
        || (page.Side == SheetSide.Back && IncludeBack));
}
