using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public void TestMultiCurveDifferenceTwoHoles()
    {
        // Solid square: [-20, 20] x [-20, 20]
        var solid = new Polyline<double>();
        solid.SetIsClosed(true);
        solid.AddVertex(new PlineVertex<double>(-20.0, -20.0, 0.0));
        solid.AddVertex(new PlineVertex<double>(20.0, -20.0, 0.0));
        solid.AddVertex(new PlineVertex<double>(20.0, 20.0, 0.0));
        solid.AddVertex(new PlineVertex<double>(-20.0, 20.0, 0.0));

        // Hole 1: Circle at (-8, 0) radius 4
        var hole1 = new Polyline<double>();
        hole1.SetIsClosed(true);
        hole1.AddVertex(new PlineVertex<double>(-12.0, 0.0, 1.0));
        hole1.AddVertex(new PlineVertex<double>(-4.0, 0.0, 1.0));

        // Hole 2: Circle at (8, 0) radius 4
        var hole2 = new Polyline<double>();
        hole2.SetIsClosed(true);
        hole2.AddVertex(new PlineVertex<double>(4.0, 0.0, 1.0));
        hole2.AddVertex(new PlineVertex<double>(12.0, 0.0, 1.0));

        var booleanOpts = new PlineBooleanOptions<double>();

        // Step 1: solid minus hole1 -> 1 pos (outer boundary), 1 neg (hole1)
        var res1 = PlineBoolean.PolylineBoolean<Polyline<double>, double>(solid, hole1, BooleanOp.Not, booleanOpts);
        Assert.Single(res1.PosPlines);
        Assert.Single(res1.NegPlines);

        // Step 2: solid minus hole2 -> 1 pos, 1 neg (hole2)
        var res2 = PlineBoolean.PolylineBoolean<Polyline<double>, double>(solid, hole2, BooleanOp.Not, booleanOpts);
        Assert.Single(res2.PosPlines);
        Assert.Single(res2.NegPlines);
    }

    [Fact]
    public void TestMultiCurveUnionFormingHole()
    {
        // 4 overlapping bars forming a square frame with a center hole
        // Bottom bar: [-10, 10] x [-10, -5]
        var b1 = new Polyline<double>();
        b1.SetIsClosed(true);
        b1.AddVertex(new PlineVertex<double>(-10.0, -10.0, 0.0));
        b1.AddVertex(new PlineVertex<double>(10.0, -10.0, 0.0));
        b1.AddVertex(new PlineVertex<double>(10.0, -5.0, 0.0));
        b1.AddVertex(new PlineVertex<double>(-10.0, -5.0, 0.0));

        // Top bar: [-10, 10] x [5, 10]
        var b2 = new Polyline<double>();
        b2.SetIsClosed(true);
        b2.AddVertex(new PlineVertex<double>(-10.0, 5.0, 0.0));
        b2.AddVertex(new PlineVertex<double>(10.0, 5.0, 0.0));
        b2.AddVertex(new PlineVertex<double>(10.0, 10.0, 0.0));
        b2.AddVertex(new PlineVertex<double>(-10.0, 10.0, 0.0));

        // Left bar: [-10, -5] x [-10, 10]
        var b3 = new Polyline<double>();
        b3.SetIsClosed(true);
        b3.AddVertex(new PlineVertex<double>(-10.0, -10.0, 0.0));
        b3.AddVertex(new PlineVertex<double>(-5.0, -10.0, 0.0));
        b3.AddVertex(new PlineVertex<double>(-5.0, 10.0, 0.0));
        b3.AddVertex(new PlineVertex<double>(-10.0, 10.0, 0.0));

        // Right bar: [5, 10] x [-10, 10]
        var b4 = new Polyline<double>();
        b4.SetIsClosed(true);
        b4.AddVertex(new PlineVertex<double>(5.0, -10.0, 0.0));
        b4.AddVertex(new PlineVertex<double>(10.0, -10.0, 0.0));
        b4.AddVertex(new PlineVertex<double>(10.0, 10.0, 0.0));
        b4.AddVertex(new PlineVertex<double>(5.0, 10.0, 0.0));

        var booleanOpts = new PlineBooleanOptions<double>();

        // Union b1 and b3 (L-shape)
        var u1 = PlineBoolean.PolylineBoolean<Polyline<double>, double>(b1, b3, BooleanOp.Or, booleanOpts);
        Assert.Single(u1.PosPlines);
        Assert.Empty(u1.NegPlines);

        // Union (b1+b3) and b2 (U-shape)
        var u2 = PlineBoolean.PolylineBoolean<Polyline<double>, double>(u1.PosPlines[0].Pline, b2, BooleanOp.Or, booleanOpts);
        Assert.Single(u2.PosPlines);
        Assert.Empty(u2.NegPlines);

        // Union (b1+b3+b2) and b4 (Closed frame with central hole)
        var u3 = PlineBoolean.PolylineBoolean<Polyline<double>, double>(u2.PosPlines[0].Pline, b4, BooleanOp.Or, booleanOpts);
        Assert.Single(u3.PosPlines); // Outer boundary [-10, 10]
        Assert.Single(u3.NegPlines); // Inner hole [-5, 5]
    }

    [Fact]
    public void TestMultiCurveIntersection()
    {
        // Boundary A: large square [-50, 50] x [-50, 50]
        var boundary = new Polyline<double>();
        boundary.SetIsClosed(true);
        boundary.AddVertex(new PlineVertex<double>(-50.0, -50.0, 0.0));
        boundary.AddVertex(new PlineVertex<double>(50.0, -50.0, 0.0));
        boundary.AddVertex(new PlineVertex<double>(50.0, 50.0, 0.0));
        boundary.AddVertex(new PlineVertex<double>(-50.0, 50.0, 0.0));

        // Shape B1: circle at (-20, 0) r=10 (partially overlaps boundary if placed at (-50, 0))
        var b1 = new Polyline<double>();
        b1.SetIsClosed(true);
        b1.AddVertex(new PlineVertex<double>(-60.0, 0.0, 1.0));
        b1.AddVertex(new PlineVertex<double>(-40.0, 0.0, 1.0));

        // Shape B2: square at (0, 0) [-10, 10] (fully inside)
        var b2 = new Polyline<double>();
        b2.SetIsClosed(true);
        b2.AddVertex(new PlineVertex<double>(-10.0, -10.0, 0.0));
        b2.AddVertex(new PlineVertex<double>(10.0, -10.0, 0.0));
        b2.AddVertex(new PlineVertex<double>(10.0, 10.0, 0.0));
        b2.AddVertex(new PlineVertex<double>(-10.0, 10.0, 0.0));

        // Shape B3: circle at (50, 0) r=10 (partially overlaps boundary)
        var b3 = new Polyline<double>();
        b3.SetIsClosed(true);
        b3.AddVertex(new PlineVertex<double>(40.0, 0.0, 1.0));
        b3.AddVertex(new PlineVertex<double>(60.0, 0.0, 1.0));

        var booleanOpts = new PlineBooleanOptions<double>();
        var results = new List<Polyline<double>>();

        foreach (var b in new[] { b1, b2, b3 })
        {
            var res = PlineBoolean.PolylineBoolean<Polyline<double>, double>(boundary, b, BooleanOp.And, booleanOpts);
            foreach (var p in res.PosPlines) results.Add(p.Pline);
        }

        // All 3 shapes produce an intersection with the boundary
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void TestBooleanArgumentValidation()
    {
        // RhinoCommon's native library is unavailable outside the Rhino host process, so these
        // assertions deliberately stay on paths that return before any Rhino geometry is touched.
        Assert.Throws<ArgumentNullException>(() => BBCavalier.Boolean(BooleanOp.Or, null!, [], Plane.WorldXY, 0.01));
        Assert.Empty(BBCavalier.Boolean(BooleanOp.Or, [], [], null, 0.01));
        Assert.Empty(BBCavalier.Boolean(BooleanOp.Or, [], null!, null, 0.01));

        Assert.Throws<ArgumentNullException>(() => BBCavalier.BooleanPlines(BooleanOp.Or, null!, []));
        Assert.Throws<ArgumentNullException>(() => BBCavalier.BooleanPlines(BooleanOp.Or, [], null!));
    }

    // --- BooleanPlines: the Rhino-independent core of BBCavalier.Boolean ---

    private static Polyline<double> Rect(double x0, double y0, double x1, double y1)
    {
        var p = new Polyline<double>();
        p.SetIsClosed(true);
        p.AddVertex(new PlineVertex<double>(x0, y0, 0.0));
        p.AddVertex(new PlineVertex<double>(x1, y0, 0.0));
        p.AddVertex(new PlineVertex<double>(x1, y1, 0.0));
        p.AddVertex(new PlineVertex<double>(x0, y1, 0.0));
        return p;
    }

    private static Polyline<double> Circ(double cx, double cy, double r)
    {
        var p = new Polyline<double>();
        p.SetIsClosed(true);
        p.AddVertex(new PlineVertex<double>(cx - r, cy, 1.0));
        p.AddVertex(new PlineVertex<double>(cx + r, cy, 1.0));
        return p;
    }

    private static void AssertNoDegenerateLoops(List<Polyline<double>> loops)
    {
        foreach (var loop in loops)
        {
            Assert.True(loop.VertexCount >= 2, "result loop has fewer than 2 vertexes");
            Assert.True(Math.Abs(loop.Area()) > 1e-9, $"result loop has degenerate area {loop.Area()}");
        }
    }

    [Fact]
    public void BooleanPlines_UnionOfOverlappingCircles_MergesIntoOneLoop()
    {
        var result = BBCavalier.BooleanPlines(BooleanOp.Or, [Circ(-3, 0, 5)], [Circ(3, 0, 5)]);

        Assert.Single(result);
        AssertNoDegenerateLoops(result);
    }

    [Fact]
    public void BooleanPlines_UnionOfDisjointCircles_KeepsBothLoops()
    {
        var result = BBCavalier.BooleanPlines(BooleanOp.Or, [Circ(-20, 0, 5)], [Circ(20, 0, 5)]);

        Assert.Equal(2, result.Count);
        AssertNoDegenerateLoops(result);
    }

    [Fact]
    public void BooleanPlines_UnionOfEdgeAdjacentRectangles_EmitsNoZeroAreaSliver()
    {
        // Cavalier's Or on two rectangles sharing the edge x=10 returns the merged outline plus a
        // zero-area two-vertex sliver. The sliver must never reach the output.
        var result = BBCavalier.BooleanPlines(BooleanOp.Or, [Rect(0, 0, 10, 10)], [Rect(10, 0, 20, 10)]);

        AssertNoDegenerateLoops(result);
        Assert.Single(result);
        Assert.Equal(200.0, result[0].Area(), 1e-6);
    }

    [Fact]
    public void BooleanPlines_UnionFormingRing_ReturnsOuterLoopAndHole()
    {
        // Four bars welded into a square frame: one solid outline plus one clockwise hole.
        var bars = new List<Polyline<double>>
        {
            Rect(-10, -10, 10, -5),
            Rect(-10, 5, 10, 10),
            Rect(-10, -10, -5, 10),
            Rect(5, -10, 10, 10)
        };

        var result = BBCavalier.BooleanPlines(BooleanOp.Or, bars, []);

        AssertNoDegenerateLoops(result);
        Assert.Equal(2, result.Count);
        Assert.Single(result, p => p.Orientation() == PlineOrientation.CounterClockwise);
        Assert.Single(result, p => p.Orientation() == PlineOrientation.Clockwise);
    }

    [Fact]
    public void BooleanPlines_DifferenceWithTwoDisjointHoles_KeepsOneOutlineAndTwoHoles()
    {
        // Regression: holes produced by the first clip must not be clipped again as if they were
        // solid material, and the outline must not be duplicated per clip.
        var solid = new List<Polyline<double>> { Rect(-20, -20, 20, 20) };
        var clips = new List<Polyline<double>> { Circ(-8, 0, 4), Circ(8, 0, 4) };

        var result = BBCavalier.BooleanPlines(BooleanOp.Not, solid, clips);

        AssertNoDegenerateLoops(result);
        Assert.Equal(3, result.Count);
        Assert.Single(result, p => p.Orientation() == PlineOrientation.CounterClockwise);
        Assert.Equal(2, result.Count(p => p.Orientation() == PlineOrientation.Clockwise));
        Assert.Equal(1600.0 - 2 * Math.PI * 16.0, result.Sum(p => p.Area()), 1e-3);
    }

    [Fact]
    public void BooleanPlines_IntersectionWithSeparateShapes_ReturnsEveryOverlap()
    {
        // Regression for the reported defect: only a single overlap survived.
        var boundary = new List<Polyline<double>> { Rect(-50, -50, 50, 50) };
        var shapes = new List<Polyline<double>>
        {
            Circ(-50, 0, 10),      // straddles the left edge, so only half of it counts
            Rect(-10, -10, 10, 10) // fully inside
        };

        var result = BBCavalier.BooleanPlines(BooleanOp.And, boundary, shapes);

        AssertNoDegenerateLoops(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(Math.PI * 100.0 / 2.0 + 400.0, result.Sum(p => Math.Abs(p.Area())), 1e-3);
    }

    [Fact]
    public void BooleanPlines_IntersectionWithMutuallyOverlappingShapes_CoversTheirWholeUnion()
    {
        // Every shape of B lies inside A, so A n B must cover exactly the same area as B on its own.
        // This is the configuration the reported defect showed: B's members overlap each other, and
        // collapsing them with a lossy pre-union silently drops material.
        var boundary = new List<Polyline<double>> { Rect(-50, -50, 50, 50) };
        var shapes = new List<Polyline<double>>
        {
            Circ(-6, 0, 10),
            Circ(0, 0, 10),
            Circ(6, 0, 10),
            Circ(0, 8, 10)
        };

        var intersection = BBCavalier.BooleanPlines(BooleanOp.And, boundary, shapes);
        var shapesAlone = BBCavalier.BooleanPlines(BooleanOp.Or, shapes, []);

        AssertNoDegenerateLoops(intersection);
        Assert.NotEmpty(intersection);
        Assert.Equal(shapesAlone.Sum(p => p.Area()), intersection.Sum(p => p.Area()), 1e-6);
    }

    [Fact]
    public void BooleanPlines_IntersectionOfDisjointShapes_ReturnsNothing()
    {
        var result = BBCavalier.BooleanPlines(BooleanOp.And, [Circ(-20, 0, 5)], [Circ(20, 0, 5)]);

        Assert.Empty(result);
    }

    [Fact]
    public void BooleanPlines_XorOfOverlappingCircles_ReturnsTwoLunes()
    {
        var result = BBCavalier.BooleanPlines(BooleanOp.Xor, [Circ(-3, 0, 5)], [Circ(3, 0, 5)]);

        AssertNoDegenerateLoops(result);
        Assert.Equal(2, result.Count);
    }

    [Fact(Timeout = 30000)]
    public async Task BooleanPlines_UnionOfEdgeAdjacentGrid_Terminates()
    {
        // Edge-adjacent tiles are the configuration where the pairwise Or does not reduce the loop
        // count. Without the "a merge must reduce the solid count" rule this loops forever and
        // would hang Rhino, so the timeout is the assertion that matters here.
        var tiles = new List<Polyline<double>>();
        for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
                tiles.Add(Rect(x * 10, y * 10, (x + 1) * 10, (y + 1) * 10));

        var result = await Task.Run(() => BBCavalier.BooleanPlines(BooleanOp.Or, tiles, []));

        AssertNoDegenerateLoops(result);
        Assert.NotEmpty(result);
        Assert.Equal(1600.0, result.Sum(p => Math.Abs(p.Area())), 1e-6);
    }
}

