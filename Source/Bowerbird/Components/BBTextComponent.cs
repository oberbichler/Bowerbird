using Bowerbird.Text;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Bowerbird.Components
{
    public class BBTextComponent : GH_Component
    {
        public BBTextComponent() : base("BB Text", "BBText", "Create a single line text" + Util.InfoString, "Bowerbird", "Text") { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Location", "L", "Location and orientation of the text", GH_ParamAccess.item);
            pManager.AddTextParameter("Text", "T", "Text to display", GH_ParamAccess.item);
            pManager.AddNumberParameter("Size", "S", "Size of the text", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Bold", "B", "True for bold font", GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("Horizontal alignment", "H", "0=Center, 1=Left, 2=Right", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Vertical alignment", "V", "0=Center, 1=Top, 2=Bottom, 3=Baseline", GH_ParamAccess.item, 0);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("Curves", "C", "Text as curves", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var location = default(Plane);
            var text = default(string);
            var size = default(double);
            var bold = default(bool);
            var hAlign = default(int);
            var vAlign = default(int);

            if (!DA.GetData(0, ref location)) return;
            if (!DA.GetData(1, ref text)) return;
            if (!DA.GetData(2, ref size)) return;
            if (!DA.GetData(3, ref bold)) return;
            if (!DA.GetData(4, ref hAlign)) return;
            if (!DA.GetData(5, ref vAlign)) return;

            var typeWriter = bold ? Typewriter.Bold : Typewriter.Regular;

            var position = location.Origin;
            var unitX = location.XAxis * size;
            var unitZ = location.YAxis * size;

            var curves = typeWriter.Write(text, position, unitX, unitZ, hAlign, vAlign);

            DA.SetDataList(0, curves);

            //FontWriter.Save(@"C:\Users\Thomas\Desktop\Test.xml", new[] { Typewriter.Regular, Typewriter.Bold });
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return Properties.Resources.icon_text; }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("{D6938666-DEC5-42D7-8EFD-AB17E9AAE67A}"); }
        }
    }
}
