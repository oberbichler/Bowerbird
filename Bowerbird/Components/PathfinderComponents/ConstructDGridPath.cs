using Bowerbird.Curvature;
using Bowerbird.Parameters;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace Bowerbird.Components.PathfinderComponents;

public class ConstructDGridPath : GH_Component
{
    public ConstructDGridPath() 
        : base("BB DGrid Path", "DGrid", "Create a deformation grid (DGrid) path configuration.", "Bowerbird", "Paths")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddSurfaceParameter("Undeformed Surface", "S", "Reference undeformed shell surface", GH_ParamAccess.item);
        pManager.AddSurfaceParameter("Deformed Surface", "s", "Actual deformed shell surface", GH_ParamAccess.item);
        pManager.AddNumberParameter("Youngs Modulus", "E", "Elastic modulus", GH_ParamAccess.item);
        pManager.AddNumberParameter("Poissons Ratio", "v", "Poisson ratio", GH_ParamAccess.item);
        pManager.AddNumberParameter("Thickness", "t", "Shell thickness", GH_ParamAccess.item);
        
        var dIndex = pManager.AddIntegerParameter("Direction", "D", "Trace directions (1 = First, 2 = Second, 3 = Both)", GH_ParamAccess.item, 3);
        pManager[dIndex].AddNamedValues<Bowerbird.Curvature.Path.Types>();
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new PathParameter(), "Path Type", "T", "The configured DGrid path", GH_ParamAccess.item);
    }

    public override void AddedToDocument(GH_Document document)
    {
        base.AddedToDocument(document);
        Params.Input[5].SetInputValueList<Bowerbird.Curvature.Path.Types>();
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var refSurface = default(Surface);
        var actSurface = default(Surface);
        var youngsModulus = default(double);
        var poissonsRatio = default(double);
        var thickness = default(double);
        var directionValue = default(int);

        if (!DA.GetData(0, ref refSurface)) return;
        if (!DA.GetData(1, ref actSurface)) return;
        if (!DA.GetData(2, ref youngsModulus)) return;
        if (!DA.GetData(3, ref poissonsRatio)) return;
        if (!DA.GetData(4, ref thickness)) return;
        if (!DA.GetData(5, ref directionValue)) return;

        if (refSurface == null || actSurface == null) return;

        var direction = (Bowerbird.Curvature.Path.Types)directionValue;

        var path = DGridPath.Create(refSurface.ToNurbsSurface(), actSurface.ToNurbsSurface(), youngsModulus, poissonsRatio, thickness, direction);

        DA.SetData(0, new Bowerbird.Types.GH_Path(path));
    }

    protected override System.Drawing.Bitmap? Icon => null;

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public override Guid ComponentGuid => new("{FD063786-F798-485D-8A53-97DE6B890327}");
}
