﻿using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Bowerbird.Curvature
{
    struct SurfaceCurvature
    {
        private SurfaceCurvature(
            Point3d x,
            Vector3d a1,
            Vector3d a2,
            Vector3d n,
            double k1,
            double k2,
            Vector2d k1ParameterDirection,
            Vector2d k2ParameterDirection,
            Vector3d k1Direction,
            Vector3d k2Direction)
        {
            X = x;
            A1 = a1;
            A2 = a2;
            N = n;
            K1 = k1;
            K2 = k2;
            K1ParameterDirection = k1ParameterDirection;
            K2ParameterDirection = k2ParameterDirection;
            K1Direction = k1Direction;
            K2Direction = k2Direction;
        }

        public static SurfaceCurvature Create(Surface surface, double u, double v)
        {
            var curvature = surface.CurvatureAt(u, v);
            surface.Evaluate(u, v, 2, out var x, out var derivatives);

            var a1 = derivatives[0];
            var a2 = derivatives[1];

            var n = Vector3d.CrossProduct(a1, a2);
            n.Unitize();

            var k1 = curvature?.Kappa(0) ?? double.NaN;
            var k2 = curvature?.Kappa(1) ?? double.NaN;

            var k1Direction = Vector3d.Unset;
            var k2Direction = Vector3d.Unset;

            var k1ParameterDirection = Vector2d.Unset;
            var k2ParameterDirection = Vector2d.Unset;

            if (!double.IsNaN(k1))
            {
                k1Direction = curvature.Direction(0);
                k1Direction.Unitize();
                k1ParameterDirection = ToUV(a1, a2, k1Direction);
                k1ParameterDirection.Unitize();
            }

            if (!double.IsNaN(k2))
            {
                k2Direction = curvature.Direction(1);
                k2Direction.Unitize();
                k2ParameterDirection = ToUV(a1, a2, k2Direction);
                k2ParameterDirection.Unitize();
            }

            return new SurfaceCurvature(x, a1, a2, n, k1, k2, k1ParameterDirection, k2ParameterDirection, k1Direction, k2Direction);
        }

        private static Vector2d ToUV(Vector3d a1, Vector3d a2, Vector3d d)
        {
            var det = a1.X * a2.Y - a2.X * a1.Y;
            var u = (d.X * a2.Y - d.Y * a2.X) / det;
            var v = (d.Y * a1.X - d.X * a1.Y) / det;
            return new Vector2d(u, v);
        }

        public Point3d X { get; private set; }

        public Vector3d A1 { get; private set; }

        public Vector3d A2 { get; private set; }

        public Vector3d N { get; private set; }

        public double K1 { get; private set; }

        public double K2 { get; private set; }

        public Vector2d K1ParameterDirection { get; private set; }

        public Vector2d K2ParameterDirection { get; private set; }

        public Vector3d K1Direction { get; private set; }

        public Vector3d K2Direction { get; private set; }

        public Vector3d MinDirection => K1 < K2 ? K1Direction : K2Direction;

        public Vector3d MaxDirection => K1 < K2 ? K2Direction : K1Direction;

        public bool FindAngleByNormalCurvature(double k, out double angle1, out double angle2)
        {
            var c = Complex.Sqrt(K2 - k) / Complex.Sqrt(K2 - K1);
            var s = Complex.Sqrt(K1 - k) / Complex.Sqrt(K1 - K2);

            if (Math.Abs(c.Imaginary) > 1e-10 || Math.Abs(s.Imaginary) > 1e-10)
            {
                angle1 = default;
                angle2 = default;
                return false;
            }

            angle1 = Math.Atan2(s.Real, c.Real);
            angle2 = Math.Atan2(s.Real, -c.Real);

            return true;
        }

        public bool FindAngleByGeodesicTorsion(double value, out double angle1, out double angle2)
        {
            if (value == 0 || (K2 - K1) == 0)
            {
                angle1 = 0.0;
                angle2 = Math.PI / 2;
                return true;
            }

            var t = value / (K2 - K1);
            var d = 1 - 4 * t * t;

            if (d < 0)
            {
                angle1 = default;
                angle2 = default;
                return false;
            }

            var b = Math.Sqrt(d);

            angle1 = Math.Atan2(2 * t / Math.Sqrt(1 - b), Math.Sqrt(1 - b));
            angle2 = Math.Atan2(2 * t / Math.Sqrt(1 + b), Math.Sqrt(1 + b));

            if (double.IsNaN(angle1))
                angle1 = Math.PI / 2;

            if (double.IsNaN(angle2))
                angle2 = Math.PI / 2;

            return true;
        }
    }
}
