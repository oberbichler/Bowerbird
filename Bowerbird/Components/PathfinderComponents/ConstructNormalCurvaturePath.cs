using Bowerbird.Curvature;
using Bowerbird.Parameters;
using Grasshopper.Kernel;
using System;

namespace Bowerbird.Components.PathfinderComponents;

public class ConstructNormalCurvaturePath : GH_Component
{
    public ConstructNormalCurvaturePath() 
        : base("BB Normal Curvature Path", "BBκn", "Follow the surface along a specified normal curvature.", "Bowerbird", "Paths")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Value", "V", "Desired normal curvature. Choosing value=0 returns asymptotic curves.", GH_ParamAccess.item, 0.0);
        pManager.AddNumberParameter("Angle", "A", "Optional angle to rotate the search direction", GH_ParamAccess.item, 0.0);
        
        var dIndex = pManager.AddIntegerParameter("Direction", "D", "Trace directions (1 = First, 2 = Second, 3 = Both)", GH_ParamAccess.item, 3);
        pManager[dIndex].AddNamedValues<Bowerbird.Curvature.Path.Types>();
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new PathParameter(), "Path Type", "T", "The configured normal curvature path", GH_ParamAccess.item);
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

        var path = NormalCurvaturePath.Create(value, angle, direction);

        DA.SetData(0, new Bowerbird.Types.GH_Path(path));
    }

    protected override System.Drawing.Bitmap? Icon => Bowerbird.Properties.Resources.icon_asymptotic_path;

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public override Guid ComponentGuid => new("{F3B20CCE-C077-480E-A03D-48845AF2EF6F}");
}
