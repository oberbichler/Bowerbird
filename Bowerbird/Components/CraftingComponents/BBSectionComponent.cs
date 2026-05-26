using Bowerbird.Crafting;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Bowerbird.Components.CraftingComponents;

public class BBSectionComponent : GH_Component
{
    public BBSectionComponent() 
        : base("BB Section", "BBSection", "Create a section model from a mesh" + Util.InfoString, "Bowerbird", "Crafting") 
    { 
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddMeshParameter("Mesh", "M", "Mesh of the model", GH_ParamAccess.item);
        pManager.AddPlaneParameter("Planes", "P", "Section Planes", GH_ParamAccess.list);
        pManager.AddNumberParameter("Thickness", "T", "Thickness of the parts", GH_ParamAccess.item);
        pManager.AddNumberParameter("Deeper", "D", "Makes the slits deeper", GH_ParamAccess.item, 0.0);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddCurveParameter("Section Curves", "C", "The resulting geschlitzte section curves", GH_ParamAccess.tree);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var mesh = default(Mesh);
        var planes = new List<Plane>();
        var thickness = default(double);
        var deeper = default(double);

        if (!DA.GetData(0, ref mesh)) return;
        if (!DA.GetDataList(1, planes)) return;
        if (!DA.GetData(2, ref thickness)) return;
        if (!DA.GetData(3, ref deeper)) return;

        if (mesh == null || planes.Count == 0) return;

        var unit = DocumentTolerance();

        var result = Section.Create(mesh, planes, thickness, deeper, unit);

        DA.SetEnum2D(0, result.Curves);
    }

    protected override System.Drawing.Bitmap? Icon => Bowerbird.Properties.Resources.icon_section;

    public override Guid ComponentGuid => new("{63013336-F9FB-4902-B63C-6B13C8A2F2DA}");
}
