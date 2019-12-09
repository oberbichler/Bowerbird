using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bowerbird.Components
{
    internal class BBAngleAtCurvatureComponent : GH_Component
    {
        public BBAngleAtCurvatureComponent() : base("BB Angle at Curvature", "BBAngleAtK", "", "Bowerbird", "Curvature")
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

            var curvature = Curvature.SurfaceCurvature.Create(surface, uv.X, uv.Y);

            if (!curvature.FindAngleByNormalCurvature(k, out var angle1, out var angle2))
                return;

            Vector3d direction1 = curvature.K1Direction;
            Vector3d direction2 = curvature.K1Direction;

            direction1.Rotate(angle1, surface.NormalAt(uv.X, uv.Y));
            direction2.Rotate(angle2, surface.NormalAt(uv.X, uv.Y));


            // --- Output

            DA.SetData(0, angle1);
            DA.SetData(1, angle2);
            DA.SetData(2, direction1);
            DA.SetData(3, direction2);
        }

        protected override Bitmap Icon => null;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public override Guid ComponentGuid => new Guid("{C9A2B5C9-5F63-4500-8C66-6019147603AA}");
    }
}