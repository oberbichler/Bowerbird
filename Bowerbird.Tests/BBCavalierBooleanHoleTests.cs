using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using CavalierContours.Polyline;
using Bowerbird.Crafting;

namespace Bowerbird.Tests;

/// <summary>
/// Booleans where an operand consists of an outer boundary plus inner loops. Nested loops are read
/// by nesting depth, the same way the offset reads them: even depth is material, odd depth is a
/// hole. Reference areas are measured by sampling the analytic shapes on a grid, so they do not
/// depend on CavalierContours.
/// </summary>
public class BBCavalierBooleanHoleTests
{
    private const double HalfWidth = 40;
    private const double HalfHeight = 27;
    private const double CornerRadius = 12;

    // Bulge of a quarter circle arc: tan(90 deg / 4).
    private static readonly double QuarterBulge = Math.Tan(Math.PI / 8);

    private static Polyline<double> RoundedRectangle()
    {
        double x0 = HalfWidth - CornerRadius, y0 = HalfHeight - CornerRadius;
        var p = new Polyline<double>();
        p.SetIsClosed(true);
        p.AddVertex(new PlineVertex<double>(-x0, -HalfHeight, 0));
        p.AddVertex(new PlineVertex<double>(x0, -HalfHeight, QuarterBulge));
        p.AddVertex(new PlineVertex<double>(HalfWidth, -y0, 0));
        p.AddVertex(new PlineVertex<double>(HalfWidth, y0, QuarterBulge));
        p.AddVertex(new PlineVertex<double>(x0, HalfHeight, 0));
        p.AddVertex(new PlineVertex<double>(-x0, HalfHeight, QuarterBulge));
        p.AddVertex(new PlineVertex<double>(-HalfWidth, y0, 0));
        p.AddVertex(new PlineVertex<double>(-HalfWidth, -y0, QuarterBulge));
        return p;
    }

    private static bool InRoundedRectangle(double x, double y)
    {
        double ax = Math.Abs(x), ay = Math.Abs(y);
        if (ax > HalfWidth || ay > HalfHeight) return false;
        double dx = ax - (HalfWidth - CornerRadius), dy = ay - (HalfHeight - CornerRadius);
        return dx <= 0 || dy <= 0 || dx * dx + dy * dy <= CornerRadius * CornerRadius;
    }

    private sealed record Circle(double X, double Y, double R)
    {
        public bool Contains(double x, double y) => (x - X) * (x - X) + (y - Y) * (y - Y) <= R * R;

        /// <param name="clockwise">Inputs are not required to carry any particular winding.</param>
        public Polyline<double> ToPline(bool clockwise = false)
        {
            double b = clockwise ? -1.0 : 1.0;
            var p = new Polyline<double>();
            p.SetIsClosed(true);
            p.AddVertex(new PlineVertex<double>(X - R, Y, b));
            p.AddVertex(new PlineVertex<double>(X + R, Y, b));
            return p;
        }
    }

    private static readonly Circle Hole = new(-14, 0, 9);

    /// <summary>Rounded rectangle with a circular hole, the hole given clockwise or not.</summary>
    private static List<Polyline<double>> PlateWithHole(bool holeClockwise = false) =>
        new() { RoundedRectangle(), Hole.ToPline(holeClockwise) };

    private static bool InPlate(double x, double y) => InRoundedRectangle(x, y) && !Hole.Contains(x, y);

    /// <summary>Area of a region by midpoint sampling over the box that holds every fixture.</summary>
    private static double SampledArea(Func<double, double, bool> inside)
    {
        const double step = 0.05;
        const double minX = -45, maxX = 50, minY = -32, maxY = 36;
        long hits = 0;
        for (double x = minX + step / 2; x < maxX; x += step)
            for (double y = minY + step / 2; y < maxY; y += step)
                if (inside(x, y)) hits++;
        return hits * step * step;
    }

    private static Func<double, double, bool> Combine(BooleanOp op, Func<double, double, bool> a, Func<double, double, bool> b) => op switch
    {
        BooleanOp.Or => (x, y) => a(x, y) || b(x, y),
        BooleanOp.And => (x, y) => a(x, y) && b(x, y),
        BooleanOp.Not => (x, y) => a(x, y) && !b(x, y),
        BooleanOp.Xor => (x, y) => a(x, y) ^ b(x, y),
        _ => throw new ArgumentOutOfRangeException(nameof(op)),
    };

    private static int Solids(List<Polyline<double>> loops) => loops.Count(p => p.Orientation() == PlineOrientation.CounterClockwise);
    private static int Holes(List<Polyline<double>> loops) => loops.Count(p => p.Orientation() == PlineOrientation.Clockwise);

    /// <summary>Solids are wound counter clockwise and holes clockwise, so this is the covered area.</summary>
    private static double NetArea(List<Polyline<double>> loops) => loops.Sum(p => p.Area());

    private static void AssertResult(
        BooleanOp op,
        List<Polyline<double>> a, Func<double, double, bool> inA,
        List<Polyline<double>> b, Func<double, double, bool> inB,
        int expectedSolids, int expectedHoles)
    {
        var result = BBCavalier.BooleanPlines(op, a, b);

        Assert.Equal(SampledArea(Combine(op, inA, inB)), NetArea(result), 1.0);
        Assert.Equal(expectedSolids, Solids(result));
        Assert.Equal(expectedHoles, Holes(result));
    }

    // B overlaps the plate at its top right corner and stays clear of the hole.
    private static readonly Circle Corner = new(22, 10, 22);

