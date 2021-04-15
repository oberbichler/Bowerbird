using Bowerbird.Types;
using Grasshopper.Kernel;
using System;

namespace Bowerbird.Parameters
{
    internal class PathParameter : GH_Param<GH_Path>
    {
        public PathParameter() : base(new GH_InstanceDescription("BB Path", "BBPath", "", "Bowerbird", "Paths"))
        {
        }

        public override Guid ComponentGuid { get; } = new Guid("{E58614E4-F77C-4110-A418-8BF8446CF9F6}");
    }
}
