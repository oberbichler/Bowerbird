using Bowerbird.Crafting;
using Clipper2Lib;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace Bowerbird.Components.CraftingComponents;

public class BBOffsetComponent : GH_Component
{
    public BBOffsetComponent() 
        : base("BB Offset", "BBOffset", "Offset a polyline with a specified distance" + Util.InfoString, "Bowerbird", "Polyline") 
    { 
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddCurveParameter("Curves", "C", "Curves to offset", GH_ParamAccess.list);
        pManager.AddNumberParameter("Distance", "D", "Offset distance", GH_ParamAccess.item);
        pManager.AddPlaneParameter("Plane", "P", "Plane for offset operation", GH_ParamAccess.item);
        
        var eIndex = pManager.AddIntegerParameter("End Type", "E", "End Cap Type (0 = Polygon, 1 = Joined, 2 = Butt, 3 = Square, 4 = Round)", GH_ParamAccess.item, 4); // Default to Round (4)
        var jIndex = pManager.AddIntegerParameter("Join Type", "J", "Corner Join Type (0 = Miter, 1 = Square, 2 = Bevel, 3 = Round)", GH_ParamAccess.item, 3); // Default to Round
        
        pManager.AddNumberParameter("Miter Limit", "M", "Miter Limit", GH_ParamAccess.item, 2.0);
        pManager.AddNumberParameter("Arc Tolerance", "A", "The maximum distance that the flattened path will deviate from the 'true' arc", GH_ParamAccess.item, 0.25);

        pManager[2].Optional = true;

        // Automatically bind Enums for dropdown behavior
        pManager[eIndex].AddNamedValues<EndType>();
        pManager[jIndex].AddNamedValues<JoinType>();
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddCurveParameter("Curve", "C", "", GH_ParamAccess.list);
    }

    public override void AddedToDocument(GH_Document document)
    {
        base.AddedToDocument(document);

        // Configure dropdown Value Lists automatically on placement
        Params.Input[3].SetInputValueList<EndType>();
        Params.Input[4].SetInputValueList<JoinType>();
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var curves = new List<Curve>();
        var distance = default(double);
        var plane = default(Plane?);
        var endTypeInt = default(int);
        var joinTypeInt = default(int);
        var miter = default(double);
        var arcTolerance = default(double);

        if (!DA.GetDataList(0, curves)) return;
        if (!DA.GetData(1, ref distance)) return;
        DA.GetData(2, ref plane);
        if (!DA.GetData(3, ref endTypeInt)) return;
        if (!DA.GetData(4, ref joinTypeInt)) return;
        if (!DA.GetData(5, ref miter)) return;
        if (!DA.GetData(6, ref arcTolerance)) return;

        var endType = (EndType)endTypeInt;
        var joinType = (JoinType)joinTypeInt;

        var result = BBPolyline.Offset(curves, distance, joinType, endType, miter, arcTolerance, plane);

        DA.SetDataList(0, result);
    }

    protected override System.Drawing.Bitmap? Icon => Bowerbird.Properties.Resources.icon_offset;

    public override GH_Exposure Exposure => GH_Exposure.primary;

    public override Guid ComponentGuid => new ("{01A4A06C-0FD3-4037-9CE7-10E9A9263439}");
}
