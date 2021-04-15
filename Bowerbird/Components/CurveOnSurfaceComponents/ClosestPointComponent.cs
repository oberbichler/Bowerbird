using Bowerbird.Curvature;
using Bowerbird.Parameters;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Drawing;

namespace Bowerbird.Components.CurveOnSurfaceComponents
{
    public class ClosestPointComponent : GH_Component
    {
        public ClosestPointComponent() : base("BB CurveOnSurface Closest Point", "CP", "Find the closest point on an embedded curve.", "Bowerbird", "Curve on Surface")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "P", "Point to project onto the curve", GH_ParamAccess.item);
            pManager.AddParameter(new CurveOnSurfaceParameter(), "Curve on Surface", "C", "Embedded curve to project onto", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "P", "Point on the embedded curve closest to the base point", GH_ParamAccess.item);
            pManager.AddNumberParameter("Parameter", "t", "Parameter on curve domain of closest point", GH_ParamAccess.item);
            pManager.AddNumberParameter("Distance", "D", "Distance between base point and curve", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var curveOnSurface = default(CurveOnSurface);
            var sample = default(Point3d);

            if (!DA.GetData(0, ref curveOnSurface)) return;
            if (!DA.GetData(1, ref sample)) return;


            // --- Execute

            var tolerance = DocumentTolerance();

            var point = curveOnSurface.Invert(sample, tolerance);


            // --- Output

            DA.SetData(0, point.Item2);
            DA.SetData(1, point.Item1);
            DA.SetData(2, point.Item2.DistanceTo(sample));
        }

        protected override Bitmap Icon { get; } = Properties.Resources.icon_curve_on_surface_cp;

        public override GH_Exposure Exposure { get; } = GH_Exposure.secondary;

        public override Guid ComponentGuid { get; } = new Guid("{1E6B0DBB-29A6-4EDE-938F-D1280D9E46FD}");
    }
}