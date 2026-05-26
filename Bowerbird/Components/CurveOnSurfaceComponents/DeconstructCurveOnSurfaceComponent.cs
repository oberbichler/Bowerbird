using Bowerbird.Curvature;
using Bowerbird.Parameters;
using Bowerbird.Types;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace Bowerbird.Components.CurveOnSurfaceComponents;

public class DeconstructCurveOnSurfaceComponent : GH_Component
{
    public DeconstructCurveOnSurfaceComponent() 
        : base("BB Deconstruct CurveOnSurface", "Deconstruct", "Deconstruct an embedded curve into its components.", "Bowerbird", "Curve on Surface")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new CurveOnSurfaceParameter(), "Curve on Surface", "C", "Embedded curve to deconstruct", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddSurfaceParameter("Surface", "S", "Surface on which the curve is embedded", GH_ParamAccess.item);
        pManager.AddCurveParameter("Curve", "C", "Parameter curve", GH_ParamAccess.item);
        pManager.AddCurveParameter("Approximation", "A", "Approximation of the embedded curve as ordinary Rhino curve", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var ghCurveOnSurface = default(GH_CurveOnSurface);
        if (!DA.GetData(0, ref ghCurveOnSurface)) return;
        if (ghCurveOnSurface == null || ghCurveOnSurface.Value == null) return;

        var curveOnSurface = ghCurveOnSurface.Value;

        if (curveOnSurface is CurveOnSurface cos)
        {
            DA.SetData(0, cos.Surface);
            DA.SetData(1, cos.Curve);
        }
        else
        {
            DA.SetData(0, null);
            DA.SetData(1, null);
        }

        DA.SetData(2, curveOnSurface.ToCurve(DocumentTolerance()));
    }

    protected override System.Drawing.Bitmap? Icon => Bowerbird.Properties.Resources.icon_curve_on_surface_deconstruct;

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public override Guid ComponentGuid => new("{27762E37-A208-4C07-BA02-DA650F7052B1}");
}
