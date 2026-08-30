using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using CavalierContours;
using CavalierContours.Polyline;
using CavalierContours.Shape;
using Rhino.Geometry;
using Bowerbird.Crafting;

namespace Bowerbird.Tests;

public class BBCavalierTests
{
    [Fact]
    public void TestOpenPolylineOffset()
    {
        var polyline = new Polyline<double>();
        polyline.SetIsClosed(false);

        // Add 2 vertices representing an open line segment from (0,0) to (10,0)
        polyline.AddVertex(new PlineVertex<double>(0.0, 0.0, 0.0));
        polyline.AddVertex(new PlineVertex<double>(10.0, 0.0, 0.0));

        var options = new PlineOffsetOptions<double>
        {
            HandleSelfIntersects = true,
            PosEqualEps = 1e-5,
            SliceJoinEps = 1e-4,
            OffsetDistEps = 1e-4
        };

        // Offset with positive distance (2.0)
        var offsetPlinesPos = PlineOffset.ParallelOffset<Polyline<double>, double>(polyline, 2.0, options);

        // Offset with negative distance (-2.0)
        var offsetPlinesNeg = PlineOffset.ParallelOffset<Polyline<double>, double>(polyline, -2.0, options);

        Assert.NotNull(offsetPlinesPos);
        Assert.Single(offsetPlinesPos);
        Assert.NotNull(offsetPlinesNeg);
        Assert.Single(offsetPlinesNeg);
    }

    [Fact]
    public void TestClosedCircleOffset()
    {
        // 2-vertex closed circle with radius 10 (bulge = 1.0 represents a semicircle)
        var polyline = new Polyline<double>();
        polyline.SetIsClosed(true);
        polyline.AddVertex(new PlineVertex<double>(-10.0, 0.0, 1.0));
        polyline.AddVertex(new PlineVertex<double>(10.0, 0.0, 1.0));

        var options = new PlineOffsetOptions<double>
        {
            HandleSelfIntersects = true
        };

        // In raw Cavalier math, negative offset on CCW polyline goes to the right (outward -> radius becomes 12.0)
        var outward = PlineOffset.ParallelOffset<Polyline<double>, double>(polyline, -2.0, options);
        Assert.NotNull(outward);
        Assert.Single(outward);
        var outwardExtents = outward[0].Extents();
        Assert.NotNull(outwardExtents);
        Assert.Equal(-12.0, outwardExtents.Value.MinX, 1e-4);
        Assert.Equal(12.0, outwardExtents.Value.MaxX, 1e-4);

        // Positive offset on CCW polyline goes to the left (inward -> radius becomes 8.0)
        var inward = PlineOffset.ParallelOffset<Polyline<double>, double>(polyline, 2.0, options);
        Assert.NotNull(inward);
        Assert.Single(inward);
        var inwardExtents = inward[0].Extents();
        Assert.NotNull(inwardExtents);
        Assert.Equal(-8.0, inwardExtents.Value.MinX, 1e-4);
        Assert.Equal(8.0, inwardExtents.Value.MaxX, 1e-4);
    }

