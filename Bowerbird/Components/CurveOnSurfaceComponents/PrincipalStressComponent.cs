using Bowerbird.Curvature;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace Bowerbird.Components.CurveOnSurfaceComponents;

public class PrincipalStressComponent : GH_Component
{
    public PrincipalStressComponent() 
        : base("BB Principal Stress", "σ", "Evaluate the principal stresses at a point on a deformed shell." + Bowerbird.Crafting.Util.InfoString, "Bowerbird", "Stress")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddSurfaceParameter("Reference Surface", "S", "Reference undeformed shell surface", GH_ParamAccess.item);
        pManager.AddSurfaceParameter("Actual Surface", "s", "Actual deformed shell surface", GH_ParamAccess.item);
        pManager.AddVectorParameter("Parameter UV", "UV", "Point on parameters space", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddNumberParameter("Principal Stress 1", "σ1", "First principal stress", GH_ParamAccess.item);
        pManager.AddNumberParameter("Principal Stress 2", "σ2", "Second principal stress", GH_ParamAccess.item);
        pManager.AddVectorParameter("Principal Stress Direction 1", "D1", "First principal stress direction", GH_ParamAccess.item);
        pManager.AddVectorParameter("Principal Stress Direction 2", "D2", "Second principal stress direction", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var refSurface = default(Surface);
        var actSurface = default(Surface);
        var uv = default(Vector3d);

        if (!DA.GetData(0, ref refSurface)) return;
        if (!DA.GetData(1, ref actSurface)) return;
        if (!DA.GetData(2, ref uv)) return;

        if (refSurface == null || actSurface == null) return;

        var u = uv.X;
        var v = uv.Y;

        // Use our hocheffiziente value-type PrincipalStress struct instead of heap-allocated class objects!
        var ps = new PrincipalStress(refSurface, actSurface, 1.0, 1.0, 0.0);
        ps.Compute(u, v);

        DA.SetData(0, ps.S1);
        DA.SetData(1, ps.S2);
        DA.SetData(2, ps.D1);
        DA.SetData(3, ps.D2);
    }

    protected override System.Drawing.Bitmap? Icon => Bowerbird.Properties.Resources.icon_curve_on_surface_curvature;

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public override Guid ComponentGuid => new("{F23CF650-B288-4E6C-968D-F3BCD64C64E9}");
}
