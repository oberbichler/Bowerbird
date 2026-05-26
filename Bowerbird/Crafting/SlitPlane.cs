using Clipper2Lib;
using Rhino.Geometry;
using System.Collections.Generic;
using System.Linq;

namespace Bowerbird.Crafting;

public class SlitPlane
{
    public SlitPlane(Mesh volume, Plane plane, double unit)
    {
        Plane = plane;
        Unit = unit;
        Curves = new List<Curve>();
        Polygons = volume.Section(plane, unit, Curves);
        Slits = new Paths64();
    }

    public void Clip(double x0, double x1, double y0, double y1)
    {
        var p00 = new Point3d(x0, y0, 0.0);
        var p01 = new Point3d(x0, y1, 0.0);
        var p11 = new Point3d(x1, y1, 0.0);
        var p10 = new Point3d(x1, y0, 0.0);

        var clip = new[] { p00, p10, p11, p01, p00 }.ToPolygon(Plane.WorldXY, Unit);
        var clipPaths = new Paths64 { clip };

        var solution = Clipper.Intersect(Polygons, clipPaths, FillRule.EvenOdd);

        Polygons = solution;
        Curves = Polygons.ToCurves(Plane, Unit);
    }

    public Plane Plane { get; set; }

    private double Unit { get; set; }

    private List<Curve> Curves { get; set; }

    private Paths64 Polygons { get; set; }

    private Paths64 Slits { get; set; }

    public void AddSlit(Point3d from, Point3d to, double thickness, double deeper)
    {
        using var cut = new LineCurve(from, to);

        var list = new List<SlitPoint>();
        for (int i = 0; i < Curves.Count; i++)
        {
            CollectSlitPoints(Curves[i], cut, list);
        }

        var points = list.OrderBy(o => o.T).ToList();

        var yUnit = cut.Line.UnitTangent;
        var xUnit = Vector3d.CrossProduct(yUnit, Plane.Normal);

        for (int i = 1; i < points.Count; i += 2)
        {
            var slitA = points[i - 1];
            var slitB = points[i];

            var a = slitA.Point;
            var b = slitB.Point;

            var mid = a + (b - a) / 2.0 + deeper * yUnit;
            var end = a - (b - a) / 2.0;

            var p00 = mid - thickness / 2.0 * xUnit;
            var p10 = p00 + thickness * xUnit;
            var p01 = end - thickness / 2.0 * xUnit;
            var p11 = p01 + thickness * xUnit;

            var slit = new[] { p00, p10, p11, p01, p00 }.ToPolygon(Plane, Unit);
            Slits.Add(slit);
        }
    }

    private void CollectSlitPoints(Curve curve, LineCurve line, List<SlitPoint> list)
    {
        var points = Rhino.Geometry.Intersect.Intersection.CurveCurve(line, curve, Unit, Unit);
        if (points == null) return;

        if (curve.IsClosed)
        {
            for (int i = 0; i < points.Count; i++)
            {
                var item = points[i];
                if (item.IsOverlap)
                {
                    if (item.OverlapA.T0 < item.OverlapA.T1)
                        list.Add(new SlitPoint(item.OverlapA.T0, item.PointA));
                    else
                        list.Add(new SlitPoint(item.OverlapA.T1, item.PointA2));
                }
                else
                {
                    list.Add(new SlitPoint(item.ParameterA, item.PointA));
                }
            }

            if (points.Count % 2 == 1)
                list.Add(new SlitPoint(line.GetLength(), line.PointAtEnd));
        }
        else
        {
            var len = line.GetLength();
            if (len < 1e-9) return;

            var unit = line.Domain.Length / len;
            var dir = line.Line.Direction;
            dir.Unitize();

            for (int i = 0; i < points.Count; i++)
            {
                var item = points[i];
                if (item.IsOverlap)
                {
                    if (item.OverlapA.T0 < item.OverlapA.T1)
                    {
                        list.Add(new SlitPoint(item.OverlapA.T0 - unit, item.PointA - dir));
                        list.Add(new SlitPoint(item.OverlapA.T0 + unit, item.PointA + dir));
                    }
                    else
                    {
                        list.Add(new SlitPoint(item.OverlapA.T1 - unit, item.PointA2 - dir));
                        list.Add(new SlitPoint(item.OverlapA.T1 + unit, item.PointA2 + dir));
                    }
                }
                else
                {
                    list.Add(new SlitPoint(item.ParameterA - unit, item.PointA - dir));
                    list.Add(new SlitPoint(item.ParameterA + unit, item.PointA + dir));
                }
            }
        }
    }

    private class SlitPoint
    {
        public SlitPoint(double t, Point3d point)
        {
            T = t;
            Point = point;
        }

        public double T { get; }
        public Point3d Point { get; }
    }

    public List<Curve> GetResult()
    {
        var solution = Clipper.Difference(Polygons, Slits, FillRule.EvenOdd);
        return solution.ToCurves(Plane, Unit);
    }
}
