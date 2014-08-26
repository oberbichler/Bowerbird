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
    }
}
