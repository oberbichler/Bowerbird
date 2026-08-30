using CavalierContours;
using CavalierContours.Polyline;
using CavalierContours.Shape;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bowerbird.Crafting;

public static class BBCavalier
{
    public static Shape<double> CreateShape(IEnumerable<Polyline<double>> closedPolylines)
    {
        ArgumentNullException.ThrowIfNull(closedPolylines);

        var plineList = closedPolylines.Where(p => p != null).ToList();
        if (plineList.Count == 0)
            return Shape<double>.Empty();

        var orientedPolylines = new List<Polyline<double>>(plineList.Count);
        var containsOptions = new PlineContainsOptions<double>();

        // Precompute approximate bounding boxes for fast broad-phase filtering
        var extents = new (double MinX, double MinY, double MaxX, double MaxY)[plineList.Count];
        for (int i = 0; i < plineList.Count; i++)
        {
            var box = plineList[i].Extents();
            if (box.HasValue)
            {
                extents[i] = (box.Value.MinX, box.Value.MinY, box.Value.MaxX, box.Value.MaxY);
            }
        }

        for (int i = 0; i < plineList.Count; i++)
        {
            var current = plineList[i];
            var (cMinX, cMinY, cMaxX, cMaxY) = extents[i];
            int depth = 0;

            for (int j = 0; j < plineList.Count; j++)
            {
                if (i == j) continue;
                var (oMinX, oMinY, oMaxX, oMaxY) = extents[j];

                // Broad-phase: current must be within other's AABB to be contained
                if (cMinX < oMinX || cMaxX > oMaxX || cMinY < oMinY || cMaxY > oMaxY)
                    continue;

                var other = plineList[j];
                var containsResult = PlineContains.PolylineContains(other, current, containsOptions);
                if (containsResult == PlineContainsResult.Pline2InsidePline1)
                {
                    depth++;
                }
            }

            var oriented = new Polyline<double>(current.IterVertexes(), current.IsClosed);
            var orientation = oriented.Orientation();

            if (depth % 2 == 0) // Island (even depth: 0, 2, ...) -> CounterClockwise
            {
                if (orientation == PlineOrientation.Clockwise)
                {
                    oriented.InvertDirection();
                }
            }
            else // Hole (odd depth: 1, 3, ...) -> Clockwise
            {
                if (orientation == PlineOrientation.CounterClockwise)
                {
                    oriented.InvertDirection();
                }
            }

            orientedPolylines.Add(oriented);
        }

        return Shape<double>.FromPlines(orientedPolylines);
    }

