using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bowerbird
{
    public class BowerbirdRhinoPlugIn : Rhino.PlugIns.PlugIn
    {
        public BowerbirdRhinoPlugIn()
        {
            Instance = this;
        }

        public static BowerbirdRhinoPlugIn Instance { get; private set; }
    }
}
