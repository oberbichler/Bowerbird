using Bowerbird.Types;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace Bowerbird.Parameters
{
    internal class CurveOnSurfaceParameter : GH_Param<GH_CurveOnSurface>, IGH_PreviewObject
    {
        public CurveOnSurfaceParameter() : base(new GH_InstanceDescription("BB Path", "BBPath", "", "Bowerbird", "CurveOnSurface"))
        {
        }

        public override Guid ComponentGuid => new Guid("{84DD6CB7-9DC6-4EF4-B46B-45F37361F7F1}");

        // --- IGH_PreviewObject

        public bool Hidden { get; set; }

        public bool IsPreviewCapable => true;

        public BoundingBox ClippingBox => Preview_ComputeClippingBox();

        public void DrawViewportMeshes(IGH_PreviewArgs args) => Preview_DrawMeshes(args);

        public void DrawViewportWires(IGH_PreviewArgs args) => Preview_DrawWires(args);
    }
}
