using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bowerbird
{
    public class CurveOnSurface
    {
        private CurveOnSurface(Surface surface, Curve curve)
        {
            Surface = surface;
            Curve = curve;
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
            return c_t.X * s_uv[0] + c_t.Y * s_uv[1];
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
            var x_c = Curve.DerivativeAt(t, 2);
            var u = x_c[0].X;
            var v = x_c[0].Y;
            var u1 = x_c[1].X;
            var v1 = x_c[1].Y;
            var u2 = x_c[2].X;
            var v2 = x_c[2].Y;

            Surface.Evaluate(u, v, 2, out var _, out var x_s);
            var su = x_s[0];
            var sv = x_s[1];
            var suu = x_s[2];
            var suv = x_s[3];
            var svv = x_s[4];

            var x1 = u1 * su + v1 * sv;
            var x2 = u2 * su + v2 * sv + Math.Pow(u1, 2) * suu + Math.Pow(v1, 2) * svv + 2 * u1 * v1 * suv;

            var kappa = Vector3d.CrossProduct(x1, x2).Length / Math.Pow(x1.Length, 3);

            var t1 = (x2 * (x1 * x1) - x1 * (x1 * x2)) / Math.Pow(x1 * x1, 1.5);
            
            return t1 / t1.Length * kappa;
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
            var cs = Curve.DerivativeAt(t, 2);
            var u = cs[0].X;
            var v = cs[0].Y;
            var u1 = cs[1].X;
            var v1 = cs[1].Y;
            var u2 = cs[2].X;
            var v2 = cs[2].Y;

            Surface.Evaluate(u, v, 2, out var _, out var ss);
            var su = ss[0];
            var sv = ss[1];
            var suu = ss[2];
            var suv = ss[3];
            var svv = ss[4];

            var n = Vector3d.CrossProduct(su, sv);
            n /= n.Length;

            var x1 = u1 * su + v1 * sv;
            var x2 = suu * Math.Pow(u1, 2) + su * u2 + 2 * suv * u1 * v1 + svv * Math.Pow(v1, 2) + sv * v2;

            return x2 * n / x1.SquareLength * n;
        }

        //public double GeodesicCurvatureAt(double t, out Vector3d n)
        //{

        //}

        public Vector3d GeodesicTorsionAt(double t)
        {
            var cs = Curve.DerivativeAt(t, 3);
            var u = cs[0].X;
            var v = cs[0].Y;
            var u1 = cs[1].X;
            var v1 = cs[1].Y;
            var u2 = cs[2].X;
            var v2 = cs[2].Y;
            var u3 = cs[3].X;
            var v3 = cs[3].Y;

            Surface.Evaluate(u, v, 3, out var _, out var ss);
            var su = ss[0];
            var sv = ss[1];
            var suu = ss[2];
            var suv = ss[3];
            var svv = ss[4];
            var suuu = ss[5];
            var suuv = ss[6];
            var suvv = ss[7];
            var svvv = ss[8];

            var n = Vector3d.CrossProduct(su, sv);
            n /= n.Length;

            var x1 = u1 * su + v1 * sv;
            var x2 = suu * Math.Pow(u1, 2) + su * u2 + 2 * suv * u1 * v1 + svv * Math.Pow(v1, 2) + sv * v2;
            var x3 = suuu * Math.Pow(u1, 3) + 3 * suu * u1 * u2 + su * u3 + v1 * (3 * suuv * Math.Pow(u1, 2) + 3 * suv * u2 + 3 * suvv * u1 * v1 + svvv * Math.Pow(v1, 2)) + 3 * (suv * u1 + svv * v1) * v2 + sv * v3;

            var a = Vector3d.CrossProduct(x1, x2);
            var alpha2 = a.SquareLength;
            var d = a * x3;

            var tau = d / alpha2;

            return x1 / x1.Length * tau;
        }

        public Curve ToCurve(double tolerance)
        {
            return Surface.Pushup(Curve, tolerance);
        }
    }
}
