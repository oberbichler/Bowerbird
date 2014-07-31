using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Bowerbird
{
    public class BowerbirdInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get { return "Bowerbird"; }
        }

        public override string Description
        {
            get { return "Bowerbird Tools for Grasshopper"; }
        }

        public override string Version
        {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        public override Bitmap Icon
        {
            get { return Properties.Resources.logo_24; }
        }

        public override Guid Id
        {
            get { return new Guid("{DA048873-4434-442D-BA53-C9CCD138CFB0}"); }
        }

        public override string AuthorName
        {
            get { return "Thomas J. Oberbichler"; }
        }

        public override string AuthorContact
        {
            get { return "thomas.oberbichler@gmail.com"; }
        }
    }
}
