using Clipper2Lib;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bowerbird.Crafting;

public static class BBPolyline
{
    public static IEnumerable<Curve> Boolean(ClipType operation, FillRule fillRule, IEnumerable<Curve> curvesA, IEnumerable<Curve> curvesB, Plane? plane)
    {
        var closedA = curvesA.Where(o => o.IsClosed).ToList();
        var closedB = curvesB.Where(o => o.IsClosed).ToList();

        if (!plane.HasValue)
        {
            foreach (var curve in closedA)
            {
                if (curve.TryGetPlane(out var curvePlane))
                {
                    plane = curvePlane;
                    break;
                }
            }
        }

        if (!plane.HasValue)
        {
            foreach (var curve in closedB)
            {
                if (curve.TryGetPlane(out var curvePlane))
                {
                    plane = curvePlane;
                    break;
                }
            }
        }

        if (!plane.HasValue)
            plane = Plane.WorldXY;

        var subjects = ToPathsD(closedA, plane.Value);
        var clips = ToPathsD(closedB, plane.Value);

        PathsD solution = operation switch
        {
            ClipType.Intersection => Clipper.Intersect(subjects, clips, fillRule),
            ClipType.Union => Clipper.Union(subjects, clips, fillRule),
            ClipType.Difference => Clipper.Difference(subjects, clips, fillRule),
            ClipType.Xor => Clipper.Xor(subjects, clips, fillRule),
            _ => new PathsD()
        };

        return ToCurves(solution, plane.Value);
    }

    public static IEnumerable<Curve> Offset(IEnumerable<Curve> curves, double distance, JoinType joinType, EndType endType, double miter, double arcTolerance, Plane? plane)
    {
        var curveList = curves as IList<Curve> ?? curves.ToList();
        plane = GetPlane(curveList, plane);

        var closedPaths = new PathsD();
        var openPaths = new PathsD();

        foreach (var curve in curveList)
        {
            var path = ToPathD(curve, plane.Value, arcTolerance > 0 ? arcTolerance : 0.01);
            if (path == null || path.Count == 0)
                continue;

            if (curve.IsClosed)
                closedPaths.Add(path);
            else
                openPaths.Add(path);
        }

        const int precision = 3;
        double scale = Math.Pow(10, precision);
        var offset = new ClipperOffset(miter, arcTolerance * scale);

        var effectiveClosedEndType = (endType == EndType.Joined) ? EndType.Joined : EndType.Polygon;
        var effectiveOpenEndType = (endType == EndType.Polygon || endType == EndType.Joined) ? EndType.Round : endType;

        if (closedPaths.Count > 0)
        {
            var scaledClosed = Clipper.ScalePaths64(closedPaths, scale);
            offset.AddPaths(scaledClosed, joinType, effectiveClosedEndType);
        }

        if (openPaths.Count > 0)
        {
            var scaledOpen = Clipper.ScalePaths64(openPaths, scale);
            offset.AddPaths(scaledOpen, joinType, effectiveOpenEndType);
        }

        var solution64 = new Paths64();
        offset.Execute(distance * scale, solution64);

        var solution = Clipper.ScalePathsD(solution64, 1.0 / scale);
        return ToCurves(solution, plane.Value);
    }

    private static Plane GetPlane(IEnumerable<Curve> curves, Plane? plane)
    {
        if (plane.HasValue)
            return plane.Value;

        foreach (var curve in curves)
        {
            if (curve.TryGetPlane(out var curvePlane))
                return curvePlane;
        }

        return Plane.WorldXY;
    }

    public static PathD? ToPathD(Curve curve, Plane plane, double tolerance = 0.01)
    {
        Polyline polyline;
        if (!curve.TryGetPolyline(out polyline))
        {
            var polylineCurve = curve.ToPolyline(tolerance, 0, 0, 0);
            if (polylineCurve == null || !polylineCurve.TryGetPolyline(out polyline))
                return null;
        }

        var points = new List<Point3d>(polyline);

        if (curve.IsClosed)
        {
            if (points.Count > 1 && points[0].DistanceTo(points[points.Count - 1]) < 1e-9)
            {
                points.RemoveAt(points.Count - 1);
            }

            if (curve.ClosedCurveOrientation(plane.ZAxis) == CurveOrientation.Clockwise)
            {
                points.Reverse();
            }
        }

        var path = new PathD(points.Count);
        for (int i = 0; i < points.Count; i++)
        {
            var pt = points[i] - plane.Origin;
            var x = pt * plane.XAxis;
            var y = pt * plane.YAxis;
            path.Add(new PointD(x, y));
        }

        return path;
    }

    public static PathsD ToPathsD(IEnumerable<Curve> curves, Plane plane, double tolerance = 0.01)
    {
        var paths = new PathsD();
        foreach (var curve in curves)
        {
            var path = ToPathD(curve, plane, tolerance);
            if (path != null && path.Count > 0)
                paths.Add(path);
        }
        return paths;
    }

    public static List<Curve> ToCurves(PathsD paths, Plane plane)
    {
        var curves = new List<Curve>(paths.Count);
        for (int i = 0; i < paths.Count; i++)
        {
            var path = paths[i];
            var points = new List<Point3d>(path.Count + 1);
            for (int j = 0; j < path.Count; j++)
            {
                var pt = path[j];
                points.Add(plane.Origin + pt.x * plane.XAxis + pt.y * plane.YAxis);
            }

            if (points.Count > 0 && points[0] != points[points.Count - 1])
                points.Add(points[0]);

            curves.Add(new PolylineCurve(points));
        }
        return curves;
    }
}
