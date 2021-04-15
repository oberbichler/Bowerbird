using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Reflection;

namespace Bowerbird
{
    public class Info : GH_AssemblyInfo
    {
        static Info()
        {
            Experimental = Environment.GetEnvironmentVariable("BOWERBIRD_EXPERIMENTAL") == "1";
        }

        public override string Name { get; } = "Bowerbird2";

        public override Bitmap Icon { get; } = Properties.Resources.logo_24;

        public override string Description { get; } = "";

        public override Guid Id { get; } = new Guid("6ad6b4dc-e6e6-40a4-9198-3a2c55b9ad30");

        public override string AuthorName { get; } = "Thomas Oberbichler";

        public override string AuthorContact { get; } = "thomas.oberbichler@gmail.com";

        public override string Version { get; } = Assembly.GetAssembly(typeof(Info)).GetName().Version.ToString();

        public override string AssemblyVersion => Version;

        public static bool Experimental { get; }
    }
}
