using Bowerbird.Curvature;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace Bowerbird.Components.CurvatureComponents;

public class ComputePrincipalCurvature : GH_Component
{
    public ComputePrincipalCurvature() 
        : base("BB Principal Curvature", "BBCrv", "Compute the principal curvatures on a surface" + Bowerbird.Crafting.Util.InfoString, "Bowerbird", "Curvature")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddSurfaceParameter("Surface", "S", "Surface to evaluate", GH_ParamAccess.item);
        pManager.AddPointParameter("Parameter Point", "uv", "Point on the surface parameter space", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddNumberParameter("K1", "K1", "First principal curvature value", GH_ParamAccess.item);
        pManager.AddNumberParameter("K2", "K2", "Second principal curvature value", GH_ParamAccess.item);
        pManager.AddVectorParameter("K1 Parameter Direction", "d1", "First principal curvature parameter direction vector", GH_ParamAccess.item);
        pManager.AddVectorParameter("K2 Parameter Direction", "d2", "Second principal curvature parameter direction vector", GH_ParamAccess.item);
        pManager.AddVectorParameter("K1 Direction", "D1", "First principal curvature 3D direction vector", GH_ParamAccess.item);
        pManager.AddVectorParameter("K2 Direction", "D2", "Second principal curvature 3D direction vector", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var surface = default(Surface);
        var uv = default(Point3d);

        if (!DA.GetData(0, ref surface)) return;
        if (!DA.GetData(1, ref uv)) return;
        if (surface == null) return;

        var crv = new PrincipalCurvature();
        if (crv.Compute(surface, uv.X, uv.Y))
        {
            DA.SetData(0, crv.K1);
            DA.SetData(1, crv.K2);
            DA.SetData(2, crv.U1);
            DA.SetData(3, crv.U2);
            DA.SetData(4, crv.D1);
            DA.SetData(5, crv.D2);
        }
    }

    protected override System.Drawing.Bitmap? Icon => null;

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public override Guid ComponentGuid => new("{5300DCE5-F233-4CDA-BDC3-56DB437DA1EF}");
}
