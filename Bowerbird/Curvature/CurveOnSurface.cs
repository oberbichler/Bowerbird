using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using static System.Math;

namespace Bowerbird.Curvature
{
    public class CurveOnSurface : IOrientableCurve
    {
        private CurveOnSurface(Surface surface, Curve curve)
        {
            Surface = (Surface)surface?.DuplicateShallow() ?? throw new ArgumentNullException(nameof(surface));
            Curve = (Curve)curve?.DuplicateShallow() ?? throw new ArgumentNullException(nameof(curve));
        }

        public static CurveOnSurface Create(Surface surface, Curve curve)
        {
            if (!surface.IsValid || !curve.IsValid || curve.Domain.Length == 0)
                return null;
            return new CurveOnSurface(surface, curve);
        }

        public Surface Surface { get; }

        public Curve Curve { get; }

        public Interval Domain => Curve.Domain;

        public Point3d PointAt(double t)
        {
            var uv = Curve.PointAt(t);
            return Surface.PointAt(uv.X, uv.Y);
        }

        public Vector3d TangentAt(double t)
        {
            var c_t = Curve.TangentAt(t);
            var uv = Curve.PointAt(t);
            Surface.Evaluate(uv.X, uv.Y, 1, out var _, out var s_uv);
            var x_t = c_t.X * s_uv[0] + c_t.Y * s_uv[1];
            return x_t / x_t.Length;
        }

        public Vector3d NormalAt(double t)
        {
            var uv = Curve.PointAt(t);
            return Surface.NormalAt(uv.X, uv.Y);
        }

        public Vector3d BinormalAt(double t)
        {
            var normal = NormalAt(t);
            var tangent = TangentAt(t);
            return Vector3d.CrossProduct(normal, tangent) / tangent.Length;
        }

        public Vector3d CurvatureAt(double t)
        {
            var uv = Curve.DerivativeAt(t, 2);
            var u = uv[0].X;
            var v = uv[0].Y;
            var u1 = uv[1].X;
            var v1 = uv[1].Y;
            var u2 = uv[2].X;
            var v2 = uv[2].Y;

            Surface.Evaluate(u, v, 2, out var _, out var x);
            var s10 = x[0];
            var s01 = x[1];
            var s20 = x[2];
            var s11 = x[3];
            var s02 = x[4];

            var x1 = u1 * s10 + v1 * s01;
            var x2 = u2 * s10 + v2 * s01 + Pow(u1, 2) * s20 + Pow(v1, 2) * s02 + 2 * u1 * v1 * s11;

            return (x2 * x1.SquareLength - x1 * (x1 * x2)) / Pow(x1.SquareLength, 2);
        }

        public bool ClosestPoint(Point3d sample, out double t)
        {
            if (Surface.ClosestPoint(sample, out var u, out var v) && Curve.ClosestPoint(new Point3d(u, v, 0), out t))
                return true;

            t = 0.0;
            return false;
        }

        public Vector3d NormalCurvatureAt(double t)
        {
            var uv = Curve.DerivativeAt(t, 2);
            var u = uv[0].X;
            var v = uv[0].Y;
            var u1 = uv[1].X;
            var v1 = uv[1].Y;
            var u2 = uv[2].X;
            var v2 = uv[2].Y;

            Surface.Evaluate(u, v, 2, out var _, out var x);
            var s10 = x[0];
            var s01 = x[1];
            var s20 = x[2];
            var s11 = x[3];
            var s02 = x[4];

            var x1 = u1 * s10 + v1 * s01;
            var x2 = u2 * s10 + v2 * s01 + Pow(u1, 2) * s20 + Pow(v1, 2) * s02 + 2 * u1 * v1 * s11;

            var n = Vector3d.CrossProduct(s10, s01);
            n.Unitize();

            var c = (x2 * x1.SquareLength - x1 * (x1 * x2)) / Pow(x1.SquareLength, 2);

            return c * n * n; //x2 * n / x1.SquareLength * n;
        }

        public Vector3d GeodesicCurvatureAt(double t)
        {
            var uv = Curve.DerivativeAt(t, 2);
            var u = uv[0].X;
            var v = uv[0].Y;
            var u1 = uv[1].X;
            var v1 = uv[1].Y;
            var u2 = uv[2].X;
            var v2 = uv[2].Y;

            Surface.Evaluate(u, v, 2, out var _, out var xyz);
            var s10 = xyz[0];
            var s01 = xyz[1];
            var s20 = xyz[2];
            var s11 = xyz[3];
            var s02 = xyz[4];

            var x1 = u1 * s10 + v1 * s01;
            var x2 = u2 * s10 + v2 * s01 + Pow(u1, 2) * s20 + Pow(v1, 2) * s02 + 2 * u1 * v1 * s11;

            var b = Vector3d.CrossProduct(Vector3d.CrossProduct(s10, s01), x1);
            b.Unitize();

            var c = (x2 * x1.SquareLength - x1 * (x1 * x2)) / Pow(x1.SquareLength, 2);

            return c * b * b;
        }