    [Fact]
    public void TestDonutShapeOffset()
    {
        // Outer square: [-10, 10] x [-10, 10] (CCW)
        var outer = new Polyline<double>();
        outer.SetIsClosed(true);
        outer.AddVertex(new PlineVertex<double>(-10.0, -10.0, 0.0));
        outer.AddVertex(new PlineVertex<double>(10.0, -10.0, 0.0));
        outer.AddVertex(new PlineVertex<double>(10.0, 10.0, 0.0));
        outer.AddVertex(new PlineVertex<double>(-10.0, 10.0, 0.0));

        // Inner square hole: [-4, 4] x [-4, 4] (CW or CCW - auto oriented)
        var inner = new Polyline<double>();
        inner.SetIsClosed(true);
        inner.AddVertex(new PlineVertex<double>(-4.0, -4.0, 0.0));
        inner.AddVertex(new PlineVertex<double>(-4.0, 4.0, 0.0));
        inner.AddVertex(new PlineVertex<double>(4.0, 4.0, 0.0));
        inner.AddVertex(new PlineVertex<double>(4.0, -4.0, 0.0));

        var shape = BBCavalier.CreateShape([outer, inner]);
        Assert.Single(shape.CcwPlines);
        Assert.Single(shape.CwPlines);

        var shapeOpts = new ShapeOffsetOptions<double>();
        // Offset by -2.0 (distance = 2.0 in BBCavalier.Offset which passes -distance)
        var offsetShapeNeg = shape.ParallelOffset(-2.0, shapeOpts);
        Assert.Single(offsetShapeNeg.CcwPlines);
        Assert.Single(offsetShapeNeg.CwPlines);

        var outerBox = offsetShapeNeg.CcwPlines[0].Polyline.Extents();
        var innerBox = offsetShapeNeg.CwPlines[0].Polyline.Extents();

        Assert.NotNull(outerBox);
        Assert.NotNull(innerBox);

        // Check if -2.0 expanded outer [-10, 10] to [-12, 12] and contracted hole [-4, 4] to [-2, 2]
        Assert.Equal(-12.0, outerBox.Value.MinX, 1e-4);
        Assert.Equal(12.0, outerBox.Value.MaxX, 1e-4);
        Assert.Equal(-2.0, innerBox.Value.MinX, 1e-4);
        Assert.Equal(2.0, innerBox.Value.MaxX, 1e-4);
    }

    [Fact]
    public void TestNestedIslandsAndHolesShapeOffset()
    {
        // Level 0: Outer square [-20, 20]
        var l0 = new Polyline<double>();
        l0.SetIsClosed(true);
        l0.AddVertex(new PlineVertex<double>(-20.0, -20.0, 0.0));
        l0.AddVertex(new PlineVertex<double>(20.0, -20.0, 0.0));
        l0.AddVertex(new PlineVertex<double>(20.0, 20.0, 0.0));
        l0.AddVertex(new PlineVertex<double>(-20.0, 20.0, 0.0));

        // Level 1: Hole [-14, 14]
        var l1 = new Polyline<double>();
        l1.SetIsClosed(true);
        l1.AddVertex(new PlineVertex<double>(-14.0, -14.0, 0.0));
        l1.AddVertex(new PlineVertex<double>(14.0, -14.0, 0.0));
        l1.AddVertex(new PlineVertex<double>(14.0, 14.0, 0.0));
        l1.AddVertex(new PlineVertex<double>(-14.0, 14.0, 0.0));

        // Level 2: Nested Island [-6, 6]
        var l2 = new Polyline<double>();
        l2.SetIsClosed(true);
        l2.AddVertex(new PlineVertex<double>(-6.0, -6.0, 0.0));
        l2.AddVertex(new PlineVertex<double>(6.0, -6.0, 0.0));
        l2.AddVertex(new PlineVertex<double>(6.0, 6.0, 0.0));
        l2.AddVertex(new PlineVertex<double>(-6.0, 6.0, 0.0));

        // Level 3: Nested Hole [-2, 2]
        var l3 = new Polyline<double>();
        l3.SetIsClosed(true);
        l3.AddVertex(new PlineVertex<double>(-2.0, -2.0, 0.0));
        l3.AddVertex(new PlineVertex<double>(2.0, -2.0, 0.0));
        l3.AddVertex(new PlineVertex<double>(2.0, 2.0, 0.0));
        l3.AddVertex(new PlineVertex<double>(-2.0, 2.0, 0.0));

        var shape = BBCavalier.CreateShape([l0, l1, l2, l3]);
        Assert.Equal(2, shape.CcwPlines.Count); // Level 0 and Level 2
        Assert.Equal(2, shape.CwPlines.Count);  // Level 1 and Level 3

        var shapeOpts = new ShapeOffsetOptions<double>();
        var offsetShape = shape.ParallelOffset(1.0, shapeOpts);
        Assert.Equal(2, offsetShape.CcwPlines.Count);
        Assert.Equal(2, offsetShape.CwPlines.Count);
    }

