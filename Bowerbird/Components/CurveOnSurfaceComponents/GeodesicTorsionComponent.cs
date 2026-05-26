using Bowerbird.Curvature;
using Bowerbird.Parameters;
using Bowerbird.Types;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace Bowerbird.Components.CurveOnSurfaceComponents;

public class GeodesicTorsionComponent : GH_Component
{
    public GeodesicTorsionComponent() 
        : base("BB Geodesic Torsion CurveOnSurface", "τg", "Evaluate the geodesic torsion of an embedded curve at a specified parameter.", "Bowerbird", "Curve on Surface")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new CurveOnSurfaceParameter(), "Curve on Surface", "C", "Embedded curve to evaluate", GH_ParamAccess.item);
        pManager.AddNumberParameter("Parameter", "t", "Parameter on curve domain to evaluate", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddPointParameter("Point", "P", "Point on curve at {t}", GH_ParamAccess.item);
        pManager.AddVectorParameter("Torsion", "T", "Torsion vector at {t}", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var ghCurveOnSurface = default(GH_CurveOnSurface);
        var t = default(double);

        if (!DA.GetData(0, ref ghCurveOnSurface)) return;
        if (!DA.GetData(1, ref t)) return;
        if (ghCurveOnSurface == null || ghCurveOnSurface.Value == null) return;

        var curveOnSurface = ghCurveOnSurface.Value;
        var point = curveOnSurface.PointAt(t);
        var curvature = curveOnSurface.GeodesicTorsionAt(t);

        DA.SetData(0, point);
        DA.SetData(1, curvature);
    }

    protected override System.Drawing.Bitmap? Icon => Bowerbird.Properties.Resources.icon_curve_on_surface_geodesic_torsion;

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public override Guid ComponentGuid => new("{2EF18881-820F-4A80-B767-F78930BA72F6}");
}
