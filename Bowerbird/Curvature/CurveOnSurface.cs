using Rhino.Geometry;
using System;

namespace Bowerbird.Curvature
{
    public class CurveOnSurface : IOrientableCurve
    {
        private CurveOnSurface(Surface surface, Curve curve)
        {
            Surface = surface ?? throw new ArgumentNullException(nameof(surface));
            Curve = curve ?? throw new ArgumentNullException(nameof(curve));
        }

        public static CurveOnSurface Create(Surface surface, Curve curve)
        {
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
            var x2 = u2 * s10 + v2 * s01 + Math.Pow(u1, 2) * s20 + Math.Pow(v1, 2) * s02 + 2 * u1 * v1 * s11;

            return (x2 * x1.SquareLength - x1 * (x1 * x2)) / Math.Pow(x1.SquareLength, 2);
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
            var x2 = u2 * s10 + v2 * s01 + Math.Pow(u1, 2) * s20 + Math.Pow(v1, 2) * s02 + 2 * u1 * v1 * s11;

            var n = Vector3d.CrossProduct(s10, s01);
            n.Unitize();

            var c = (x2 * x1.SquareLength - x1 * (x1 * x2)) / Math.Pow(x1.SquareLength, 2);

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
            var x2 = u2 * s10 + v2 * s01 + Math.Pow(u1, 2) * s20 + Math.Pow(v1, 2) * s02 + 2 * u1 * v1 * s11;

            var b = Vector3d.CrossProduct(Vector3d.CrossProduct(s10, s01), x1);
            b.Unitize();

            var c = (x2 * x1.SquareLength - x1 * (x1 * x2)) / Math.Pow(x1.SquareLength, 2);

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

            var n10 = (cross * cross * cross10 - cross * cross10 * cross) / Math.Pow(cross.SquareLength, 1.5);
            var n01 = (cross * cross * cross01 - cross * cross01 * cross) / Math.Pow(cross.SquareLength, 1.5);

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
            var x2 = s20 * Math.Pow(u1, 2) + s10 * u2 + 2 * s12 * u1 * v1 + s02 * Math.Pow(v1, 2) + s01 * v2;
            var x3 = s10 * u3 + 3 * s12 * u1 * Math.Pow(v1, 2) + s03 * Math.Pow(v1, 3) + 3 * v1 * (s21 * Math.Pow(u1, 2) + s11 * u2 + s02 * v2) + u1 * (s30 * Math.Pow(u1, 2) + 3 * s20 * u2 + 3 * s11 * v2) + s01 * v3;

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

        public bool IsValid => Curve.IsValid && Surface.IsValid;

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
    }
}