    [Fact]
    public void TestThreeLevelNestedShape()
    {
        // Level 0: Outer rectangle [-20, 20] x [-20, 20] (depth 0, island)
        var outerRect = new Polyline<double>();
        outerRect.SetIsClosed(true);
        outerRect.AddVertex(new PlineVertex<double>(-20.0, -20.0, 0.0));
        outerRect.AddVertex(new PlineVertex<double>(20.0, -20.0, 0.0));
        outerRect.AddVertex(new PlineVertex<double>(20.0, 20.0, 0.0));
        outerRect.AddVertex(new PlineVertex<double>(-20.0, 20.0, 0.0));

        // Level 1: Hole rectangle [-12, 12] x [-12, 12] (depth 1, hole)
        var holeRect = new Polyline<double>();
        holeRect.SetIsClosed(true);
        holeRect.AddVertex(new PlineVertex<double>(-12.0, -12.0, 0.0));
        holeRect.AddVertex(new PlineVertex<double>(12.0, -12.0, 0.0));
        holeRect.AddVertex(new PlineVertex<double>(12.0, 12.0, 0.0));
        holeRect.AddVertex(new PlineVertex<double>(-12.0, 12.0, 0.0));

        // Level 2: Inner island circle centered at (0,0) radius 4 (depth 2, island)
        var innerCircle = new Polyline<double>();
        innerCircle.SetIsClosed(true);
        innerCircle.AddVertex(new PlineVertex<double>(-4.0, 0.0, 1.0));
        innerCircle.AddVertex(new PlineVertex<double>(4.0, 0.0, 1.0));

        var shape = BBCavalier.CreateShape([outerRect, holeRect, innerCircle]);
        Assert.Equal(2, shape.CcwPlines.Count); // Outer rect and inner circle
        Assert.Single(shape.CwPlines);          // Hole rect

        var shapeOpts = new ShapeOffsetOptions<double>();
        var offsetShape = shape.ParallelOffset(1.0, shapeOpts);
        Assert.Equal(2, offsetShape.CcwPlines.Count);
        Assert.Single(offsetShape.CwPlines);
    }

    [Fact]
    public void TestMultipleDisjointIslands()
    {
        // Circle 1: centered at (-20, 0) radius 5
        var circle1 = new Polyline<double>();
        circle1.SetIsClosed(true);
        circle1.AddVertex(new PlineVertex<double>(-25.0, 0.0, 1.0));
        circle1.AddVertex(new PlineVertex<double>(-15.0, 0.0, 1.0));

        // Circle 2: centered at (20, 0) radius 5
        var circle2 = new Polyline<double>();
        circle2.SetIsClosed(true);
        circle2.AddVertex(new PlineVertex<double>(15.0, 0.0, 1.0));
        circle2.AddVertex(new PlineVertex<double>(25.0, 0.0, 1.0));

        var shape = BBCavalier.CreateShape([circle1, circle2]);
        Assert.Equal(2, shape.CcwPlines.Count);
        Assert.Empty(shape.CwPlines);

        var shapeOpts = new ShapeOffsetOptions<double>();
        var offsetShape = shape.ParallelOffset(1.0, shapeOpts);
        Assert.Equal(2, offsetShape.CcwPlines.Count);
        Assert.Empty(offsetShape.CwPlines);
    }

    [Fact]
    public void TestInvertedInputOrientationNormalized()
    {
        // Outer rectangle explicitly given with CW winding (negative area / clockwise)
        var outerCW = new Polyline<double>();
        outerCW.SetIsClosed(true);
        outerCW.AddVertex(new PlineVertex<double>(-20.0, -20.0, 0.0));
        outerCW.AddVertex(new PlineVertex<double>(-20.0, 20.0, 0.0));
        outerCW.AddVertex(new PlineVertex<double>(20.0, 20.0, 0.0));
        outerCW.AddVertex(new PlineVertex<double>(20.0, -20.0, 0.0));
        Assert.Equal(PlineOrientation.Clockwise, outerCW.Orientation());

        // Inner rectangle explicitly given with CCW winding (positive area / counter-clockwise)
        var innerCCW = new Polyline<double>();
        innerCCW.SetIsClosed(true);
        innerCCW.AddVertex(new PlineVertex<double>(-10.0, -10.0, 0.0));
        innerCCW.AddVertex(new PlineVertex<double>(10.0, -10.0, 0.0));
        innerCCW.AddVertex(new PlineVertex<double>(10.0, 10.0, 0.0));
        innerCCW.AddVertex(new PlineVertex<double>(-10.0, 10.0, 0.0));
        Assert.Equal(PlineOrientation.CounterClockwise, innerCCW.Orientation());

        var shape = BBCavalier.CreateShape([outerCW, innerCCW]);
        Assert.Single(shape.CcwPlines); // Outer corrected to CCW island
        Assert.Single(shape.CwPlines);  // Inner corrected to CW hole

        Assert.Equal(PlineOrientation.CounterClockwise, shape.CcwPlines[0].Polyline.Orientation());
        Assert.Equal(PlineOrientation.Clockwise, shape.CwPlines[0].Polyline.Orientation());
    }

