using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Drawing;

namespace Bowerbird.Components
{
    internal class BBCurvatureComponent : GH_Component
    {
        public BBCurvatureComponent() : base("BB Curvature", "BBCurv", "", "Bowerbird", "Curvature")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "", GH_ParamAccess.item);
            pManager.AddPointParameter("Parameter Point", "uv", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("K1", "K1", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("K2", "K2", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("K1 Parameter Direction", "d1", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("K2 Parameter Direction", "d2", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("K1 Direction", "D1", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("K2 Direction", "D2", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var surface = default(Surface);
            var uv = default(Point3d);

            if (!DA.GetData(0, ref surface)) return;
            if (!DA.GetData(1, ref uv)) return;


            // --- Execute

            var curvature = Curvature.SurfaceCurvature.Create(surface, uv.X, uv.Y);


            // --- Output

            DA.SetData(0, curvature.K1);
            DA.SetData(1, curvature.K2);
            DA.SetData(2, curvature.K1ParameterDirection);
            DA.SetData(3, curvature.K2ParameterDirection);
            DA.SetData(4, curvature.K1Direction);
            DA.SetData(5, curvature.K2Direction);
        }

        protected override Bitmap Icon => null;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public override Guid ComponentGuid => new Guid("{5300DCE5-F233-4CDA-BDC3-56DB437DA1EF}");
    }
}