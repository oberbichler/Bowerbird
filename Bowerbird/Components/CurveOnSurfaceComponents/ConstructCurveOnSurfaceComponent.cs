using Bowerbird.Curvature;
using Bowerbird.Parameters;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Bowerbird.Components.CurveOnSurfaceComponents
{
    public class ConstructCurveOnSurfaceComponent : GH_Component
    {
        public ConstructCurveOnSurfaceComponent() : base("BB Construct CurveOnSurface", "BBCrvOnSrf", "Beta! Interface might change!", "Bowerbird", "Curve on Surface")
        {
            UpdateMessage();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddSurfaceParameter("Surface", "S", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("Curve", "C", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new CurveOnSurfaceParameter(), "Curve on Surface", "C", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("Approximation", "A", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var surface = default(Surface);
            var curve = default(Curve);

            if (!DA.GetData(0, ref surface)) return;
            if (!DA.GetData(1, ref curve)) return;


            // --- Execute

            surface = (Surface)surface.Duplicate();

            var parameterCurve = curve;

            if (Space == SpaceTypes.XYZ)
                parameterCurve = surface.Pullback(curve, DocumentTolerance());

            var curveOnSurface = CurveOnSurface.Create(surface, parameterCurve);

            // --- Output

            DA.SetData(0, curveOnSurface);
            DA.SetData(1, curveOnSurface.ToCurve(DocumentTolerance()));
        }

        protected override Bitmap Icon => Properties.Resources.icon_curve_on_surface_construct;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        public override Guid ComponentGuid => new Guid("{DE62F1CD-5E5C-4372-B1BB-2494ED62D349}");


        public enum SpaceTypes
        {
            XYZ = 0,
            UV = 1
        }

        private SpaceTypes _space = SpaceTypes.XYZ;

        public SpaceTypes Space
        {
            get
            {
                return _space;
            }
            set
            {
                _space = value;
                UpdateMessage();
            }
        }

        private void UpdateMessage()
        {
            Message = Space.ToString();
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Utility.SetMenuList(this, menu, "Change space", () => Space, o => Space = o);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.Set("Space", Space);

            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            Space = reader.GetOrDefault("Space", SpaceTypes.XYZ);

            return base.Read(reader);
        }
    }
}