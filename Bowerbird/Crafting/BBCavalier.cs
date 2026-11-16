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

        var depths = NestingDepths(plineList);
        var orientedPolylines = new List<Polyline<double>>(plineList.Count);

        for (int i = 0; i < plineList.Count; i++)
        {
            // Island (even depth: 0, 2, ...) -> CounterClockwise, hole (odd depth) -> Clockwise
            orientedPolylines.Add(AsOrientedLoop(plineList[i], isSolid: depths[i] % 2 == 0));
        }

        return Shape<double>.FromPlines(orientedPolylines);
    }

    /// <summary>
    /// Returns, for every loop, how many of the other loops strictly contain it.
    /// </summary>
    /// <remarks>
    /// Even depth (0, 2, ...) marks material, odd depth marks a hole. Loops that touch or cross
    /// do not contain each other.
    /// </remarks>
    private static int[] NestingDepths(IReadOnlyList<Polyline<double>> plines)
    {
        var containsOptions = new PlineContainsOptions<double>();
        var extents = plines.Select(Extents).ToArray();
        var depths = new int[plines.Count];

        for (int i = 0; i < plines.Count; i++)
        {
            var (cMinX, cMinY, cMaxX, cMaxY) = extents[i];

            for (int j = 0; j < plines.Count; j++)
            {
                if (i == j) continue;
                var (oMinX, oMinY, oMaxX, oMaxY) = extents[j];

                // Broad-phase: current must be within other's AABB to be contained
                if (cMinX < oMinX || cMaxX > oMaxX || cMinY < oMinY || cMaxY > oMaxY)
                    continue;

                if (PlineContains.PolylineContains(plines[j], plines[i], containsOptions) == PlineContainsResult.Pline2InsidePline1)
                    depths[i]++;
            }
        }

        return depths;
    }

    private static (double MinX, double MinY, double MaxX, double MaxY) Extents(Polyline<double> pline)
    {
        var box = pline.Extents();
        return box.HasValue ? (box.Value.MinX, box.Value.MinY, box.Value.MaxX, box.Value.MaxY) : default;
    }

    private static bool ExtentsOverlap((double MinX, double MinY, double MaxX, double MaxY) a, (double MinX, double MinY, double MaxX, double MaxY) b, double eps)
        => a.MinX <= b.MaxX + eps && b.MinX <= a.MaxX + eps && a.MinY <= b.MaxY + eps && b.MinY <= a.MaxY + eps;

    public static Polyline<double> ToPolyline(Curve curve, Plane plane, double tolerance)
    {
        ArgumentNullException.ThrowIfNull(curve);

        // 1. Project onto the plane first. An arc in a plane tilted against `plane` projects to an
        // ellipse, and the three-point bulge below would wrongly turn it back into a circular arc.
        var projected = Curve.ProjectToPlane(curve, plane) ?? curve;

        // 2. Convert freeform curve into a PolyCurve composed strictly of lines and arcs
        // We use standard tolerances: angle tolerance of 0.1 radians, min length 0.001, max length 0.0
        var lineArcCurve = projected.ToArcsAndLines(tolerance, 0.1, 0.001, 0.0);

        var simplified = lineArcCurve ?? (projected.Duplicate() as Curve);
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

        polyline = SplitArcsWiderThanHalfCircle(polyline);

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

    /// <summary>
    /// Returns a polyline in which every arc sweeping more than a half circle has been replaced by
    /// two halves.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cavalier Contours requires <c>-1 &lt;= bulge &lt;= 1</c>, that is no segment sweeping more
    /// than a half circle. Its own notes state the algorithms rely on that to keep the chord
    /// between the two vertexes, and upstream issue 15 tracks lifting the restriction. Rhino places
    /// no such limit on an arc, so a wider one has to be split before it is handed over. Feeding a
    /// wider sweep in does not fail loudly: the arc is silently treated as its minor complement and
    /// the results are wrong by a wide margin without any error being raised.
    /// </para>
    /// <para>
    /// Both formulas are exact and need no trigonometry. With <c>b = tan(sweep / 4)</c> the two
    /// halves sweep half as far and therefore share the bulge <c>tan(sweep / 8)</c>, which the
    /// tangent half angle identity gives as <c>(sqrt(1 + b^2) - 1) / b</c>. The point where they
    /// meet is the chord midpoint displaced by the sagitta, <c>b / 2</c> times the perpendicular
    /// chord.
    /// </para>
    /// </remarks>
    internal static Polyline<double> SplitArcsWiderThanHalfCircle(Polyline<double> polyline)
    {
        bool anyWiderThanHalfCircle = false;
        for (int i = 0; i < polyline.VertexCount; i++)
        {
            if (Math.Abs(polyline.Get(i).Bulge) > 1.0)
            {
                anyWiderThanHalfCircle = true;
                break;
            }
        }

        if (!anyWiderThanHalfCircle)
            return polyline;

        var result = new Polyline<double>();
        result.SetIsClosed(polyline.IsClosed);

        // The last vertex of an open polyline starts no segment, so its bulge carries no arc.
        int segmentCount = polyline.IsClosed ? polyline.VertexCount : polyline.VertexCount - 1;

        for (int i = 0; i < segmentCount; i++)
        {
            var start = polyline.Get(i);
            var end = polyline.Get((i + 1) % polyline.VertexCount);

            if (Math.Abs(start.Bulge) <= 1.0)
            {
                result.AddVertex(start);
                continue;
            }

            double halfBulge = (Math.Sqrt(1.0 + start.Bulge * start.Bulge) - 1.0) / start.Bulge;
            double midX = (start.X + end.X) / 2.0 + (start.Bulge / 2.0) * (end.Y - start.Y);
            double midY = (start.Y + end.Y) / 2.0 - (start.Bulge / 2.0) * (end.X - start.X);

            result.AddVertex(new PlineVertex<double>(start.X, start.Y, halfBulge));
            result.AddVertex(new PlineVertex<double>(midX, midY, halfBulge));
        }

        if (!polyline.IsClosed)
        {
            result.AddVertex(polyline.Get(polyline.VertexCount - 1));
        }

        return result;
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

        var validCurves = curves.Where(c => c is not null).ToList();
        if (validCurves.Count == 0)
            return new List<Curve>();

        var refPlane = GetPlane(validCurves, plane);
        var results = new List<Curve>();

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

        var plinesA = listA.Select(c => ToPolyline(c, refPlane, tolerance)).Where(IsUsableLoop).ToList();
        var plinesB = listB.Select(c => ToPolyline(c, refPlane, tolerance)).Where(IsUsableLoop).ToList();

        var outputPlines = BooleanPlines(operation, plinesA, plinesB);

        var results = new List<Curve>(outputPlines.Count);
        foreach (var pline in outputPlines)
        {
            results.Add(ToCurve(pline, refPlane));
        }

        return results;
    }

    /// <summary>
    /// Area below which a resulting loop is considered degenerate and dropped. Cavalier's boolean
    /// stitching can emit zero-area slivers (for example when two inputs share an edge exactly);
    /// those would turn into invalid Rhino curves.
    /// </summary>
    private const double DegenerateAreaTolerance = 1e-9;

    private static bool IsUsableLoop(Polyline<double> pline)
        => pline != null
        && pline.IsClosed
        && pline.VertexCount >= 2
        && Math.Abs(pline.Area()) > DegenerateAreaTolerance;

    /// <summary>
    /// Returns a copy of <paramref name="pline"/> wound counter-clockwise for a solid and clockwise
    /// for a hole.
    /// </summary>
    /// <remarks>
    /// Cavalier reports holes through <c>NegPlines</c> but hands them back with their original
    /// winding, so the loop geometry alone does not say whether it is material or a cutout. This
    /// encodes that distinction in the orientation, matching the convention used by
    /// <see cref="CreateShape"/> and by Rhino's own planar boolean output.
    /// </remarks>
    private static Polyline<double> AsOrientedLoop(Polyline<double> pline, bool isSolid)
    {
        var copy = new Polyline<double>(pline.IterVertexes(), pline.IsClosed);
        var wanted = isSolid ? PlineOrientation.CounterClockwise : PlineOrientation.Clockwise;

        if (copy.Orientation() != wanted)
            copy.InvertDirection();

        return copy;
    }

    /// <summary>
    /// Performs a boolean operation between two sets of closed polylines.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each set describes a region the same way <see cref="CreateShape"/> does: loops are classified
    /// by nesting depth, so a loop inside one other loop is a hole, a loop inside a hole is an
    /// island again, and so on. Loops on the same level that overlap are merged. The winding of the
    /// input loops does not matter.
    /// </para>
    /// <para>
    /// Cavalier Contours only provides a boolean between two simple closed loops. The sets are
    /// therefore handled as even-odd parity sets of loops, where a point belongs to the region when
    /// it lies inside an odd number of loops. In that form the operations reduce to pairwise
    /// intersections and a symmetric difference:
    /// <c>A xor B</c>, <c>A and B = xor of all a n b</c>, <c>A not B = A xor (A and B)</c> and
    /// <c>A or B = A xor B xor (A and B)</c>. The parity set is finally resolved into loops that
    /// neither cross nor share edges, which are classified by nesting depth again.
    /// </para>
    /// <para>
    /// The result holds the solids wound counter-clockwise followed by the holes wound clockwise.
    /// </para>
    /// </remarks>
    public static List<Polyline<double>> BooleanPlines(BooleanOp operation, List<Polyline<double>> plinesA, List<Polyline<double>> plinesB)
    {
        ArgumentNullException.ThrowIfNull(plinesA);
        ArgumentNullException.ThrowIfNull(plinesB);

        var boolOpts = new PlineBooleanOptions<double>();

        var regionA = RegionLoops(plinesA, boolOpts);
        var regionB = RegionLoops(plinesB, boolOpts);

        List<Polyline<double>> parity;

        switch (operation)
        {
            case BooleanOp.Or:
                if (IsFlat(regionA) && IsFlat(regionB))
                {
                    // Plain solids on both sides: merging them directly gives the cleanest outline.
                    var (solids, holes) = UnionAll(regionA.Concat(regionB).ToList(), boolOpts);
                    parity = solids.Concat(holes).ToList();
                }
                else
                {
                    parity = regionA.Concat(regionB).Concat(IntersectRegions(regionA, regionB, boolOpts)).ToList();
                }
                break;

            case BooleanOp.And:
                parity = IntersectRegions(regionA, regionB, boolOpts);
                break;

            case BooleanOp.Not:
                parity = regionA.Concat(IntersectRegions(regionA, regionB, boolOpts)).ToList();
                break;

            case BooleanOp.Xor:
                parity = regionA.Concat(regionB).ToList();
                break;

            default:
                parity = new List<Polyline<double>>();
                break;
        }

        return ClassifyLoops(ResolveParity(parity, boolOpts));
    }

    /// <summary>
    /// True when no loop of the resolved region lies inside another, that is the region has no
    /// holes.
    /// </summary>
    private static bool IsFlat(List<Polyline<double>> loops) => NestingDepths(loops).All(d => d == 0);

    /// <summary>
    /// Turns a set of input loops into an equivalent parity set of loops that neither cross nor
    /// share edges.
    /// </summary>
    /// <remarks>
    /// Loops are grouped by nesting depth. Every level is merged on its own, so overlapping loops
    /// on the same level add up instead of cancelling, and the levels are then combined by parity:
    /// holes cut the level above them, islands fill the level above them.
    /// </remarks>
    private static List<Polyline<double>> RegionLoops(List<Polyline<double>> plines, PlineBooleanOptions<double> boolOpts)
    {
        var loops = CounterClockwiseLoops(plines);
        if (loops.Count <= 1)
            return loops;

        var depths = NestingDepths(loops);
        var parity = new List<Polyline<double>>();

        foreach (var level in Enumerable.Range(0, loops.Count).GroupBy(i => depths[i]))
        {
            var (solids, holes) = UnionAll(level.Select(i => loops[i]).ToList(), boolOpts);
            parity.AddRange(solids);
            parity.AddRange(holes);
        }

        return ResolveParity(parity, boolOpts);
    }

    /// <summary>
    /// Intersection of two parity sets: the parity set of all pairwise loop intersections.
    /// </summary>
    private static List<Polyline<double>> IntersectRegions(List<Polyline<double>> loopsA, List<Polyline<double>> loopsB, PlineBooleanOptions<double> boolOpts)
    {
        var result = new List<Polyline<double>>();
        var extentsB = loopsB.Select(Extents).ToArray();

        foreach (var a in loopsA)
        {
            var extentsA = Extents(a);

            for (int j = 0; j < loopsB.Count; j++)
            {
                if (!ExtentsOverlap(extentsA, extentsB[j], boolOpts.PosEqualEps))
                    continue;

                var res = PlineBoolean.PolylineBoolean<Polyline<double>, double>(a, loopsB[j], BooleanOp.And, boolOpts);
                result.AddRange(CounterClockwiseLoops(res.PosPlines.Select(p => p.Pline).Concat(res.NegPlines.Select(p => p.Pline))));
            }
        }

        return result;
    }

    /// <summary>
    /// Rewrites a parity set of loops until no two loops cross or share an edge.
    /// </summary>
    /// <remarks>
    /// Any two interacting loops are replaced by their symmetric difference, which leaves the
    /// parity region unchanged. Crossing loops lose their overlap, loops sharing an edge are welded
    /// into one outline, and identical loops cancel. A replacement is only accepted when it
    /// changes something, so loops that merely touch at points are left alone.
    /// </remarks>
    private static List<Polyline<double>> ResolveParity(List<Polyline<double>> input, PlineBooleanOptions<double> boolOpts)
    {
        var loops = CounterClockwiseLoops(input);
        var extents = loops.Select(Extents).ToList();

        // Every accepted step removes overlap or welds loops; the cap only guards against
        // numerical ping-pong.
        int budget = 1000 + 16 * loops.Count * loops.Count;
        bool changed = true;

        while (changed && budget-- > 0)
        {
            changed = false;

            for (int i = 0; i < loops.Count && !changed; i++)
            {
                for (int j = i + 1; j < loops.Count; j++)
                {
                    if (!ExtentsOverlap(extents[i], extents[j], boolOpts.PosEqualEps))
                        continue;

                    var res = PlineBoolean.PolylineBoolean<Polyline<double>, double>(loops[i], loops[j], BooleanOp.Xor, boolOpts);

                    List<Polyline<double>> replacement;
                    if (res.ResultInfo == BooleanResultInfo.Overlapping)
                    {
                        replacement = new List<Polyline<double>>();
                    }
                    else if (res.ResultInfo == BooleanResultInfo.Intersected)
                    {
                        replacement = CounterClockwiseLoops(res.PosPlines.Select(p => p.Pline).Concat(res.NegPlines.Select(p => p.Pline)));

                        double before = Math.Abs(loops[i].Area()) + Math.Abs(loops[j].Area());
                        double eps = 1e-7 * Math.Max(1.0, before);

                        if (replacement.Count >= 2 && Math.Abs(before - replacement.Sum(p => Math.Abs(p.Area()))) <= eps)
                        {
                            // Nothing overlaps, the loops only touch. The symmetric difference
                            // hands both back unchanged, so weld loops sharing an edge with a union.
                            var welded = WeldTouching(loops[i], loops[j], before, eps, boolOpts);
                            if (welded is null)
                                continue;

                            replacement = welded;
                        }
                    }
                    else
                    {
                        continue;
                    }

                    loops.RemoveAt(j);
                    loops.RemoveAt(i);
                    extents.RemoveAt(j);
                    extents.RemoveAt(i);
                    loops.AddRange(replacement);
                    extents.AddRange(replacement.Select(Extents));
                    changed = true;
                    break;
                }
            }
        }

        return loops;
    }

    /// <summary>
    /// Union of two loops whose interiors do not overlap, or <see langword="null"/> when the union
    /// does not join them into a single outline (for example when they only touch at points).
    /// </summary>
    private static List<Polyline<double>>? WeldTouching(Polyline<double> a, Polyline<double> b, double coveredArea, double eps, PlineBooleanOptions<double> boolOpts)
    {
        var res = PlineBoolean.PolylineBoolean<Polyline<double>, double>(a, b, BooleanOp.Or, boolOpts);
        if (res.ResultInfo != BooleanResultInfo.Intersected)
            return null;

        var pos = res.PosPlines.Select(p => p.Pline).Where(IsUsableLoop).ToList();
        var neg = res.NegPlines.Select(p => p.Pline).Where(IsUsableLoop).ToList();
        if (pos.Count != 1)
            return null;

        double covered = Math.Abs(pos[0].Area()) - neg.Sum(p => Math.Abs(p.Area()));
        if (Math.Abs(covered - coveredArea) > eps)
            return null;

        // Loops sharing an edge lose it twice; loops touching at points keep their full length and
        // would only be pinched into one self touching outline.
        double lengthBefore = a.PathLength() + b.PathLength();
        double lengthAfter = pos[0].PathLength() + neg.Sum(p => p.PathLength());
        if (lengthBefore - lengthAfter <= 1e-6 * lengthBefore)
            return null;

        return CounterClockwiseLoops(pos.Concat(neg));
    }

    /// <summary>
    /// Usable loops, all wound counter-clockwise. Inside a parity set winding carries no meaning,
    /// and the pairwise boolean expects both inputs to share an orientation.
    /// </summary>
    private static List<Polyline<double>> CounterClockwiseLoops(IEnumerable<Polyline<double>> plines)
        => plines.Where(IsUsableLoop).Select(p => AsOrientedLoop(p, isSolid: true)).ToList();

    /// <summary>
    /// Orients loops that neither cross nor share edges by nesting depth and returns the solids
    /// followed by the holes.
    /// </summary>
    private static List<Polyline<double>> ClassifyLoops(List<Polyline<double>> loops)
    {
        var depths = NestingDepths(loops);
        var solids = new List<Polyline<double>>();
        var holes = new List<Polyline<double>>();

        for (int i = 0; i < loops.Count; i++)
        {
            bool isSolid = depths[i] % 2 == 0;
            (isSolid ? solids : holes).Add(AsOrientedLoop(loops[i], isSolid));
        }

        solids.AddRange(holes);
        return solids;
    }

    /// <summary>
    /// Merges overlapping loops until no further merge reduces the loop count, returning the
    /// merged outlines and the holes enclosed by them.
    /// </summary>
    /// <remarks>
    /// The solid count is strictly decreasing per accepted merge, which guarantees termination.
    /// </remarks>
    private static (List<Polyline<double>> Solids, List<Polyline<double>> Holes) UnionAll(List<Polyline<double>> inputPlines, PlineBooleanOptions<double> boolOpts)
    {
        var solids = CounterClockwiseLoops(inputPlines);
        var holes = new List<Polyline<double>>();

        bool changed = true;
        while (changed)
        {
            changed = false;

            for (int i = 0; i < solids.Count && !changed; i++)
            {
                for (int j = i + 1; j < solids.Count; j++)
                {
                    var res = PlineBoolean.PolylineBoolean<Polyline<double>, double>(solids[i], solids[j], BooleanOp.Or, boolOpts);

                    if (res.ResultInfo == BooleanResultInfo.Disjoint || res.ResultInfo == BooleanResultInfo.InvalidInput)
                        continue;

                    var pos = CounterClockwiseLoops(res.PosPlines.Select(p => p.Pline));
                    var neg = CounterClockwiseLoops(res.NegPlines.Select(p => p.Pline));

                    // Accept only when the two solids collapse into at most one solid.
                    // Anything else would leave the solid count unchanged and could loop forever.
                    if (pos.Count > 1)
                        continue;

                    solids.RemoveAt(j);
                    solids.RemoveAt(i);
                    solids.AddRange(pos);
                    holes.AddRange(neg);
                    changed = true;
                    break;
                }
            }
        }

        return (solids, holes);
    }
}
