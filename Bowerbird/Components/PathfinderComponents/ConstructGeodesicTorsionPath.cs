using Bowerbird.Curvature;
using Bowerbird.Parameters;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Bowerbird.Components.PathfinderComponents
{
    public class ConstructGeodesicTorsionPath : GH_Component
    {
        public ConstructGeodesicTorsionPath() : base("BB Geodesic Torsion Path", "BBτg", "Follow the surface along a specified geodesic torsion. Connect this component to a Pathfinder.", "Bowerbird", "Paths")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Value", "V", "Desired geodesic torsion. Choosing value=0 returns principal curvature paths.", GH_ParamAccess.item, 0.0);
            pManager.AddNumberParameter("Angle", "A", "Optional angle to rotate the search direction", GH_ParamAccess.item, 0.0);
            pManager.AddIntegerParameter("Direction", "D", "Paths to evaluated (1=first, 2=second, 3=both, or use the context menu of the input parameter)", GH_ParamAccess.item, 3);
            Utility.AddNamedValues<Path.Types>(Params.Input[2]);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new PathParameter(), "Path Type", "T", "Path type for the Pathfinder component", GH_ParamAccess.item);
        }

        protected override void BeforeSolveInstance()
        {
            Utility.SetInputValueList<Path.Types>(Params.Input[2]);
            base.BeforeSolveInstance();
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var value = default(double);
            var angle = default(double);
            var directionValue = default(int);

            if (!DA.GetData(0, ref value)) return;
            if (!DA.GetData(1, ref angle)) return;
            if (!DA.GetData(2, ref directionValue)) return;

            var direction = (Path.Types)directionValue;

            // --- Execute

            var path = GeodesicTorsionPath.Create(value, angle, direction);

            // --- Output

            DA.SetData(0, path);
        }

        protected override Bitmap Icon => Properties.Resources.icon_principal_path;

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        public override Guid ComponentGuid => new Guid("{E540E209-EF00-4E66-9460-CC62C1D394AB}");
    }
}