    [Theory]
    [InlineData(BooleanOp.Or, 1, 1)]
    [InlineData(BooleanOp.And, 1, 0)]
    [InlineData(BooleanOp.Not, 1, 1)]
    [InlineData(BooleanOp.Xor, 2, 1)]
    public void HoleAwayFromOtherOperandIsKept(BooleanOp op, int solids, int holes)
    {
        AssertResult(op, PlateWithHole(), InPlate, new() { Corner.ToPline() }, Corner.Contains, solids, holes);
    }

    [Fact]
    public void UnionKeepsHoleWhenOtherOperandDoesNotReachIt()
    {
        var result = BBCavalier.BooleanPlines(BooleanOp.Or, PlateWithHole(), new() { Corner.ToPline() });

        Assert.Equal(2, result.Count);
        var hole = Assert.Single(result, p => p.Orientation() == PlineOrientation.Clockwise);
        Assert.Equal(Math.PI * Hole.R * Hole.R, -hole.Area(), 1e-6);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HoleIsRecognisedRegardlessOfInputWinding(bool holeClockwise)
    {
        var result = BBCavalier.BooleanPlines(BooleanOp.Or, PlateWithHole(holeClockwise), new() { Corner.ToPline() });

        Assert.Equal(SampledArea(Combine(BooleanOp.Or, InPlate, Corner.Contains)), NetArea(result), 1.0);
        Assert.Equal(1, Holes(result));
    }

    // B crosses the rim of the hole.
    private static readonly Circle AcrossHole = new(-4, 0, 6);

    [Theory]
    [InlineData(BooleanOp.Or, 1, 1)]
    [InlineData(BooleanOp.And, 1, 0)]
    [InlineData(BooleanOp.Not, 1, 1)]
    [InlineData(BooleanOp.Xor, 1, 2)]
    public void OperandCrossingHole(BooleanOp op, int solids, int holes)
    {
        AssertResult(op, PlateWithHole(), InPlate, new() { AcrossHole.ToPline() }, AcrossHole.Contains, solids, holes);
    }

    // B sits entirely inside the hole.
    private static readonly Circle InsideHole = new(-14, 0, 4);

    [Theory]
    [InlineData(BooleanOp.Or, 2, 1)]
    [InlineData(BooleanOp.And, 0, 0)]
    [InlineData(BooleanOp.Not, 1, 1)]
    [InlineData(BooleanOp.Xor, 2, 1)]
    public void OperandInsideHole(BooleanOp op, int solids, int holes)
    {
        AssertResult(op, PlateWithHole(), InPlate, new() { InsideHole.ToPline() }, InsideHole.Contains, solids, holes);
    }

    // B swallows the hole but stays inside the plate.
    private static readonly Circle AroundHole = new(-14, 0, 12);

    [Theory]
    [InlineData(BooleanOp.Or, 1, 0)]
    [InlineData(BooleanOp.And, 1, 1)]
    [InlineData(BooleanOp.Not, 1, 1)]
    [InlineData(BooleanOp.Xor, 2, 1)]
    public void OperandAroundHole(BooleanOp op, int solids, int holes)
    {
        AssertResult(op, PlateWithHole(), InPlate, new() { AroundHole.ToPline() }, AroundHole.Contains, solids, holes);
    }

    [Theory]
    [InlineData(BooleanOp.Or, 1, 0)]
    [InlineData(BooleanOp.And, 1, 1)]
    [InlineData(BooleanOp.Not, 2, 0)]
    [InlineData(BooleanOp.Xor, 3, 0)]
    public void HoleInSecondOperand(BooleanOp op, int solids, int holes)
    {
        // B is a ring around the corner circle whose inner circle lies inside the plate, so the
        // union fills it again and the difference leaves it behind as a separate piece.
        var ring = new Circle(22, 10, 8);
        bool InRing(double x, double y) => Corner.Contains(x, y) && !ring.Contains(x, y);

        var a = new List<Polyline<double>> { RoundedRectangle() };
        var b = new List<Polyline<double>> { Corner.ToPline(), ring.ToPline() };

        AssertResult(op, a, InRoundedRectangle, b, InRing, solids, holes);
    }

    [Fact]
    public void IslandInsideHoleIsMaterial()
    {
        // Depth two counts as material again: plate, hole, and an island inside the hole.
        var island = new Circle(-14, 0, 3);
        var a = PlateWithHole();
        a.Add(island.ToPline());
        bool InA(double x, double y) => InPlate(x, y) || island.Contains(x, y);

        AssertResult(BooleanOp.Or, a, InA, new() { Corner.ToPline() }, Corner.Contains, 2, 1);
        AssertResult(BooleanOp.Not, a, InA, new() { InsideHole.ToPline() }, InsideHole.Contains, 1, 1);
    }

    [Fact]
    public void OverlappingSolidsInOneOperandAreStillMerged()
    {
        // Loops that cross are not nested, so they stay material and merge as before.
        var c1 = new Circle(0, 0, 10);
        var c2 = new Circle(12, 0, 10);
        var a = new List<Polyline<double>> { c1.ToPline(), c2.ToPline() };

        var result = BBCavalier.BooleanPlines(BooleanOp.Or, a, new List<Polyline<double>>());

        var single = Assert.Single(result);
        Assert.Equal(SampledArea((x, y) => c1.Contains(x, y) || c2.Contains(x, y)), single.Area(), 1.0);
    }
}
