using Clipper2Lib;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bowerbird.Components.TextComponents;

public class BBLabelPoint : GH_Component
{
    public BBLabelPoint()
        : base(
            name: "BB Label Point",
            nickname: "BBLabelPt",
            description: "Finds the pole of inaccessibility (optimal label point) for a planar polygon.",
            category: "Bowerbird",
            subCategory: "Text")
    { }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddCurveParameter("Boundary", "B",
            "Closed curves forming the polygon (outer boundary + holes, mixed)",
            GH_ParamAccess.list);
        pManager.AddNumberParameter("Precision", "P",
            "Search precision in model units",
            GH_ParamAccess.item, 1.0);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddPointParameter("Point", "P",
            "Optimal label point per outer polygon",
            GH_ParamAccess.list);
        pManager.AddNumberParameter("Distance", "D",
            "Distance to nearest edge per outer polygon",
            GH_ParamAccess.list);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var curves = new List<Curve>();
        var precision = 1.0;

        if (!DA.GetDataList(0, curves)) return;
        DA.GetData(1, ref precision);

        if (precision <= 0)
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                "Precision must be positive. Clamping to 1e-6.");
            precision = 1e-6;
        }

        foreach (var curve in curves)
        {
            if (!curve.IsClosed)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "All boundary curves must be closed.");
                return;
            }
        }

        if (!curves[0].TryGetPlane(out Plane plane))
        {
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Boundary curves must be planar.");
            return;
        }

        // Classify outer vs. hole rings via Clipper2 Union.
        // FillRule.EvenOdd is used so winding direction of input curves is irrelevant:
        // each nested ring alternates between filled (outer) and unfilled (hole).
        var clipper = new ClipperD();

        foreach (var curve in curves)
            clipper.AddSubject(CurveToPathD(curve, plane, precision));

        var tree = new PolyTreeD();
        clipper.Execute(ClipType.Union, FillRule.EvenOdd, tree);

        var resultPoints = new List<Point3d>();
        var resultDistances = new List<double>();

        foreach (PolyPathD outerChild in tree)
        {
            var rings = new List<Polylabel.Point[]>
            {
                outerChild.Polygon!.Select(p => new Polylabel.Point(p.x, p.y)).ToArray()
            };

            foreach (PolyPathD holeChild in outerChild)
                rings.Add(holeChild.Polygon!.Select(p => new Polylabel.Point(p.x, p.y)).ToArray());

            var polygon = new Polylabel.Polygon(rings.ToArray());
            var result = Polylabel.Polylabel.Run(polygon, precision);

            resultPoints.Add(plane.PointAt(result.Point.X, result.Point.Y));
            resultDistances.Add(result.Distance);
        }

        DA.SetDataList(0, resultPoints);
        DA.SetDataList(1, resultDistances);
    }

    private static PathD CurveToPathD(Curve curve, Plane plane, double precision)
    {
        Polyline polyline;

        if (!curve.TryGetPolyline(out polyline))
        {
            var plc = curve.ToPolyline(0, 0, 0.1, 0, precision, precision, precision, 0, true);
            if (plc == null || !plc.TryGetPolyline(out polyline))
                throw new InvalidOperationException("Failed to tessellate curve to polyline.");
        }

        // Clipper2 does not require a repeated closing vertex
        var count = polyline.Count;
        if (count > 1 && polyline[0].EpsilonEquals(polyline[count - 1], 1e-10))
            count--;

        var path = new PathD(count);
        for (var i = 0; i < count; i++)
        {
            plane.RemapToPlaneSpace(polyline[i], out var local);
            path.Add(new PointD(local.X, local.Y));
        }

        return path;
    }

    protected override System.Drawing.Bitmap? Icon => Properties.Resources.icon_label_point;

    public override Guid ComponentGuid => new("{CA940C52-E6B8-4495-AFAF-5050E62798C4}");
}
