using Bowerbird.Curvature;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace Bowerbird.Components.CurvatureComponents;

public class ComputePrincipalStress : GH_Component
{
    public ComputePrincipalStress() 
        : base("BB Principal Stress", "BB PS", "Compute the principal stresses on a deformed shell" + Bowerbird.Crafting.Util.InfoString, "Bowerbird", "Curvature")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddSurfaceParameter("Undeformed Surface", "S", "Reference shell surface", GH_ParamAccess.item);
        pManager.AddSurfaceParameter("Deformed Surface", "s", "Actual deformed shell surface", GH_ParamAccess.item);
        pManager.AddPointParameter("Parameter Point", "uv", "Point on surface parameters space", GH_ParamAccess.item);
        pManager.AddNumberParameter("Youngs Modulus", "E", "Elastic modulus of shell material", GH_ParamAccess.item, 1.0);
        pManager.AddNumberParameter("Poissons Ratio", "v", "Poisson ratio of shell material", GH_ParamAccess.item, 0.0);
        pManager.AddNumberParameter("Thickness", "t", "Thickness of shell", GH_ParamAccess.item, 1.0);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddNumberParameter("S1", "S1", "First principal stress value", GH_ParamAccess.item);
        pManager.AddNumberParameter("S2", "S2", "Second principal stress value", GH_ParamAccess.item);
        pManager.AddVectorParameter("S1 Parameter Direction", "d1", "First principal stress parameter direction vector", GH_ParamAccess.item);
        pManager.AddVectorParameter("S2 Parameter Direction", "d2", "Second principal stress parameter direction vector", GH_ParamAccess.item);
        pManager.AddVectorParameter("S1 Direction", "D1", "First principal stress 3D direction vector", GH_ParamAccess.item);
        pManager.AddVectorParameter("S2 Direction", "D2", "Second principal stress 3D direction vector", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var refSurface = default(Surface);
        var actSurface = default(Surface);
        var uv = default(Point3d);
        var youngsModulus = default(double);
        var poissonsRatio = default(double);
        var thickness = default(double);

        if (!DA.GetData(0, ref refSurface)) return;
        if (!DA.GetData(1, ref actSurface)) return;
        if (!DA.GetData(2, ref uv)) return;
        if (!DA.GetData(3, ref youngsModulus)) return;
        if (!DA.GetData(4, ref poissonsRatio)) return;
        if (!DA.GetData(5, ref thickness)) return;

        if (refSurface == null || actSurface == null) return;

        // Use our hocheffiziente value-type PrincipalStress struct instead of heap-allocated class objects!
        var ps = new PrincipalStress(refSurface, actSurface, thickness, youngsModulus, poissonsRatio);
        if (ps.Compute(uv.X, uv.Y))
        {
            DA.SetData(0, ps.S1);
            DA.SetData(1, ps.S2);
            DA.SetData(2, ps.U1);
            DA.SetData(3, ps.U2);
            DA.SetData(4, ps.D1);
            DA.SetData(5, ps.D2);
        }
    }

    protected override System.Drawing.Bitmap? Icon => null;

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public override Guid ComponentGuid => new("{393BFCE6-1A65-4FAD-8621-1BE1FF6D5EF9}");
}