    public static Polyline<double> ToPolyline(Curve curve, Plane plane, double tolerance)
    {
        ArgumentNullException.ThrowIfNull(curve);

        // 1. Convert freeform curve into a PolyCurve composed strictly of lines and arcs
        // We use standard tolerances: angle tolerance of 0.1 radians, min length 0.001, max length 0.0
        var lineArcCurve = curve.ToArcsAndLines(tolerance, 0.1, 0.001, 0.0);

        var simplified = lineArcCurve ?? (curve.Duplicate() as Curve);
        if (simplified == null)
            throw new InvalidOperationException("Failed to convert or duplicate curve.");

        var polyline = new Polyline<double>();

        var segmentsList = new List<Curve>();
        var originalSegments = simplified.DuplicateSegments();
        if (originalSegments == null || originalSegments.Length == 0)
        {
            segmentsList.Add(simplified);
        }
        else
        {
            segmentsList.AddRange(originalSegments);
        }

        // If the curve is closed and has only 1 segment (like a circle), split it in half
        // to prevent 1-vertex closed polyline degeneracy (Cavalier requires >= 2 vertices for closed shapes).
        if (curve.IsClosed && segmentsList.Count == 1)
        {
            var singleSegment = segmentsList[0];
            var halfT = singleSegment.Domain.Mid;
            var splitCurves = singleSegment.Split(halfT);
            if (splitCurves != null && splitCurves.Length == 2)
            {
                segmentsList.Clear();
                segmentsList.AddRange(splitCurves);
            }
            else if (splitCurves != null && splitCurves.Length == 1)
            {
                // In RhinoCommon, splitting a periodic closed curve at 1 parameter opens it into 1 open curve.
                // We then split that opened curve in half to obtain 2 open segments.
                var opened = splitCurves[0];
                var subSplit = opened.Split(opened.Domain.Mid);
                if (subSplit != null && subSplit.Length == 2)
                {
                    segmentsList.Clear();
                    segmentsList.AddRange(subSplit);
                }
            }
        }

        var segments = segmentsList.ToArray();

        // Explicitly set IsClosed on the Cavalier Polyline using its mutable setter method
        polyline.SetIsClosed(curve.IsClosed);

        for (int i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];

            plane.ClosestParameter(segment.PointAtStart, out double startU, out double startV);
            plane.ClosestParameter(segment.PointAtEnd, out double endU, out double endV);

            if (segment.IsLinear(tolerance))
            {
                polyline.AddVertex(new PlineVertex<double>(startU, startV, 0.0));
            }
            else if (segment.TryGetArc(out var arc, tolerance))
            {
                plane.ClosestParameter(segment.PointAt(segment.Domain.Mid), out double midU, out double midV);

                double L = Math.Sqrt((endU - startU) * (endU - startU) + (endV - startV) * (endV - startV));
                if (L < 1e-9)
                {
                    polyline.AddVertex(new PlineVertex<double>(startU, startV, 0.0));
                    continue;
                }

                double chordMidU = (startU + endU) / 2.0;
                double chordMidV = (startV + endV) / 2.0;
                double h = Math.Sqrt((midU - chordMidU) * (midU - chordMidU) + (midV - chordMidV) * (midV - chordMidV));
                double bulgeMagnitude = 2.0 * h / L;

                // Compute orientation sign using determinant/cross product in 2D plane coordinates
                double val = (endU - startU) * (midV - startV) - (endV - startV) * (midU - startU);
                
                // For a CCW arc walking start -> end, the arc midpoint lies to the right (val < 0).
                // Since CCW arcs require a positive bulge in Cavalier, we invert the cross product sign.
                double sign = val >= 0 ? -1.0 : 1.0;
                double bulge = sign * bulgeMagnitude;

                polyline.AddVertex(new PlineVertex<double>(startU, startV, bulge));
            }
            else
            {
                // Fallback to ToPolyline for spline curve segments
                var polylineCurve = segment.ToPolyline(tolerance, 0, 0, 0);
                if (polylineCurve != null)
                {
                    var poly = polylineCurve.ToPolyline();
                    for (int j = 0; j < poly.Count - 1; j++)
                    {
                        plane.ClosestParameter(poly[j], out double ptU, out double ptV);
                        polyline.AddVertex(new PlineVertex<double>(ptU, ptV, 0.0));
                    }
                }
                else
                {
                    polyline.AddVertex(new PlineVertex<double>(startU, startV, 0.0));
                }
            }
        }

        if (!curve.IsClosed && segments.Length > 0)
        {
            plane.ClosestParameter(segments.Last().PointAtEnd, out double finalU, out double finalV);
            polyline.AddVertex(new PlineVertex<double>(finalU, finalV, 0.0));
        }

        // Enforce standard Counter-Clockwise orientation for closed polylines.
        // This is extremely reliable as it is calculated directly on the 2D plane coordinates
        // and guarantees that Cavalier's offset and self-intersection pruning work perfectly.
        if (polyline.IsClosed)
        {
            if (polyline.Orientation() == PlineOrientation.Clockwise)
            {
                polyline.InvertDirection();
            }
        }