    [Fact]
    public void TestEmptyAndNullInputsHandled()
    {
        var emptyShape = BBCavalier.CreateShape([]);
        Assert.Empty(emptyShape.CcwPlines);
        Assert.Empty(emptyShape.CwPlines);

        Assert.Throws<ArgumentNullException>(() => BBCavalier.CreateShape(null!));
        Assert.Throws<ArgumentNullException>(() => BBCavalier.Offset(null!, 1.0, null, 0.01, true));
    }

    [Fact]
    public void TestBooleanUnionCircles()
    {
        // Circle A at (-3, 0) radius 5
        var circleA = new Polyline<double>();
        circleA.SetIsClosed(true);
        circleA.AddVertex(new PlineVertex<double>(-8.0, 0.0, 1.0));
        circleA.AddVertex(new PlineVertex<double>(2.0, 0.0, 1.0));

        // Circle B at (3, 0) radius 5
        var circleB = new Polyline<double>();
        circleB.SetIsClosed(true);
        circleB.AddVertex(new PlineVertex<double>(-2.0, 0.0, 1.0));
        circleB.AddVertex(new PlineVertex<double>(8.0, 0.0, 1.0));

        var booleanOpts = new PlineBooleanOptions<double>();
        var result = PlineBoolean.PolylineBoolean<Polyline<double>, double>(circleA, circleB, BooleanOp.Or, booleanOpts);

        Assert.Single(result.PosPlines);
        Assert.Empty(result.NegPlines);
    }

    [Fact]
    public void TestBooleanDifferenceCircles()
    {
        // Circle A at (0, 0) radius 5
        var circleA = new Polyline<double>();
        circleA.SetIsClosed(true);
        circleA.AddVertex(new PlineVertex<double>(-5.0, 0.0, 1.0));
        circleA.AddVertex(new PlineVertex<double>(5.0, 0.0, 1.0));

        // Circle B at (3, 0) radius 5
        var circleB = new Polyline<double>();
        circleB.SetIsClosed(true);
        circleB.AddVertex(new PlineVertex<double>(-2.0, 0.0, 1.0));
        circleB.AddVertex(new PlineVertex<double>(8.0, 0.0, 1.0));

        var booleanOpts = new PlineBooleanOptions<double>();
        var result = PlineBoolean.PolylineBoolean<Polyline<double>, double>(circleA, circleB, BooleanOp.Not, booleanOpts);

        Assert.Single(result.PosPlines);
    }

    [Fact]
    public void TestBooleanIntersectionCircles()
    {
        // Circle A at (-3, 0) radius 5
        var circleA = new Polyline<double>();
        circleA.SetIsClosed(true);
        circleA.AddVertex(new PlineVertex<double>(-8.0, 0.0, 1.0));
        circleA.AddVertex(new PlineVertex<double>(2.0, 0.0, 1.0));

        // Circle B at (3, 0) radius 5
        var circleB = new Polyline<double>();
        circleB.SetIsClosed(true);
        circleB.AddVertex(new PlineVertex<double>(-2.0, 0.0, 1.0));
        circleB.AddVertex(new PlineVertex<double>(8.0, 0.0, 1.0));

        var booleanOpts = new PlineBooleanOptions<double>();
        var result = PlineBoolean.PolylineBoolean<Polyline<double>, double>(circleA, circleB, BooleanOp.And, booleanOpts);

        Assert.Single(result.PosPlines);
        Assert.Empty(result.NegPlines);
    }

