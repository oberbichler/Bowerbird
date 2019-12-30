using Grasshopper.Kernel.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bowerbird.Types
{
    public class GH_CurveOnSurface : GH_Goo<CurveOnSurface>
    {
        public GH_CurveOnSurface() : base()
        {
        }

        public GH_CurveOnSurface(CurveOnSurface value) : base(value)
        {
        }

        public override bool IsValid => Value != null;

        public override string TypeName => "BBCurveOnSurface";

        public override string TypeDescription => "";

        public override IGH_Goo Duplicate()
        {
            return new GH_CurveOnSurface(Value);
        }

        public override bool CastFrom(object source)
        {
            switch (source)
            {
                case CurveOnSurface curveOnSurface:
                    Value = curveOnSurface;
                    return true;
                case GH_CurveOnSurface ghCurveOnSurface:
                    Value = ghCurveOnSurface.Value;
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
