using Rhino.Geometry;
using System;

namespace Bowerbird.Curvature
{
    public class DGridPath : Path
    {
        public NurbsSurface RefSurface { get; private set; }

        public NurbsSurface ActSurface { get; private set; }

        public Transform Material { get; private set; }

        private DGridPath(NurbsSurface refSurface, NurbsSurface actSurface, Transform material, Types type)
        {
            RefSurface = refSurface;
            ActSurface = actSurface;
            Material = material;
            Type = type;
        }

        public static DGridPath Create(NurbsSurface refSurface, NurbsSurface actSurface, double youngsModulus, double poissonsRatio, double thickness, Types type)
        {
            var material = MaterialMatrix(youngsModulus, poissonsRatio, thickness);
            return new DGridPath(refSurface, actSurface, material, type);
        }

        static Transform TransformToLocalCartesian(Vector3d a1, Vector3d a2)
        {
            var g11 = a1 * a1;
            var g12 = a1 * a2;
            var g22 = a2 * a2;

            var det = g11 * g22 - g12 * g12;
            var gCon = new Vector3d(g22 / det, g11 / det, -g12 / det);

            var gCon1 = gCon[0] * a1 + gCon[2] * a2;
            var gCon2 = gCon[2] * a1 + gCon[1] * a2;

            var e1 = a1 / a1.Length;
            var e2 = gCon2 / gCon2.Length;

            var eg11 = e1 * gCon1;
            var eg12 = e1 * gCon2;
            var eg21 = e2 * gCon1;
            var eg22 = e2 * gCon2;

            var transformation = new Transform();
            transformation[0, 0] = eg11 * eg11;
            transformation[0, 1] = eg12 * eg12;
            transformation[0, 2] = 2 * eg11 * eg12;
            transformation[1, 0] = eg21 * eg21;
            transformation[1, 1] = eg22 * eg22;
            transformation[1, 2] = 2 * eg21 * eg22;
            transformation[2, 0] = 2 * eg11 * eg21;
            transformation[2, 1] = 2 * eg12 * eg22;
            transformation[2, 2] = 2 * (eg11 * eg22 + eg12 * eg21);

            return transformation;
        }

        static Transform MaterialMatrix(double youngsModulus, double poissonsRatio, double thickness)
        {
            var f = youngsModulus * thickness / (1.0 - Math.Pow(poissonsRatio, 2));

            var dm = new Transform();
            dm[0, 0] = f;
            dm[0, 1] = poissonsRatio * f;
            dm[0, 2] = 0;
            dm[1, 0] = poissonsRatio * f;
            dm[1, 1] = f;
            dm[1, 2] = 0;
            dm[2, 0] = 0;
            dm[2, 1] = 0;
            dm[2, 2] = 0.5 * (1 - poissonsRatio) * f;

            return dm;
        }

        bool FindDirections(double u, double v, out Vector3d dir1, out Vector3d dir2, out Vector3d refA1, out Vector3d refA2)
        {
            RefSurface.Evaluate(u, v, 1, out var _, out var refDerivatives);
            refA1 = refDerivatives[0];
            refA2 = refDerivatives[1];

            ActSurface.Evaluate(u, v, 1, out var _, out var actDerivatives);
            var actA1 = actDerivatives[0];
            var actA2 = actDerivatives[1];

            var e = default(Vector3d);
            e[0] = actA1 * refA1 - refA1.SquareLength;
            e[1] = actA2 * refA2 - refA2.SquareLength;
            e[2] = 0.5 * (actA1 * refA2 + actA2 * refA1) - refA1 * refA2;
            
            e.Transform(TransformToLocalCartesian(refA1, refA2));
            e.Transform(Material);

            var tmp = Math.Sqrt(Math.Pow(e[0] - e[1], 2) + 4 * Math.Pow(e[2], 2));
            var n1 = 0.5 * (e[0] + e[1] + tmp);
            var n2 = 0.5 * (e[0] + e[1] - tmp);

            if (n1 * n2 < 0)
            {
                dir1 = Vector3d.Unset;
                dir2 = Vector3d.Unset;
                return false;
            }

            var normal = Vector3d.CrossProduct(refA1, refA2);

            if (Math.Abs(e[2]) < 1e-8)
            {
                dir1 = refA1 / refA1.Length;
            }
            else
            {
                var alpha = Math.Atan2(2 * e[2], e[0] - e[1]) / 2;

                dir1 = refA1 / refA1.Length;
                dir1.Rotate(alpha, normal);
            }

            dir2 = dir1;

            var angle = Math.Atan(Math.Sqrt(n2 / n1));
            
            dir1.Rotate(angle, normal);
            dir2.Rotate(-angle, normal);

            return true;
        }

        public override Vector3d InitialDirection(Surface surface, Vector2d uv, bool type)
        {
            var u = uv.X;
            var v = uv.Y;

            if (!FindDirections(u, v, out var dir1, out var dir2, out var _, out var _))
                return Vector3d.Unset;

            return !type ? dir1 : dir2;
        }

        public override Vector2d Direction(Surface surface, Vector2d uv, Vector3d lastDirection, double stepSize)
        {
            var u = uv.X;
            var v = uv.Y;

            if (!FindDirections(u, v, out var dir1, out var dir2, out var a1, out var a2))
                return Vector2d.Unset;

            var direction = Choose(dir1, dir2, lastDirection, stepSize);

            return ToUV(a1, a2, direction);
        }
    }
}