        public Vector3d GeodesicTorsionAt(double t)
        {
            var uv = Curve.DerivativeAt(t, 3);
            var u = uv[0].X;
            var v = uv[0].Y;
            var u1 = uv[1].X;
            var v1 = uv[1].Y;

            Surface.Evaluate(u, v, 3, out var _, out var xyz);
            var s10 = xyz[0];
            var s01 = xyz[1];
            var s20 = xyz[2];
            var s11 = xyz[3];
            var s02 = xyz[4];

            var cross = Vector3d.CrossProduct(s10, s01);
            var cross10 = Vector3d.CrossProduct(s10, s11) + Vector3d.CrossProduct(s20, s01);
            var cross01 = Vector3d.CrossProduct(s10, s02) + Vector3d.CrossProduct(s11, s01);

            var n10 = (cross * cross * cross10 - cross * cross10 * cross) / Pow(cross.SquareLength, 1.5);
            var n01 = (cross * cross * cross01 - cross * cross01 * cross) / Pow(cross.SquareLength, 1.5);

            var x1 = u1 * s10 + v1 * s01;
            var n1 = u1 * n10 + v1 * n01;

            var b = Vector3d.CrossProduct(Vector3d.CrossProduct(s10, s01), x1);
            b.Unitize();

            return n1 * b * x1 / x1.SquareLength;
        }

        public double TorsionAt(double t)
        {
            var uv = Curve.DerivativeAt(t, 3);
            var u = uv[0].X;
            var v = uv[0].Y;
            var u1 = uv[1].X;
            var v1 = uv[1].Y;
            var u2 = uv[2].X;
            var v2 = uv[2].Y;
            var u3 = uv[3].X;
            var v3 = uv[3].Y;

            Surface.Evaluate(u, v, 3, out var _, out var xyz);
            var s10 = xyz[0];
            var s01 = xyz[1];
            var s20 = xyz[2];
            var s11 = xyz[3];
            var s02 = xyz[4];
            var s30 = xyz[5];
            var s21 = xyz[6];
            var s12 = xyz[7];
            var s03 = xyz[8];

            var x1 = u1 * s10 + v1 * s01;
            var x2 = s20 * Pow(u1, 2) + s10 * u2 + 2 * s12 * u1 * v1 + s02 * Pow(v1, 2) + s01 * v2;
            var x3 = s10 * u3 + 3 * s12 * u1 * Pow(v1, 2) + s03 * Pow(v1, 3) + 3 * v1 * (s21 * Pow(u1, 2) + s11 * u2 + s02 * v2) + u1 * (s30 * Pow(u1, 2) + 3 * s20 * u2 + 3 * s11 * v2) + s01 * v3;

            var a = Vector3d.CrossProduct(x1, x2);

            return a * x3 / a.SquareLength;
        }

        public Curve ToCurve(double tolerance)
        {
            var curve = Surface.Pushup(Curve, tolerance);

            if (curve == null)
                return null;

            return curve.IsValid ? curve : null;
        }

        public bool IsValid => Curve != null && Curve.IsValid && Curve.Domain.Length > 0 && Surface != null && Surface.IsValid;

        public IOrientableCurve Reparameterized()
        {
            if (Domain.T0 == 0.0 && Domain.T1 == 1.0)
                return this;

            var curve = (Curve)Curve.Duplicate();

            curve.Domain = new Interval(0.0, 1.0);

            var domain = curve.Domain;

            if (domain.T0 != 0.0 || domain.T1 != 1.0)
            {
                curve = curve.ToNurbsCurve();
                curve.Domain = new Interval(0.0, 1.0);
            }

            return Create(Surface, curve);
        }

        public IOrientableCurve Transform(Transform xform)
        {
            var surface = (Surface)Surface.Duplicate();
            surface.Transform(xform);
            return Create(surface, Curve);
        }

        public IOrientableCurve Morph(SpaceMorph xmorph)
        {
            var surface = (Surface)Surface.Duplicate();
            xmorph.Morph(surface);
            return Create(surface, Curve);
        }

        public double Ds(double t)
        {
            var uv = Curve.DerivativeAt(t, 2);
            var u = uv[0].X;
            var v = uv[0].Y;
            var u1 = uv[1].X;
            var v1 = uv[1].Y;

            Surface.Evaluate(u, v, 1, out var _, out var xyz);
            var s10 = xyz[0];
            var s01 = xyz[1];

            var x1 = u1 * s10 + v1 * s01;

            return x1.Length;
        }