        return polyline;
    }

    public static Curve ToCurve(Polyline<double> pline, Plane plane)
    {
        ArgumentNullException.ThrowIfNull(pline);

        var polyCurve = new PolyCurve();

        if (pline.VertexCount == 0)
            return polyCurve;

        int count = pline.IsClosed ? pline.VertexCount : pline.VertexCount - 1;

        for (int i = 0; i < count; i++)
        {
            var vCurrent = pline[i];
            var vNext = pline[(i + 1) % pline.VertexCount];

            var pCurrent = plane.PointAt(vCurrent.X, vCurrent.Y);
            var pNext = plane.PointAt(vNext.X, vNext.Y);

            if (Math.Abs(vCurrent.Bulge) < 1e-9)
            {
                polyCurve.Append(new LineCurve(pCurrent, pNext));
            }
            else
            {
                double startX = vCurrent.X;
                double startY = vCurrent.Y;
                double endX = vNext.X;
                double endY = vNext.Y;

                double chordMidX = (startX + endX) / 2.0;
                double chordMidY = (startY + endY) / 2.0;

                double dx = endX - startX;
                double dy = endY - startY;

                // Positive bulge (CCW arc) curves to the right, which is the right perpendicular direction (dy, -dx)
                double midX = chordMidX + vCurrent.Bulge * dy / 2.0;
                double midY = chordMidY - vCurrent.Bulge * dx / 2.0;

                var pMid = plane.PointAt(midX, midY);
                var arc = new Arc(pCurrent, pMid, pNext);
                polyCurve.Append(new ArcCurve(arc));
            }
        }

        if (pline.IsClosed)
        {
            polyCurve.MakeClosed(1e-5);
        }

        return polyCurve;
    }

    public static List<Curve> Offset(IEnumerable<Curve> curves, double distance, Plane? plane, double tolerance, bool handleSelfIntersects)
    {
        ArgumentNullException.ThrowIfNull(curves);

        var refPlane = plane ?? Plane.WorldXY;
        var results = new List<Curve>();

        var validCurves = curves.Where(c => c is not null).ToList();
        var openCurves = new List<Curve>();
        var closedCurves = new List<Curve>();

        foreach (var curve in validCurves)
        {
            if (curve.IsClosed)
            {
                closedCurves.Add(curve);
            }
            else
            {
                openCurves.Add(curve);
            }
        }

        var plineOptions = new PlineOffsetOptions<double>
        {
            HandleSelfIntersects = handleSelfIntersects
        };

        // 1. Process open curves individually
        foreach (var openCurve in openCurves)
        {
            var pline = ToPolyline(openCurve, refPlane, tolerance);
            var offsetPlines = PlineOffset.ParallelOffset<Polyline<double>, double>(pline, -distance, plineOptions);
            if (offsetPlines != null)
            {
                foreach (var offsetPline in offsetPlines)
                {
                    results.Add(ToCurve(offsetPline, refPlane));
                }
            }
        }

        // 2. Process closed curves
        if (closedCurves.Count == 1)
        {
            var pline = ToPolyline(closedCurves[0], refPlane, tolerance);
            var offsetPlines = PlineOffset.ParallelOffset<Polyline<double>, double>(pline, -distance, plineOptions);
            if (offsetPlines != null)
            {
                foreach (var offsetPline in offsetPlines)
                {
                    results.Add(ToCurve(offsetPline, refPlane));
                }
            }
        }
        else if (closedCurves.Count > 1)
        {
            var closedPlines = new List<Polyline<double>>();
            foreach (var closedCurve in closedCurves)
            {
                closedPlines.Add(ToPolyline(closedCurve, refPlane, tolerance));
            }

            var shape = CreateShape(closedPlines);
            var shapeOptions = new ShapeOffsetOptions<double>();
            var offsetShape = shape.ParallelOffset(-distance, shapeOptions);

            if (offsetShape != null)
            {
                if (offsetShape.CcwPlines != null)
                {
                    foreach (var loop in offsetShape.CcwPlines)
                    {
                        results.Add(ToCurve(loop.Polyline, refPlane));
                    }
                }

                if (offsetShape.CwPlines != null)
                {
                    foreach (var loop in offsetShape.CwPlines)
                    {
                        results.Add(ToCurve(loop.Polyline, refPlane));
                    }
                }
            }
        }

        return results;
    }

    private static Plane GetPlane(IEnumerable<Curve> curves, Plane? plane)
    {
        if (plane.HasValue)
            return plane.Value;

        foreach (var curve in curves)
        {
            if (curve != null && curve.TryGetPlane(out var curvePlane))
                return curvePlane;
        }

        return Plane.WorldXY;
    }

    public static List<Curve> Boolean(BooleanOp operation, IEnumerable<Curve> curvesA, IEnumerable<Curve> curvesB, Plane? plane, double tolerance)
    {
        ArgumentNullException.ThrowIfNull(curvesA);

        var listA = curvesA.Where(c => c is not null && c.IsClosed).ToList();
        var listB = (curvesB ?? Enumerable.Empty<Curve>()).Where(c => c is not null && c.IsClosed).ToList();

        if (listA.Count == 0 && listB.Count == 0)
            return new List<Curve>();

        var refPlane = GetPlane(listA.Concat(listB), plane);
        var boolOpts = new PlineBooleanOptions<double>();

        var plinesA = listA.Select(c => ToPolyline(c, refPlane, tolerance)).ToList();
        var plinesB = listB.Select(c => ToPolyline(c, refPlane, tolerance)).ToList();

        List<Polyline<double>> UnionAll(List<Polyline<double>> inputPlines)
        {
            if (inputPlines.Count == 0) return new List<Polyline<double>>();
            var acc = new List<Polyline<double>> { inputPlines[0] };

            for (int i = 1; i < inputPlines.Count; i++)
            {
                var next = inputPlines[i];
                var nextAcc = new List<Polyline<double>>();
                var subject = next;

                for (int j = 0; j < acc.Count; j++)
                {
                    var target = acc[j];
                    if (subject == null)
                    {
                        nextAcc.Add(target);
                        continue;
                    }

                    var res = PlineBoolean.PolylineBoolean<Polyline<double>, double>(target, subject, BooleanOp.Or, boolOpts);
                    if (res.PosPlines.Count == 1 && res.NegPlines.Count == 0)
                    {
                        subject = res.PosPlines[0].Pline;
                    }
                    else
                    {
                        nextAcc.Add(target);
                    }
                }

                if (subject != null)
                {
                    nextAcc.Add(subject);
                }

                acc = nextAcc;
            }

            return acc;
        }

        List<Polyline<double>> DifferenceAll(List<Polyline<double>> subjects, List<Polyline<double>> clips)
        {
            var currentSubjects = subjects;
            foreach (var clip in clips)
            {
                var nextSubjects = new List<Polyline<double>>();
                foreach (var subj in currentSubjects)
                {
                    var res = PlineBoolean.PolylineBoolean<Polyline<double>, double>(subj, clip, BooleanOp.Not, boolOpts);
                    foreach (var p in res.PosPlines) nextSubjects.Add(p.Pline);
                    foreach (var n in res.NegPlines) nextSubjects.Add(n.Pline);
                }
                currentSubjects = nextSubjects;
            }
            return currentSubjects;
        }

        var results = new List<Curve>();
        var unionA = UnionAll(plinesA);
        var unionB = UnionAll(plinesB);

        List<Polyline<double>> outputPlines = new();

        switch (operation)
        {
            case BooleanOp.Or:
                outputPlines = UnionAll(plinesA.Concat(plinesB).ToList());
                break;

            case BooleanOp.Not:
                if (unionB.Count == 0)
                {
                    outputPlines = unionA;
                }
                else
                {
                    outputPlines = DifferenceAll(unionA, unionB);
                }
                break;

            case BooleanOp.And:
                foreach (var a in unionA)
                {
                    foreach (var b in unionB)
                    {
                        var res = PlineBoolean.PolylineBoolean<Polyline<double>, double>(a, b, BooleanOp.And, boolOpts);
                        foreach (var p in res.PosPlines) outputPlines.Add(p.Pline);
                        foreach (var n in res.NegPlines) outputPlines.Add(n.Pline);
                    }
                }
                break;

            case BooleanOp.Xor:
                var diffAB = DifferenceAll(unionA, unionB);
                var diffBA = DifferenceAll(unionB, unionA);
                outputPlines = diffAB.Concat(diffBA).ToList();
                break;
        }

        foreach (var pline in outputPlines)
        {
            results.Add(ToCurve(pline, refPlane));
        }

        return results;
    }
}