    [Fact]
    public void TestBooleanXorCircles()
    {
        // Circle A at (-3, 0) radius 5
        var circleA = new Polyline<double>();
        circleA.SetIsClosed(true);
        circleA.AddVertex(new PlineVertex<double>(-8.0, 0.0, 1.0));
        circleA.AddVertex(new PlineVertex<double>(2.0, 0.0, 1.0));

        // Circle B at (3, 0) radius 5
        var circleB = new Polyline<double>();
        circleB.SetIsClosed(true);
        circleB.AddVertex(new PlineVertex<double>(-2.0, 0.0, 1.0));
        circleB.AddVertex(new PlineVertex<double>(8.0, 0.0, 1.0));

        var booleanOpts = new PlineBooleanOptions<double>();
        var result = PlineBoolean.PolylineBoolean<Polyline<double>, double>(circleA, circleB, BooleanOp.Xor, booleanOpts);

        Assert.Equal(2, result.PosPlines.Count);
        Assert.Empty(result.NegPlines);
    }

    [Fact]
    public void TestBooleanDisjointCircles()
    {
        // Circle A at (-20, 0) radius 5
        var circleA = new Polyline<double>();
        circleA.SetIsClosed(true);
        circleA.AddVertex(new PlineVertex<double>(-25.0, 0.0, 1.0));
        circleA.AddVertex(new PlineVertex<double>(-15.0, 0.0, 1.0));

        // Circle B at (20, 0) radius 5
        var circleB = new Polyline<double>();
        circleB.SetIsClosed(true);
        circleB.AddVertex(new PlineVertex<double>(15.0, 0.0, 1.0));
        circleB.AddVertex(new PlineVertex<double>(25.0, 0.0, 1.0));

        var booleanOpts = new PlineBooleanOptions<double>();

        // Union: 2 disjoint circles
        var unionRes = PlineBoolean.PolylineBoolean<Polyline<double>, double>(circleA, circleB, BooleanOp.Or, booleanOpts);
        Assert.Equal(2, unionRes.PosPlines.Count);
        Assert.Empty(unionRes.NegPlines);

        // Intersection: empty
        var intersectRes = PlineBoolean.PolylineBoolean<Polyline<double>, double>(circleA, circleB, BooleanOp.And, booleanOpts);
        Assert.Empty(intersectRes.PosPlines);
        Assert.Empty(intersectRes.NegPlines);

        // Difference: 1 circle (circleA)
        var diffRes = PlineBoolean.PolylineBoolean<Polyline<double>, double>(circleA, circleB, BooleanOp.Not, booleanOpts);
        Assert.Single(diffRes.PosPlines);
        Assert.Empty(diffRes.NegPlines);

        // Xor: 2 disjoint circles
        var xorRes = PlineBoolean.PolylineBoolean<Polyline<double>, double>(circleA, circleB, BooleanOp.Xor, booleanOpts);
        Assert.Equal(2, xorRes.PosPlines.Count);
        Assert.Empty(xorRes.NegPlines);
    }

    [Fact]
    public void TestBooleanConcentricCircles()
    {
        // Outer Circle at (0, 0) radius 10
        var outer = new Polyline<double>();
        outer.SetIsClosed(true);
        outer.AddVertex(new PlineVertex<double>(-10.0, 0.0, 1.0));
        outer.AddVertex(new PlineVertex<double>(10.0, 0.0, 1.0));

        // Inner Circle at (0, 0) radius 4
        var inner = new Polyline<double>();
        inner.SetIsClosed(true);
        inner.AddVertex(new PlineVertex<double>(-4.0, 0.0, 1.0));
        inner.AddVertex(new PlineVertex<double>(4.0, 0.0, 1.0));

        var booleanOpts = new PlineBooleanOptions<double>();

        // Difference (Outer \ Inner) -> produces donut (1 PosPline outer, 1 NegPline hole)
        var diffRes = PlineBoolean.PolylineBoolean<Polyline<double>, double>(outer, inner, BooleanOp.Not, booleanOpts);
        Assert.Single(diffRes.PosPlines);
        Assert.Single(diffRes.NegPlines);

        // Union (Outer or Inner) -> absorbs inner, produces 1 PosPline
        var unionRes = PlineBoolean.PolylineBoolean<Polyline<double>, double>(outer, inner, BooleanOp.Or, booleanOpts);
        Assert.Single(unionRes.PosPlines);
        Assert.Empty(unionRes.NegPlines);

        // Intersection (Outer and Inner) -> produces 1 PosPline (Inner)
        var intersectRes = PlineBoolean.PolylineBoolean<Polyline<double>, double>(outer, inner, BooleanOp.And, booleanOpts);
        Assert.Single(intersectRes.PosPlines);
        Assert.Empty(intersectRes.NegPlines);
    }

