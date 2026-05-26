using Bowerbird.Curvature;
using Bowerbird.Parameters;
using Bowerbird.Types;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace Bowerbird.Components.CurveOnSurfaceComponents;

public class CurvatureComponent : GH_Component
{
    public CurvatureComponent() 
        : base("BB Curvature CurveOnSurface", "κ", "Evaluate the curvature of an embedded curve at a specified parameter.", "Bowerbird", "Curve on Surface")
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
        pManager.AddVectorParameter("Curvature", "K", "Curvature vector at {t}", GH_ParamAccess.item);
        pManager.AddCircleParameter("Osculating Circle", "C", "Curvature circle at {t}", GH_ParamAccess.item);
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
        var tangent = curveOnSurface.TangentAt(t);
        var curvature = curveOnSurface.CurvatureAt(t);
        
        var circle = curvature.Length < 1e-10 
            ? new Circle(point, 0.0) 
            : new Circle(new Plane(point + curvature / (curvature.Length * curvature.Length), tangent, curvature), 1.0 / curvature.Length);

        DA.SetData(0, point);
        DA.SetData(1, curvature);
        DA.SetData(2, circle);
    }

    protected override System.Drawing.Bitmap? Icon => Bowerbird.Properties.Resources.icon_curve_on_surface_curvature;

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public override Guid ComponentGuid => new("{99E562F5-DAE1-4430-94CC-2690511F0260}");
}
