using ClipperLib;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bowerbird
{
    public static class BBPolyline
    {
        public static IEnumerable<Curve> Boolean(ClipType operation, PolyFillType fillType, IEnumerable<Curve> curvesA, IEnumerable<Curve> curvesB, Plane? plane)
        {
            var closedA = curvesA.Where(o => o.IsClosed);
            var closedB = curvesB.Where(o => o.IsClosed);

            if (!plane.HasValue)
            {
                foreach (var curve in closedA)
                {
                    var curvePlane = default(Plane);

                    if (!curve.TryGetPlane(out curvePlane))
                        continue;

                    plane = curvePlane;
                }
            }

            if (!plane.HasValue)
                plane = Plane.WorldXY;


            var polylinesA = new List<List<Point2d>>();
            var polylinesB = new List<List<Point2d>>();

            foreach (var curve in closedA)
            {
                var polyline = default(Polyline);

                if (!curve.TryGetPolyline(out polyline))
                    continue;

                polylinesA.Add(polyline.Select(o => o.Map2D(plane.Value)).ToList());
            }

            foreach (var curve in closedB)
            {
                var polyline = default(Polyline);

                if (!curve.TryGetPolyline(out polyline))
                    continue;

                polylinesB.Add(polyline.Select(o => o.Map2D(plane.Value)).ToList());
            }


            var minX = polylinesA.Union(polylinesB).SelectMany(o => o).Min(o => o.X);
            var minY = polylinesA.Union(polylinesB).SelectMany(o => o).Min(o => o.Y);
            var maxX = polylinesA.Union(polylinesB).SelectMany(o => o).Max(o => o.X);
            var maxY = polylinesA.Union(polylinesB).SelectMany(o => o).Max(o => o.Y);

            var unit = Math.Max(maxX - minX, maxY - minY) / (2 * 4.6e+18);

            var midX = (minX + maxX) / 2.0;
            var midY = (minY + maxY) / 2.0;
            

            var polygonsA = polylinesA.Select(o => o.Select(p => new IntPoint((p.X - midX) / unit, (p.Y - midY) / unit)).ToList())
                                      .ToList();

            var polygonsB = polylinesB.Select(o => o.Select(p => new IntPoint((p.X - midX) / unit, (p.Y - midY) / unit)).ToList())
                                      .ToList();


            var clipper = new Clipper();

            clipper.AddPaths(polygonsA, PolyType.ptSubject, true);
            clipper.AddPaths(polygonsB, PolyType.ptClip, true);

            var solution = new List<List<IntPoint>>();

            clipper.Execute(operation, solution, fillType, fillType);
            

            return solution.Select(o =>
            {
                var points = o.Select(p => plane.Value.Origin + (p.X * unit + midX) * plane.Value.XAxis + (p.Y * unit + midY) * plane.Value.YAxis)
                              .ToList();

                if (points.Count > 0 && points.First() != points.Last())
                    points.Add(points[0]);

                return new PolylineCurve(points);
            });
        }

        public static IEnumerable<Curve> Offset(IEnumerable<Curve> curves, double distance, JoinType joinType, EndType endType, double miter, double arcTolerance, Plane? plane)
        {
            var curveList = curves as IList<Curve> ?? curves.ToList();

            plane = GetPlane(curveList, plane);
            

            var polylines2D = curveList.Select(o => Polyline2D.FromCurve(o, plane.Value))
                                       .Where(o => o != null)
                                       .ToList();


            double unit;
            Point2d center;

            CalculateUnit(polylines2D, miter * distance, out unit, out center);


            var polylinesInt = polylines2D.Select(o => PolylineInt.FromPolyline2D(o, center, unit))
                                          .ToList();


            var clipper = new ClipperOffset(miter, arcTolerance / unit);

            foreach (var polygon in polylinesInt)
            {
                clipper.AddPath(polygon, joinType, polygon.Closed ? EndType.etClosedPolygon : endType);
            }


            var solution = new List<List<IntPoint>>();

            clipper.Execute(ref solution, distance / unit);


            return solution.Select(o => o.ToCurve(plane.Value, center, unit));
        }
        

        private static Plane GetPlane(IEnumerable<Curve> curves, Plane? plane)
        {
            if (plane.HasValue)
                return plane.Value;

            foreach (var curve in curves)
            {
                Plane curvePlane;

                if (!curve.TryGetPlane(out curvePlane))
                    continue;

                return curvePlane;
            }

            return Plane.WorldXY;
        }
        
        private static void CalculateUnit(IEnumerable<List<Point2d>> polylines, double margin, out double unit, out Point2d center)
        {
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxX = double.MinValue;
            var maxY = double.MinValue;

            foreach (var point in polylines.SelectMany(polyline => polyline))
            {
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
            }
            
            var centerX = (minX + maxX) / 2.0;
            var centerY = (minY + maxY) / 2.0;

            center = new Point2d(centerX, centerY);

            unit = (Math.Max(maxX - minX, maxY - minY) + Math.Max(0.0, 2.0 * margin)) / (2.0 * 4.6e+18);
        }


        private class Polyline2D : List<Point2d>
        {
            public Polyline2D(IEnumerable<Point2d> points) : base(points) { }


            public static Polyline2D FromCurve(Curve curve, Plane plane)
            {
                Polyline polyline;

                if (!curve.TryGetPolyline(out polyline))
                    return null;

                var points = polyline.Select(o => o.Map2D(plane));

                if (curve.ClosedCurveOrientation(plane.ZAxis) == CurveOrientation.Clockwise)
                    points = points.Reverse();

                var result = new Polyline2D(points) { Closed = curve.IsClosed };

                return result;
            }

            public bool Closed { get; set; }
        }

        private class PolylineInt : List<IntPoint>
        {
            public PolylineInt(IEnumerable<IntPoint> points) : base(points) { }
            
            public static PolylineInt FromPolyline2D(Polyline2D polyline2D, Point2d origin, double unit)
            {
                var intPoints = polyline2D.Select(o => new IntPoint((o.X - origin.X) / unit, (o.Y - origin.Y) / unit));

                var result = new PolylineInt(intPoints) {Closed = polyline2D.Closed};

                return result;
            }

            public Curve ToCurve(Plane plane, Point2d origin, double unit)
            {
                var points = this.Select(p => plane.Origin + (p.X * unit + origin.X) * plane.XAxis + (p.Y * unit + origin.Y) * plane.YAxis)
                                 .ToList();

                if (points.Count > 0 && points.First() != points.Last())
                    points.Add(points[0]);

                return new PolylineCurve(points);
            }

            public bool Closed { get; set; }
        }
    }
}