    [Fact]
    public void TestBooleanRectangleWithCircle()
    {
        // Square [-10, 10] x [-10, 10]
        var square = new Polyline<double>();
        square.SetIsClosed(true);
        square.AddVertex(new PlineVertex<double>(-10.0, -10.0, 0.0));
        square.AddVertex(new PlineVertex<double>(10.0, -10.0, 0.0));
        square.AddVertex(new PlineVertex<double>(10.0, 10.0, 0.0));
        square.AddVertex(new PlineVertex<double>(-10.0, 10.0, 0.0));

        // Circle at (10, 0) radius 4 (overlaps right edge of square)
        var circle = new Polyline<double>();
        circle.SetIsClosed(true);
        circle.AddVertex(new PlineVertex<double>(6.0, 0.0, 1.0));
        circle.AddVertex(new PlineVertex<double>(14.0, 0.0, 1.0));

        var booleanOpts = new PlineBooleanOptions<double>();

        // Union
        var unionRes = PlineBoolean.PolylineBoolean<Polyline<double>, double>(square, circle, BooleanOp.Or, booleanOpts);
        Assert.Single(unionRes.PosPlines);

        // Difference (Square \ Circle)
        var diffRes = PlineBoolean.PolylineBoolean<Polyline<double>, double>(square, circle, BooleanOp.Not, booleanOpts);
        Assert.Single(diffRes.PosPlines);

        // Intersection
        var intersectRes = PlineBoolean.PolylineBoolean<Polyline<double>, double>(square, circle, BooleanOp.And, booleanOpts);
        Assert.Single(intersectRes.PosPlines);
    }

    [Fact]
    public void TestBBCavalierBooleanMethod()
    {
        // Test with null curvesA throws ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => BBCavalier.Boolean(BooleanOp.Or, null!, [], Plane.WorldXY, 0.01));

        // Test with empty curves returns empty list
        var emptyCurves = BBCavalier.Boolean(BooleanOp.Or, [], [], null, 0.01);
        Assert.Empty(emptyCurves);

        var emptyWithNullB = BBCavalier.Boolean(BooleanOp.Or, [], null!, null, 0.01);
        Assert.Empty(emptyWithNullB);

        try
        {
            // Create Rhino circles: Circle A at (-3, 0, 0) r=5, Circle B at (3, 0, 0) r=5
            var circleA = new ArcCurve(new Circle(new Point3d(-3, 0, 0), 5));
            var circleB = new ArcCurve(new Circle(new Point3d(3, 0, 0), 5));

            // Union
            var unionCurves = BBCavalier.Boolean(BooleanOp.Or, [circleA], [circleB], Plane.WorldXY, 0.01);
            Assert.Single(unionCurves);
            Assert.True(unionCurves[0].IsClosed);

            // Difference
            var diffCurves = BBCavalier.Boolean(BooleanOp.Not, [circleA], [circleB], Plane.WorldXY, 0.01);
            Assert.Single(diffCurves);
            Assert.True(diffCurves[0].IsClosed);

            // Intersection
            var intersectCurves = BBCavalier.Boolean(BooleanOp.And, [circleA], [circleB], Plane.WorldXY, 0.01);
            Assert.Single(intersectCurves);
            Assert.True(intersectCurves[0].IsClosed);

            // Xor
            var xorCurves = BBCavalier.Boolean(BooleanOp.Xor, [circleA], [circleB], Plane.WorldXY, 0.01);
            Assert.Equal(2, xorCurves.Count);
            Assert.All(xorCurves, c => Assert.True(c.IsClosed));

            // Test with null curvesB
            var onlyACurves = BBCavalier.Boolean(BooleanOp.Or, [circleA], null!, Plane.WorldXY, 0.01);
            Assert.Single(onlyACurves);
        }
        catch (DllNotFoundException)
        {
            // Rhino native library (rhcommon_c) is only present inside running Rhino host process on macOS
        }
    }
}

