using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace Bowerbird;

public class Info : GH_AssemblyInfo
{
    static Info()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var name = new AssemblyName(args.Name).Name;
            if (string.IsNullOrEmpty(name))
                return null;

            var assemblyDir = Path.GetDirectoryName(typeof(Info).Assembly.Location);
            if (string.IsNullOrEmpty(assemblyDir))
                return null;

            var dllPath = Path.Combine(assemblyDir, name + ".dll");
            if (File.Exists(dllPath))
            {
                return Assembly.LoadFrom(dllPath);
            }

            return null;
        };
    }

    public override string Name { get; } = "Bowerbird";

    public override Bitmap? Icon => Bowerbird.Properties.Resources.logo_24;

    public override string Description { get; } = "A modernized .NET 8 version of Bowerbird for Rhino and Grasshopper.";

    public override Guid Id { get; } = new("6ad6b4dc-e6e6-40a4-9198-3a2c55b9ad30");

    public override string AuthorName { get; } = "Thomas Oberbichler";

    public override string AuthorContact { get; } = "thomas.oberbichler@gmail.com";

    public override string Version { get; } = "1.0.0";
}
