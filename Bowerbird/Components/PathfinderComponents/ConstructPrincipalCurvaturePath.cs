using Bowerbird.Curvature;
using Bowerbird.Parameters;
using Grasshopper.Kernel;
using System;

namespace Bowerbird.Components.PathfinderComponents;

public class ConstructPrincipalCurvaturePath : GH_Component
{
    public ConstructPrincipalCurvaturePath() 
        : base("BB Principal Curvature Path", "BBk", "Create a principal curvature path configuration.", "Bowerbird", "Paths")
    {
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddNumberParameter("Angle", "A", "Angle offset to rotate direction", GH_ParamAccess.item, 0.0);
        
        var dIndex = pManager.AddIntegerParameter("Direction", "D", "Trace directions (1 = First, 2 = Second, 3 = Both)", GH_ParamAccess.item, 3);
        pManager[dIndex].AddNamedValues<Bowerbird.Curvature.Path.Types>();
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddParameter(new PathParameter(), "Path Type", "T", "The configured principal curvature path", GH_ParamAccess.item);
    }

    public override void AddedToDocument(GH_Document document)
    {
        base.AddedToDocument(document);
        Params.Input[1].SetInputValueList<Bowerbird.Curvature.Path.Types>(); // FIXED: index was 2 (out of range bug in original code)
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var angle = default(double);
        var directionValue = default(int);

        if (!DA.GetData(0, ref angle)) return;
        if (!DA.GetData(1, ref directionValue)) return;

        var direction = (Bowerbird.Curvature.Path.Types)directionValue;

        var path = PrincipalCurvaturePath.Create(angle, direction);

        DA.SetData(0, new Bowerbird.Types.GH_Path(path));
    }

    protected override System.Drawing.Bitmap? Icon => Bowerbird.Properties.Resources.icon_principal_curvature_path;

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public override Guid ComponentGuid => new("{CF603E5A-B724-46B5-A242-C40429F89B62}");
}
