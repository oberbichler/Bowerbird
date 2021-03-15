using Bowerbird.Crafting;
using ClipperLib;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Bowerbird.Components.CraftingComponents
{
    public class BBUnionComponent : GH_Component
    {
        public BBUnionComponent() : base("BB Union", "BBUnion", "Union of a set of planar closed polylines" + Util.InfoString, "Bowerbird", "Polyline")
        {
            FillType = PolyFillType.pftNonZero;
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Plane", "P", "", GH_ParamAccess.item);

            pManager[1].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var curves = new List<Curve>();
            var plane = default(Plane?);

            DA.GetDataList(0, curves);
            DA.GetData(1, ref plane);


            // --- Execute

            var result = BBPolyline.Boolean(ClipType.ctUnion, PolyFillType.pftNonZero, curves, new Curve[0], plane);


            // --- Output

            DA.SetDataList(0, result);
        }

        private PolyFillType _fillType;

        public PolyFillType FillType
        {
            get
            {
                return _fillType;
            }
            set
            {
                _fillType = value;

                switch (_fillType)
                {
                    case PolyFillType.pftEvenOdd:
                        Message = "Even-Odd";
                        break;
                    case PolyFillType.pftNonZero:
                        Message = "Non-Zero";
                        break;
                    case PolyFillType.pftPositive:
                        Message = "Positive";
                        break;
                    case PolyFillType.pftNegative:
                        Message = "Negative";
                        break;
                }
            }
        }


        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            Menu_AppendItem(menu, "Even-Odd", (s, e) => { FillType = PolyFillType.pftEvenOdd; ExpireSolution(true); }, true, FillType == PolyFillType.pftEvenOdd);
            Menu_AppendItem(menu, "Non-Zero", (s, e) => { FillType = PolyFillType.pftNonZero; ExpireSolution(true); }, true, FillType == PolyFillType.pftNonZero);
            Menu_AppendItem(menu, "Positive", (s, e) => { FillType = PolyFillType.pftPositive; ExpireSolution(true); }, true, FillType == PolyFillType.pftPositive);
            Menu_AppendItem(menu, "Negative", (s, e) => { FillType = PolyFillType.pftNegative; ExpireSolution(true); }, true, FillType == PolyFillType.pftNegative);
        }


        protected override System.Drawing.Bitmap Icon => Properties.Resources.icon_union;

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        public override Guid ComponentGuid => new Guid("{5585F2E8-4EAC-45A9-BBE6-6BD40379B7A4}");
    }
}