        private List<Tuple<double, Point3d>> Tessellation(double tolerance)
        {
            var maxIter = 10;
            var length = 0.0;

            var tessellation = new List<Tuple<double, Point3d>>();

            for (int i = 0; i < Curve.SpanCount; i++)
            {
                var span = Curve.SpanDomain(i);

                var samplePoints = new List<Tuple<double, Point3d>>();

                var f = new Func<double, double>((t) =>
                {
                    var c = Curve.DerivativeAt(t, 1);
                    var c0 = c[0];
                    var c1 = c[1];

                    Surface.Evaluate(c0.X, c0.Y, 1, out var x, out var s);
                    var s1 = c1.X * s[0] + c1.Y * s[1];

                    samplePoints.Add(Tuple.Create(t, x));

                    return s1.Length;
                });

                length += Integrate.Romberg(f, span.T0, span.T1, tolerance, maxIter);

                samplePoints.Sort((a, b) => -a.Item1.CompareTo(b.Item1));

                //var n = Max(Surface.Degree(0), Surface.Degree(1));

                tessellation.Capacity = tessellation.Count + samplePoints.Count * 2;

                while (true)
                {
                    var a = samplePoints[samplePoints.Count - 1];
                    samplePoints.RemoveAt(samplePoints.Count - 1);

                    tessellation.Add(a);

                    if (samplePoints.Count == 0)
                        break;

                    while (true)
                    {
                        var b = samplePoints[samplePoints.Count - 1];

                        var maxDistance = 0.0;
                        var maxPoint = default(Tuple<double, Point3d>);

                        var domain = new Interval(a.Item1, b.Item1);

                        //for (int j = 1; j <= n; j++)
                        //{
                        //    var t = domain.ParameterAt(j / (n + 1.0));
                        var t = domain.ParameterAt(0.5);
                        var point = PointAt(t);

                        var distance = new Line(a.Item2, b.Item2).DistanceTo(point, true);

                        if (distance < maxDistance)
                            continue;

                        maxDistance = distance;
                        maxPoint = Tuple.Create(t, point);
                        //}

                        if (maxDistance < tolerance * 100)
                            break;

                        samplePoints.Add(maxPoint);
                    }
                }
            }

            Debug.Assert(Abs(length - ToCurve(tolerance).GetLength()) < 10 * tolerance);

            return tessellation;
        }

        public Tuple<double, Point3d> Invert(Point3d sample, double tolerance)
        {
            var tessellation = Tessellation(tolerance);

            var polyline = new Polyline(tessellation.Select(o => o.Item2));

            var closestParameter = polyline.ClosestParameter(sample);
            var closestSpan = (int)closestParameter + 1 == tessellation.Count ? (int)closestParameter - 1 : (int)closestParameter;

            var minParameter = tessellation[Max(0, closestSpan - 1)].Item1;
            var maxParameter = tessellation[Min(tessellation.Count - 1, closestSpan + 2)].Item1;

            if (minParameter > maxParameter)
            {
                var tmp = minParameter;
                minParameter = maxParameter;
                maxParameter = tmp;
            }

            var t0 = tessellation[closestSpan].Item1;
            var t1 = tessellation[closestSpan + 1].Item1;

            var t = t0 + (closestParameter - closestSpan) * (t1 - t0);
            var closestPoint = Tuple.Create(t, PointAt(t));

            var maxIterations = 10;

            for (var i = 0; i < maxIterations; i++)
            {
                var c = Curve.DerivativeAt(t, 1);
                var c0 = c[0];
                var c1 = c[1];

                Surface.Evaluate(c0.X, c0.Y, 1, out var x, out var s);
                var a1 = c1.X * s[0] + c1.Y * s[1];

                var r = x - sample;

                if (r.Length < tolerance)
                    break;

                if (Abs(r / r.Length * a1 / a1.Length) < tolerance)
                    break;

                Debug.Assert((a1 / a1.Length - TangentAt(t)).Length < 1e-10);

                var w = r.Length * a1.Length;
                var lhs = a1 * a1;
                var rhs = a1 * r;

                var delta = -rhs / lhs;

                var deltaMin = minParameter - t;
                var deltaMax = maxParameter - t;

                if (delta < deltaMin)
                    delta = deltaMin;
                else if (delta > deltaMax)
                    delta = deltaMax;

                Debug.Assert(t + delta >= minParameter);
                Debug.Assert(t + delta <= maxParameter);

                var alpha = 1.0;

                for (int j = 0; j < 5; j++)
                {
                    var x1 = PointAt(t + alpha * delta);

                    if (sample.DistanceToSquared(x1) <= r.SquareLength)
                        break;

                    alpha /= 2;

                    Debug.Assert(j + 1 < 5);
                }

                var nextParameter = t + alpha * delta;

                t = nextParameter;

                Debug.Assert(i + 1 < maxIterations || t == minParameter || t == maxParameter);
            }

            return Tuple.Create(t, PointAt(t));
        }
    }
}
