using Bowerbird.Types;
using Grasshopper.Kernel;
using System;

namespace Bowerbird.Parameters
{
    internal class CurveOnSurfaceParameter : GH_Param<GH_CurveOnSurface>
    {
        public CurveOnSurfaceParameter() : base(new GH_InstanceDescription("BB Path", "BBPath", "", "Bowerbird", "CurveOnSurface"))
        {
        }

        public override Guid ComponentGuid => new Guid("{84DD6CB7-9DC6-4EF4-B46B-45F37361F7F1}");
    }
}
