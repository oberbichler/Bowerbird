using Rhino.Geometry;
using System;
using System.Diagnostics;

using static System.Math;

namespace Bowerbird.Curvature
{
    public class DGridPath : Path
    {
        public NurbsSurface RefSurface { get; private set; }

        public NurbsSurface ActSurface { get; private set; }

        public double Thickness { get; private set; }

        public double YoungsModulus { get; private set; }

        public double PoissonsRatio { get; private set; }

        private DGridPath(NurbsSurface refSurface, NurbsSurface actSurface, double thickness, double youngsModulus, double poissonsRatio, Types type)
        {
            if (refSurface == null) throw new ArgumentNullException(nameof(refSurface));
            if (actSurface == null) throw new ArgumentNullException(nameof(actSurface));
            if (youngsModulus <= 0) throw new ArgumentException("Invalid youngs modulus");
            if (thickness <= 0) throw new ArgumentException("Invalid thickness");
            if (poissonsRatio < 0 || poissonsRatio >= 1) throw new ArgumentException("Invalid poissons ratio");

            if (refSurface.Domain(0) != actSurface.Domain(0) || refSurface.Domain(1) != actSurface.Domain(1))
                throw new Exception("Parameter space not matching");

            RefSurface = refSurface;
            ActSurface = actSurface;
            Thickness = thickness;
            YoungsModulus = youngsModulus;
            PoissonsRatio = poissonsRatio;
            Type = type;
        }

        public static DGridPath Create(NurbsSurface refSurface, NurbsSurface actSurface, double youngsModulus, double poissonsRatio, double thickness, Types type)
        {
            return new DGridPath(refSurface, actSurface, thickness, youngsModulus, poissonsRatio, type);
        }

        public override Vector3d InitialDirection(Surface surface, Vector2d uv, bool type)
        {
            var u = uv.X;
            var v = uv.Y;

            var ps = new PrincipalStress(RefSurface, ActSurface, Thickness, YoungsModulus, PoissonsRatio);

            if (!ps.Compute(u, v))
                return Vector3d.Zero;

            var t = ps.S2 / ps.S1;

            if (t < 0)
                return Vector3d.Zero;

            var angle = Atan(Sqrt(t));

            var dir1 = ps.D1;
            var dir2 = ps.D1;

            dir1.Rotate(angle, ps.N);
            dir2.Rotate(-angle, ps.N);

            Debug.Assert(dir1.IsValid && !dir1.IsZero);
            Debug.Assert(dir2.IsValid && !dir2.IsZero);

            return type ? dir1 : dir2;
        }

        public override Vector2d Direction(Surface surface, Vector2d uv, Vector3d lastDirection, double stepSize)
        {
            var u = uv.X;
            var v = uv.Y;

            var ps = new PrincipalStress(RefSurface, ActSurface, Thickness, YoungsModulus, PoissonsRatio);

            if (!ps.Compute(u, v))
                return Vector2d.Zero;

            var t = ps.S2 / ps.S1;

            if (t < 0)
                return Vector2d.Zero;

            var angle = Atan(Sqrt(t));

            Vector3d u1;
            Vector3d dir1;
            {
                var cosAlpha = Cos(angle);
                var sinAlpha = Sin(angle);

                var du = ps.A2 * ps.D2 * cosAlpha - ps.A2 * ps.D1 * sinAlpha;
                var dv = ps.A1 * ps.D1 * sinAlpha - ps.A1 * ps.D2 * cosAlpha;

                var dir = du * ps.A1 + dv * ps.A2;

                var l = dir.Length;
                du /= l;
                dv /= l;

                u1 = new Vector3d(du, dv, 0);
                dir1 = du * ps.A1 + dv * ps.A2;

                Debug.Assert(u1.IsValid && !u1.IsZero);
                Debug.Assert(dir1.IsValid && !dir1.IsZero);
            }

            Vector3d u2;
            Vector3d dir2;
            {
                var cosAlpha = Cos(-angle);
                var sinAlpha = Sin(-angle);

                var du = ps.A2 * ps.D2 * cosAlpha - ps.A2 * ps.D1 * sinAlpha;
                var dv = ps.A1 * ps.D1 * sinAlpha - ps.A1 * ps.D2 * cosAlpha;

                var dir = du * ps.A1 + dv * ps.A2;

                var l = dir.Length;
                du /= l;
                dv /= l;

                u2 = new Vector3d(du, dv, 0);
                dir2 = du * ps.A1 + dv * ps.A2;

                Debug.Assert(u2.IsValid && !u2.IsZero);
                Debug.Assert(dir2.IsValid && !dir2.IsZero);
            }

            var d = Choose(u1, u2, dir1, dir2, lastDirection, stepSize);

            Debug.Assert(d.IsValid);
            Debug.Assert(d.Length > 0);

            return new Vector2d(d.X, d.Y);
        }


        public override bool Directions(Surface surface, Vector2d uv, out Vector3d u1, out Vector3d u2, out Vector3d d1, out Vector3d d2)
        {
            var u = uv.X;
            var v = uv.Y;

            var ps = new PrincipalStress(RefSurface, ActSurface, Thickness, YoungsModulus, PoissonsRatio);

            if (!ps.Compute(u, v) || ps.S2 / ps.S1 < 0)
            {
                u1 = default;
                u2 = default;
                d1 = default;
                d2 = default;
                return false;
            }

            var angle = Atan(Sqrt(ps.S2 / ps.S1));

            {
                var cosAlpha = Cos(angle);
                var sinAlpha = Sin(angle);

                var du = ps.A2 * ps.D2 * cosAlpha - ps.A2 * ps.D1 * sinAlpha;
                var dv = ps.A1 * ps.D1 * sinAlpha - ps.A1 * ps.D2 * cosAlpha;

                var dir = du * ps.A1 + dv * ps.A2;

                var l = dir.Length;
                du /= l;
                dv /= l;

                u1 = new Vector3d(du, dv, 0);
                d1 = du * ps.A1 + dv * ps.A2;

                Debug.Assert(u1.IsValid && !u1.IsZero);
                Debug.Assert(d1.IsValid && !d1.IsZero);
            }

            {
                var cosAlpha = Cos(-angle);
                var sinAlpha = Sin(-angle);

                var du = ps.A2 * ps.D2 * cosAlpha - ps.A2 * ps.D1 * sinAlpha;
                var dv = ps.A1 * ps.D1 * sinAlpha - ps.A1 * ps.D2 * cosAlpha;

                var dir = du * ps.A1 + dv * ps.A2;

                var l = dir.Length;
                du /= l;
                dv /= l;

                u2 = new Vector3d(du, dv, 0);
                d2 = du * ps.A1 + dv * ps.A2;

                Debug.Assert(u2.IsValid && !u2.IsZero);
                Debug.Assert(d2.IsValid && !d2.IsZero);
            }

            return true;
        }
    }
}
