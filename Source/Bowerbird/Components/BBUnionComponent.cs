using ClipperLib;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bowerbird.Components
{
    public class BBUnionComponent : GH_Component
    {
        public BBUnionComponent() : base("BB Union", "BBUnion", "Union of a set of planar closed polylines" + Util.InfoString, "Bowerbird", "Polyline") { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Plane", "P", "", GH_ParamAccess.item);

            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var curves = new List<Curve>();
            var plane = default(Plane?);

            DA.GetDataList(0, curves);
            DA.GetData(1, ref plane);


            var unit = DocumentTolerance() / 10;

            var polygons = curves.Where(o => o.IsClosed).ToPolygons(ref plane, unit);

            if (polygons.Count == 0)
                return;

            var clipper = new Clipper();

            clipper.AddPath(polygons.First(), PolyType.ptSubject, true);
            clipper.AddPaths(polygons.Skip(1).ToList(), PolyType.ptClip, true);

            var solution = new List<List<IntPoint>>();

            clipper.Execute(ClipType.ctUnion, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

            var result = solution.Select(o => o.ToCurve(plane.Value, unit));

            DA.SetDataList(0, result);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return Properties.Resources.icon_union; }
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{5585F2E8-4EAC-45A9-BBE6-6BD40379B7A4}"); }
        }
    }
}