using Bowerbird.Types;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Bowerbird.Parameters
{
    internal class CurveOnSurfaceParameter : GH_Param<GH_CurveOnSurface>, IGH_PreviewObject
    {
        public CurveOnSurfaceParameter() : base(new GH_InstanceDescription("BB Path", "BBPath", "", "Bowerbird", "CurveOnSurface"))
        {
        }

        public override Guid ComponentGuid => new Guid("{84DD6CB7-9DC6-4EF4-B46B-45F37361F7F1}");

        private bool m_reparameterize;

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);
            Menu_MoveItem(Menu_AppendItem(menu, "Reparameterize", new EventHandler(Menu_ReparameterizeClicked), Properties.Resources.icon_normalize, true, m_reparameterize), "Simplify", "Graft", "Flatten", "Reverse");
        }

        private void Menu_ReparameterizeClicked(object sender, EventArgs e)
        {
            RecordUndoEvent("Reparameterize");
            m_reparameterize = !m_reparameterize;
            if (Kind == GH_ParamKind.output)
                ExpireOwner();
            ExpireSolution(true);
        }

        public override void RemoveEffects()
        {
            base.RemoveEffects();
            m_reparameterize = false;
        }

        public override GH_StateTagList StateTags
        {
            get
            {
                var stateTags = base.StateTags;
                if (m_reparameterize)
                    stateTags.Add((IGH_StateTag)new GH_StateTag_Reparameterize());
                return stateTags;
            }
        }

        protected override void OnVolatileDataCollected()
        {
            base.OnVolatileDataCollected();

            if (!m_reparameterize)
                return;

            foreach (var branch in m_data.Branches)
            {
                for (int i = 0; i < branch.Count; i++)
                {
                    var ghCurveOnSurface = branch[i];

                    if (ghCurveOnSurface?.Value == null)
                        continue;

                    var reparameterized = ghCurveOnSurface.Value.Reparameterized();

                    branch[i] = new GH_CurveOnSurface(reparameterized);
                }
            }
        }

        class GH_StateTag_Reparameterize : GH_StateTag
        {
            public override string Name => "Reparameterize";

            public override string Description => "Geometry inside this parameter is reparameterized";

            public override Bitmap Icon => Properties.Resources.icon_normalize;

            public override void Render(Graphics graphics)
            {
                RenderTagBlankIcon(graphics, new DrawCallback(RenderWave));
            }

            private void RenderWave(Graphics graphics, double alpha)
            {
                var x = Stage.X + 1.5f;
                var y = Stage.Y + 12f;
                var path = new GraphicsPath();
                path.AddLine(x + 0.0f, y + 0.0f, x + 0.0f, y - 2f);
                path.AddBezier(x + 0.0f, y - 2f, x + 3f, y - 2f, x + 2.5f, y - 8f, x + 6f, y - 8f);
                path.AddBezier(x + 6f, y - 8f, x + 9.5f, y - 8f, x + 9f, y - 2f, x + 12f, y - 2f);
                path.AddLine(x + 12f, y - 2f, x + 12f, y - 0.0f);
                path.AddBezier(x + 12f, y - 0.0f, x + 7f, y - 0.0f, x + 8f, y - 6f, x + 6f, y - 6f);
                path.AddBezier(x + 6f, y - 6f, x + 4f, y - 6f, x + 5f, y - 0.0f, x + 0.0f, y - 0.0f);
                RenderFreeformIcon(graphics, path);
                path.Dispose();
            }
        }

        // --- IGH_PreviewObject

        public bool Hidden { get; set; }

        public bool IsPreviewCapable => true;

        public BoundingBox ClippingBox => Preview_ComputeClippingBox();

        public void DrawViewportMeshes(IGH_PreviewArgs args) => Preview_DrawMeshes(args);

        public void DrawViewportWires(IGH_PreviewArgs args) => Preview_DrawWires(args);

        public override bool Read(GH_IReader reader)
        {
            var success = base.Read(reader);

            reader.TryGetBoolean("Reparameterize", ref m_reparameterize);

            return success;
        }

        public override bool Write(GH_IWriter writer)
        {
            var success = base.Write(writer);

            if (m_reparameterize)            
                writer.SetBoolean("Reparameterize", m_reparameterize);
            
            return success;
        }
    }
}
