using Bowerbird.Parameters;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bowerbird
{
    public class NormalCurvatureComponent : GH_Component
    {
        public NormalCurvatureComponent() : base("BB Normal Curvature CurveOnSurface", "κn", "", "Bowerbird", "Curve on Surface")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new CurveOnSurfaceParameter(), "Curve on Surface", "C", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("Parameter", "t", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "P", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("Curvature", "K", "", GH_ParamAccess.item);
            pManager.AddCircleParameter("Osculating Circle", "C", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var curveOnSurface = default(CurveOnSurface);
            var t = default(double);

            if (!DA.GetData(0, ref curveOnSurface)) return;
            if (!DA.GetData(1, ref t)) return;


            // --- Execute

            var point = curveOnSurface.PointAt(t);
            var tangent = curveOnSurface.TangentAt(t);
            var curvature = curveOnSurface.NormalCurvatureAt(t);
            var circle = curvature.Length < 1e-10 ? new Circle(point, 0) : new Circle(new Plane(point + curvature / Math.Pow(curvature.Length, 2), tangent, curvature), 1 / curvature.Length);


            // --- Output

            DA.SetData(0, point);
            DA.SetData(1, curvature);
            DA.SetData(2, circle);
        }

        protected override Bitmap Icon => null;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public override Guid ComponentGuid => new Guid("{ED838E93-41CC-4BDB-AACA-F8D3F948AB5E}");
    }
}