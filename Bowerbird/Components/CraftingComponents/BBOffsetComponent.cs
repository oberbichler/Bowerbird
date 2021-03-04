using Bowerbird.Crafting;
using ClipperLib;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Bowerbird.Components.CraftingComponents
{
    public class BBOffsetComponent : GH_Component
    {
        public BBOffsetComponent() : base("BB Offset", "BBOffset", "Offset a polyline with a specified distance" + Util.InfoString, "Bowerbird", "Polyline") { }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "Curves to offset", GH_ParamAccess.list);
            pManager.AddNumberParameter("Distance", "D", "Offset distance", GH_ParamAccess.item);
            pManager.AddPlaneParameter("Plane", "P", "Plane for offset operation", GH_ParamAccess.item);
            pManager.AddIntegerParameter("End Type", "E", "0 = Round\n1 = Square\n2 = Butt", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Join Type", "J", "0 = Round\n1 = Square\n2 = Miter", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Miter Limit", "M", "", GH_ParamAccess.item, 10.0);
            pManager.AddNumberParameter("Arc Tolerance", "A", "The maximum distance that the flattened path will deviate from the 'true' arc", GH_ParamAccess.item, 0.01);

            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var curves = new List<Curve>();
            var distance = default(double);
            var plane = default(Plane?);
            var endTypeInt = default(int);
            var joinTypeInt = default(int);
            var miter = default(double);
            var arcTolerance = default(double);

            DA.GetDataList(0, curves);
            DA.GetData(1, ref distance);
            DA.GetData(2, ref plane);
            DA.GetData(3, ref endTypeInt);
            DA.GetData(4, ref joinTypeInt);
            DA.GetData(5, ref miter);
            DA.GetData(6, ref arcTolerance);


            JoinType joinType;

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

            EndType endType;

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

            var result = BBPolyline.Offset(curves, distance, joinType, endType, miter, arcTolerance, plane);


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
