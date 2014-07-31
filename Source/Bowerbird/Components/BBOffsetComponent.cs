using ClipperLib;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bowerbird.Components
{
    public class BBOffsetComponent : GH_Component
    {
        public BBOffsetComponent() : base("BB Offset", "BBOffset", "Offset a polyline with a specified distance" + Util.InfoString, "Bowerbird", "Polyline") { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Curve to offset", GH_ParamAccess.item);
            pManager.AddNumberParameter("Distance", "D", "Offset distance", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "P", "Plane for offset operation", GH_ParamAccess.item);
            pManager.AddIntegerParameter("End Type", "E", "0 = Round\n1 = Square\n2 = Butt", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Join Type", "J", "0 = Round\n1 = Square\n2 = Miter", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Miter Limit", "M", "", GH_ParamAccess.item, 10);
            pManager.AddNumberParameter("Arc Tolerance", "A", "", GH_ParamAccess.item, 0.25);

            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var curve = default(Curve);
            var distance = default(double);
            var plane = default(Plane);
            var endTypeInt = default(int);
            var joinTypeInt = default(int);
            var miterLimit = default(double);
            var arcTolerance = default(double);

            DA.GetData(0, ref curve);
            DA.GetData(1, ref distance);

            if (!DA.GetData(2, ref plane))
                if (!curve.TryGetPlane(out plane))
                    throw new Exception("Curve is not planar!");

            DA.GetData(3, ref endTypeInt);
            DA.GetData(4, ref joinTypeInt);
            DA.GetData(5, ref miterLimit);
            DA.GetData(6, ref arcTolerance);


            var joinType = default(JoinType);

            switch (endTypeInt)
            {
                default:
                    joinType = JoinType.jtRound;
                    break;
                case 1:
                    joinType = JoinType.jtSquare;
                    break;
                case 2:
                    joinType = JoinType.jtMiter;
                    break;
            }

            var endType = default(EndType);

            switch (endTypeInt)
            {
                default:
                    endType = EndType.etOpenRound;
                    break;
                case 1:
                    endType = EndType.etOpenSquare;
                    break;
                case 2:
                    endType = EndType.etOpenButt;
                    break;
            }


            // --- Execute

            var unit = DocumentTolerance() / 10;

            var polygon = curve.ToPolygon(plane, unit);

            var clipper = new ClipperOffset(miterLimit, arcTolerance);

            if (curve.IsClosed)
                clipper.AddPath(polygon, joinType, ClipperLib.EndType.etClosedPolygon);
            else
                clipper.AddPath(polygon, joinType, endType);


            var solution = new List<List<IntPoint>>();

            clipper.Execute(ref solution, distance / unit);

            var result = solution.Select(o => o.ToCurve(plane, unit));


            // --- Output

            DA.SetDataList(0, result);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return Properties.Resources.icon_offset; }
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{01A4A06C-0FD3-4037-9CE7-10E9A9263439}"); }
        }
    }
}
