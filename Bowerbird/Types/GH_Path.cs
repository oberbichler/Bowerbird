using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bowerbird.Types
{
    public class GH_Path : GH_Goo<Path>
    {
        public GH_Path() : base()
        {
        }

        public GH_Path(Path value) : base(value)
        {
        }

        public override bool IsValid => Value != null;

        public override string TypeName => "BBPath";

        public override string TypeDescription => "";

        public override IGH_Goo Duplicate()
        {
            return new GH_Path(Value);
        }

        public override bool CastFrom(object source)
        {
            switch (source)
            {
                case Path path:
                    Value = path;
                    return true;
                case GH_Path ghPath:
                    Value = ghPath.Value;
                    return true;
                default:
                    return base.CastFrom(source);
            }
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (!typeof(Q).IsAssignableFrom(m_value.GetType()))
                return false;

            var obj = (object)m_value;
            target = (Q)obj;

            return true;
        }

        public override string ToString() => Value.ToString();
    }
}
