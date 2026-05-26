using Bowerbird.Curvature;
using Bowerbird.Parameters;
using Grasshopper.Kernel;
using System;

namespace Bowerbird.Components.PathfinderComponents;

public class ConstructGeodesicTorsionPath : GH_Component
{
    public ConstructGeodesicTorsionPath() 
        : base("BB Geodesic Torsion Path", "BBτg", "Follow the surface along a specified geodesic torsion.", "Bowerbird", "Paths")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Value", "V", "Desired geodesic torsion. Choosing value=0 returns principal curvature paths.", GH_ParamAccess.item, 0.0);
        pManager.AddNumberParameter("Angle", "A", "Optional angle to rotate the search direction", GH_ParamAccess.item, 0.0);
        
        var dIndex = pManager.AddIntegerParameter("Direction", "D", "Trace directions (1 = First, 2 = Second, 3 = Both)", GH_ParamAccess.item, 3);
        pManager[dIndex].AddNamedValues<Bowerbird.Curvature.Path.Types>();
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new PathParameter(), "Path Type", "T", "The configured geodesic torsion path", GH_ParamAccess.item);
    }

    public override void AddedToDocument(GH_Document document)
    {
        base.AddedToDocument(document);
        Params.Input[2].SetInputValueList<Bowerbird.Curvature.Path.Types>();
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var value = default(double);
        var angle = default(double);
        var directionValue = default(int);

        if (!DA.GetData(0, ref value)) return;
        if (!DA.GetData(1, ref angle)) return;
        if (!DA.GetData(2, ref directionValue)) return;

        var direction = (Bowerbird.Curvature.Path.Types)directionValue;

        var path = GeodesicTorsionPath.Create(value, angle, direction);

        DA.SetData(0, new Bowerbird.Types.GH_Path(path));
    }

    protected override System.Drawing.Bitmap? Icon => Bowerbird.Properties.Resources.icon_principal_path;

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public override Guid ComponentGuid => new("{E540E209-EF00-4E66-9460-CC62C1D394AB}");
}
