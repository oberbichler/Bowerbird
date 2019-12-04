using ClipperLib;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bowerbird.Components
{
    public class BBBooleanComponent : GH_Component
    {
        public BBBooleanComponent() : base("BB Boolean", "BBBool", "Boolean operation between two sets of planar closed polylines" + Util.InfoString, "Bowerbird", "Polyline") 
        {
            Operation = ClipType.ctUnion;
            FillType = PolyFillType.pftEvenOdd;
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "A", "", GH_ParamAccess.list);
            pManager.AddCurveParameter("Curves", "B", "", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Plane", "P", "", GH_ParamAccess.item);

            pManager[1].Optional = true;
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

            var result = BBPolyline.Boolean(Operation, FillType, curvesA, curvesB, plane);

            
            // --- Output

            DA.SetDataList(0, result);
        }



        private ClipType _operation;

        public ClipType Operation
        {
            get
            {
                return _operation;
            }
            set
            {
                _operation = value;

                UpdateMessage();
            }
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

                UpdateMessage();               
            }
        }


        private void UpdateMessage()
        {
            var sb = new StringBuilder();

            switch (Operation)
            {
                case ClipType.ctUnion:
                    sb.Append("Union");
                    break;
                case ClipType.ctDifference:
                    sb.Append("Difference");
                    break;
                case ClipType.ctIntersection:
                    sb.Append("Intersection");
                    break;
                case ClipType.ctXor:
                    sb.Append("Xor");
                    break;
            }

            sb.AppendLine();

            switch (FillType)
            {
                case PolyFillType.pftEvenOdd:
                    sb.Append("Even-Odd");
                    break;
                case PolyFillType.pftNonZero:
                    sb.Append("Non-Zero");
                    break;
                case PolyFillType.pftPositive:
                    sb.Append("Positive");
                    break;
                case PolyFillType.pftNegative:
                    sb.Append("Negative");
                    break;
            }

            Message = sb.ToString();
        }


        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            AppendOperationMenuItem(menu, "Union", ClipType.ctUnion);
            AppendOperationMenuItem(menu, "Difference", ClipType.ctDifference);
            AppendOperationMenuItem(menu, "Intersection", ClipType.ctIntersection);
            AppendOperationMenuItem(menu, "Xor", ClipType.ctXor);
            Menu_AppendSeparator(menu);
            AppendFillTypeMenuItem(menu, "Even-Odd", PolyFillType.pftEvenOdd);
            AppendFillTypeMenuItem(menu, "Non-Zero", PolyFillType.pftNonZero);
            AppendFillTypeMenuItem(menu, "Positive", PolyFillType.pftPositive);
            AppendFillTypeMenuItem(menu, "Negative", PolyFillType.pftNegative);
        }

        private void AppendOperationMenuItem(System.Windows.Forms.ToolStripDropDown menu, string text, ClipType operation)
        {
            Menu_AppendItem(menu, text, (s, e) => { RecordUndoEvent("Operation"); Operation = operation; ExpireSolution(true); }, true, Operation == operation);
        }

        private void AppendFillTypeMenuItem(System.Windows.Forms.ToolStripDropDown menu, string text, PolyFillType fillType)
        {
            Menu_AppendItem(menu, text, (s, e) => { RecordUndoEvent("FillType"); FillType = fillType; ExpireSolution(true); }, true, FillType == fillType);
        }


        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetInt32("Operation", (int)Operation);
            writer.SetInt32("FillType", (int)FillType);

            return base.Write(writer);
        }

        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            Operation = (ClipType)reader.GetInt32("Operation");
            FillType = (PolyFillType)reader.GetInt32("FillType");

            return base.Read(reader);
        }


        protected override System.Drawing.Bitmap Icon
        {
            get { return Properties.Resources.icon_boolean; }
        }

        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{192AB461-640A-4786-998E-662B3DF2B9C2}"); }
        }
    }
}
