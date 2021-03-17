using Bowerbird.Curvature;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Drawing;

namespace Bowerbird.Components.CurvatureComponents
{
    public class ComputeAngleAtCurvature : GH_Component
    {
        public ComputeAngleAtCurvature() : base("BB Angle at Curvature", "BBAngleAtK", "", "Bowerbird", "Curvature")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "", GH_ParamAccess.item);
            pManager.AddPointParameter("Parameter Point", "uv", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("Curvature", "K", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Angle 1", "A1", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("Angle 2", "A2", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("Direction 1", "D1", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("Direction 2", "D2", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var surface = default(Surface);
            var uv = default(Point3d);
            var k = default(double);

            if (!DA.GetData(0, ref surface)) return;
            if (!DA.GetData(1, ref uv)) return;
            if (!DA.GetData(2, ref k)) return;


            // --- Execute

            var crv = new PrincipalCurvature();
            crv.Compute(surface, uv.X, uv.Y);

            if (!crv.FindNormalCurvature(k, 0, out var _, out var _, out var dir1, out var dir2))
                return;

            var alpha1 = Vector3d.VectorAngle(crv.D1, dir1);
            var alpha2 = Vector3d.VectorAngle(crv.D2, dir2);


            // --- Output

            DA.SetData(0, alpha1);
            DA.SetData(1, alpha2);
            DA.SetData(2, dir1);
            DA.SetData(3, dir2);
        }

        protected override Bitmap Icon => null;

        public override GH_Exposure Exposure => Info.Experimental ? GH_Exposure.primary : GH_Exposure.hidden;

        public override Guid ComponentGuid => new Guid("{C9A2B5C9-5F63-4500-8C66-6019147603AA}");
    }
}