using Bowerbird.Parameters;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Drawing;

namespace Bowerbird.Components
{
    public class BBDGridPathComponent : GH_Component
    {
        public BBDGridPathComponent() : base("BB DGrid Path", "DGrid", "Beta! Interface might change!", "Bowerbird", "Paths")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Undeformed Surface", "S", "", GH_ParamAccess.item);
            pManager.AddSurfaceParameter("Deformed Surface", "s", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("Youngs Modulus", "E", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("Poissons Ration", "ν", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("Thickness", "t", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Direction", "D", "", GH_ParamAccess.item, 3);
            Utility.AddNamedValues<Path.Types>(Params.Input[5]);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new PathParameter(), "Path Type", "T", "", GH_ParamAccess.item);
        }

        protected override void BeforeSolveInstance()
        {
            Utility.SetInputValueList<Path.Types>(Params.Input[5]);
            base.BeforeSolveInstance();
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

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

            var direction = (Path.Types)directionValue;

            // --- Execute

            var path = DGridPath.Create(refSurface.ToNurbsSurface(), actSurface.ToNurbsSurface(), youngsModulus, poissonsRatio, thickness, direction);

            // --- Output

            DA.SetData(0, path);
        }

        protected override Bitmap Icon => null;

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public override Guid ComponentGuid => new Guid("{FD063786-F798-485D-8A53-97DE6B890327}");
    }
}