using ClipperLib;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bowerbird.Crafting
{
    class SlitPlane
    {
        public SlitPlane(Mesh volume, Plane plane, double unit)
        {
            Plane = plane;
            Unit = unit;

            Curves = new List<Curve>();

            Polygons = volume.Section(plane, unit, Curves);

            Slits = new List<List<IntPoint>>();
        }


        public void Clip(double x0, double x1, double y0, double y1)
        {
            var p00 = new Point3d(x0, y0, 0.0);
            var p01 = new Point3d(x0, y1, 0.0);
            var p11 = new Point3d(x1, y1, 0.0);
            var p10 = new Point3d(x1, y0, 0.0);

            var clip = new[] { p00, p10, p11, p01, p00 }.ToPolygon(Plane.WorldXY, Unit);

            var clipper = new Clipper();

            clipper.AddPaths(Polygons, PolyType.ptSubject, true);
            clipper.AddPath(clip, PolyType.ptClip, true);

            var solution = new List<List<IntPoint>>();

            clipper.Execute(ClipType.ctIntersection, solution, PolyFillType.pftEvenOdd, PolyFillType.pftNonZero);

            Polygons = solution;
            Curves = Polygons.ToCurves(Plane, Unit);
        }


        public Plane Plane { get; set; }

        private double Unit { get; set; }

        private List<Curve> Curves { get; set; }

        private List<List<IntPoint>> Polygons { get; set; }

        private List<List<IntPoint>> Slits { get; set; }


        public void AddSlit(Point3d from, Point3d to, double thickness, double deeper)
        {
            var cut = new LineCurve(from, to);

            var points = Curves.SelectMany(o => SlitPoints(o, cut)).OrderBy(o => o.T).ToList();

            var yUnit = cut.Line.UnitTangent;
            var xUnit = Vector3d.CrossProduct(yUnit, Plane.Normal);

            for (int i = 1; i < points.Count; i += 1)
            {
                var slitA = points[i - 1];
                var slitB = points[i++];

                var a = slitA.Point;
                var b = slitB.Point;

                var mid = a + (b - a) / 2 + deeper * yUnit;
                var end = a - (b - a) / 2;

                var p00 = mid - thickness / 2 * xUnit;
                var p10 = p00 + thickness * xUnit;
                var p01 = end - thickness / 2 * xUnit;
                var p11 = p01 + thickness * xUnit;

                var slit = new[] { p00, p10, p11, p01, p00 }.ToPolygon(Plane, Unit);

                Slits.Add(slit);
            }
        }

        IEnumerable<SlitPoint> SlitPoints(Curve curve, LineCurve line)
        {
            if (curve.IsClosed)
            {
                var points = Rhino.Geometry.Intersect.Intersection.CurveCurve(line, curve, Unit, Unit);
                
                foreach (var item in points)
                {
                    if (item.IsOverlap)
                        if (item.OverlapA.T0 < item.OverlapA.T1)
                            yield return new SlitPoint(item.OverlapA.T0, item.PointA);
                        else
                            yield return new SlitPoint(item.OverlapA.T1, item.PointA2);
                    else
                        yield return new SlitPoint(item.ParameterA, item.PointA);
                }

                if (points.Count % 2 == 1)
                    yield return new SlitPoint(line.GetLength(), line.PointAtEnd);
            }
            else
                foreach (var item in Rhino.Geometry.Intersect.Intersection.CurveCurve(line, curve, Unit, Unit))
                {
                    var unit = line.Domain.Length / line.GetLength();
                    var dir = line.Line.Direction;
                    dir.Unitize();

                    if (item.IsOverlap)
                        if (item.OverlapA.T0 < item.OverlapA.T1)
                        {
                            yield return new SlitPoint(item.OverlapA.T0 - unit, item.PointA - dir);
                            yield return new SlitPoint(item.OverlapA.T0 + unit, item.PointA + dir);
                        }
                        else
                        {
                            yield return new SlitPoint(item.OverlapA.T1 - unit, item.PointA2 - dir);
                            yield return new SlitPoint(item.OverlapA.T1 + unit, item.PointA2 + dir);
                        }
                    else
                    {
                        yield return new SlitPoint(item.ParameterA - unit, item.PointA - dir);
                        yield return new SlitPoint(item.ParameterA + unit, item.PointA + dir);
                    }
                }
        }

        class SlitPoint
        {
            public SlitPoint(double t, Point3d point)
            {
                T = t;
                Point = point;
            }

            public double T { get; private set; }

            public Point3d Point { get; private set; }
        }


        public List<Curve> GetResult()
        {
            var clipper = new Clipper();
            
            clipper.AddPaths(Polygons, PolyType.ptSubject, true);
            clipper.AddPaths(Slits, PolyType.ptClip, true);

            var solution = new List<List<IntPoint>>();

            clipper.Execute(ClipType.ctDifference, solution, PolyFillType.pftEvenOdd, PolyFillType.pftNonZero);

            return solution.ToCurves(Plane, Unit);
        }
    }
}
