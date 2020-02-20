using Rhino.Geometry;
using System;

namespace Bowerbird
{
    class OrientedCurve : IOrientableCurve
    {
        public Curve Curve { get; }

        private OrientedCurve(Curve curve)
        {
            Curve = curve;
        }

        public static OrientedCurve Create(Curve curve)
        {
            var curveDuplicate = curve.DuplicateCurve();

            return new OrientedCurve(curveDuplicate);
        }

        public Interval Domain => Curve.Domain;

        public Plane FrameAt(double t)
        {
            var t0 = Curve.Domain.T0;

            var theta = Integrate.AdaptiveSimpson(TorsionAt, t0, t, 0.001);

            var c = Curve.DerivativeAt(t, 3);

            var a = Vector3d.CrossProduct(c[1], c[2]);
            var d = a * c[3];
            var delta = c[1].Length;
            var alpha = a.Length;

            var P = c[0];
            var T = c[1] / delta;
            var B = a / alpha;
            var N = Vector3d.CrossProduct(B, T);

            var e2 = N * Math.Cos(theta) + B * Math.Sin(theta);
            var e3 = B * Math.Cos(theta) - N * Math.Sin(theta);

            return new Plane(new Point3d(P), e2, e3);
        }

        public Point3d PointAt(double t)
        {
            return Curve.PointAt(t);
        }

        public Vector3d TangentAt(double t)
        {
            return Curve.TangentAt(t);
        }

        public Vector3d NormalAt(double t)
        {
            return FrameAt(t).YAxis;
        }

        public Vector3d BinormalAt(double t)
        {
            return FrameAt(t).XAxis;
        }

        public Vector3d CurvatureAt(double t)
        {
            var c = Curve.DerivativeAt(t, 2);

            var x1 = c[1];
            var x2 = c[2];

            return (x2 * x1.SquareLength - x1 * (x1 * x2)) / Math.Pow(x1.SquareLength, 2);
        }

        public bool ClosestPoint(Point3d sample, out double t)
        {
            if (Curve.ClosestPoint(sample, out t))
                return true;

            t = default;
            return false;
        }

        public Vector3d NormalCurvatureAt(double t)
        {
            var k = CurvatureAt(t);
            var n = NormalAt(t);
            n.Unitize();

            return k * n * n;
        }

        public Vector3d GeodesicCurvatureAt(double t)
        {
            var k = CurvatureAt(t);
            var b = BinormalAt(t);
            b.Unitize();

            return k * b * b;
        }

        public Vector3d GeodesicTorsionAt(double t)
        {
            var c = Curve.DerivativeAt(t, 4);

            var theta = Integrate.AdaptiveSimpson(u => TorsionAt(u), Curve.Domain.T0, t, 0.001);
            var theta_1 = TorsionAt(t);

            var a = Vector3d.CrossProduct(c[1], c[2]);
            var a_1 = Vector3d.CrossProduct(c[1], c[3]) + Vector3d.CrossProduct(c[2], c[2]);
            
            var d = a * c[3];
            var d_1 = a * c[4];
            
            var delta = c[1].Length;
            var delta_1 = c[1] * c[2] / c[1].Length;
            
            var alpha = a.Length;
            var alpha_1 = a * a_1 / a.Length;
                        
            var T = c[1] / delta;
            var T_1 = (delta * c[2] - c[1] * delta_1) / Math.Pow(delta, 2);
            
            var B = a / alpha;
            var B_1 = (alpha * a_1 - a * alpha_1) / Math.Pow(alpha, 2);
            
            var N = Vector3d.CrossProduct(B, T);
            var N_1 = Vector3d.CrossProduct(B, T_1) + Vector3d.CrossProduct(B_1, T);

            var e2 = N * Math.Cos(theta) + B * Math.Sin(theta);
            
            var e3 = B * Math.Cos(theta) - N * Math.Sin(theta);
            var e3_1 = Math.Cos(theta) * (B_1 - N * theta_1) - Math.Sin(theta) * (N_1 + B * theta_1);

            return e3_1 * e2 * c[1] / c[1].SquareLength;
        }

        public double TorsionAt(double t)
        {
            var c = Curve.DerivativeAt(t, 3);

            var a = Vector3d.CrossProduct(c[1], c[2]);
            var d = a * c[3];
            var delta = c[1].Length;
            var alpha = a.Length;

            var tau = d / Math.Pow(alpha, 2);

            return -tau * delta;
        }

        public Curve ToCurve(double tolerance)
        {
            return Curve;
        }
        
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

            return Create(curve);
        }

        public IOrientableCurve Transform(Transform xform)
        {
            var curve = Curve.DuplicateCurve();
            curve.Transform(xform);
            return Create(curve);
        }

        public IOrientableCurve Morph(SpaceMorph xmorph)
        {
            var curve = Curve.DuplicateCurve();
            xmorph.Morph(curve);
            return Create(curve);
        }
    }
}
