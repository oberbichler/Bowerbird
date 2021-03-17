using Bowerbird.Curvature;
using Bowerbird.Parameters;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Bowerbird.Components.PathfinderComponents
{
    public class ConstructPrincipalCurvaturePath : GH_Component
    {
        public ConstructPrincipalCurvaturePath() : base("BB Principal Curvature Path", "BBk", "Beta! Interface might change!", "Bowerbird", "Paths")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Angle", "A", "", GH_ParamAccess.item, 0.0);
            pManager.AddIntegerParameter("Direction", "D", "", GH_ParamAccess.item, 3);
            Utility.AddNamedValues<Path.Types>(Params.Input[1]);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new PathParameter(), "Path Type", "T", "", GH_ParamAccess.item);
        }

        protected override void BeforeSolveInstance()
        {
            Utility.SetInputValueList<Path.Types>(Params.Input[2]);
            base.BeforeSolveInstance();
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var angle = default(double);
            var directionValue = default(int);

            if (!DA.GetData(0, ref angle)) return;
            if (!DA.GetData(1, ref directionValue)) return;

            var direction = (Path.Types)directionValue;

            // --- Execute

            var path = PrincipalCurvaturePath.Create(angle, direction);

            // --- Output

            DA.SetData(0, path);
        }

        protected override Bitmap Icon => Properties.Resources.icon_principal_curvature_path;

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        public override Guid ComponentGuid => new Guid("{CF603E5A-B724-46B5-A242-C40429F89B62}");
    }
}