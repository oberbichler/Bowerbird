using ClipperLib;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bowerbird.Components
{
    public class BBIntersectionComponent : GH_Component
    {
        public BBIntersectionComponent() : base("BB Intersection", "BBInt", "Intersection of a set of planar closed polylines" + Util.InfoString, "Bowerbird", "Polyline") { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "A", "", GH_ParamAccess.list);
            pManager.AddCurveParameter("Curve", "B", "", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Plane", "P", "", GH_ParamAccess.item);

            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var curvesA = new List<Curve>();
            var curvesB = new List<Curve>();
            var plane = default(Plane?);

            DA.GetDataList(0, curvesA);
            DA.GetDataList(1, curvesB);
            DA.GetData(2, ref plane);


            // --- Execute

            var result = BBPolyline.Boolean(ClipType.ctIntersection, PolyFillType.pftNonZero, curvesA, curvesB, plane);
            

            // --- Output

            DA.SetDataList(0, result);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return Properties.Resources.icon_intersection; }
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{DB9100DA-9996-4EE1-A955-399663EB9216}"); }
        }
    }
}
