﻿using Rhino.Geometry;
using System;
using System.Diagnostics;

namespace Bowerbird.Curvature
{
    public abstract class Path
    {
        public enum Types
        {
            First = 1,
            Second = 2,
            Both = 3
        }

        public Types Type { get; protected set; }

        public abstract Vector3d InitialDirection(Surface surface, Vector2d uv, bool type);

        public abstract bool Directions(Surface surface, Vector2d uv, out Vector3d u1, out Vector3d u2, out Vector3d d1, out Vector3d d2);

        public abstract Vector2d Direction(Surface surface, Vector2d uv, Vector3d lastDirection, double stepSize);

        protected static Vector2d ToUV(Vector3d a1, Vector3d a2, Vector3d d)
        {
            var det = a1.X * a2.Y - a1.Y * a2.X;

            //Debug.Assert(Math.Abs(det) > 1e-10);

            if (Math.Abs(det) < 1e-10)
                return Vector2d.Zero;

            var u = (d.X * a2.Y - d.Y * a2.X) / det;
            var v = (d.Y * a1.X - d.X * a1.Y) / det;

            return new Vector2d(u, v);
        }

        protected static Vector3d Align(Vector3d v, Vector3d sample, double scale = 1.0)
        {
            if (v * sample > 0)
                return v * scale;
            else
                return v * -scale;
        }

        protected static Vector2d Choose(Vector2d u1, Vector2d u2, Vector3d dir1, Vector3d dir2, Vector3d sample, double stepSize)
        {
            Debug.Assert(dir1.Length > 1e-10);
            Debug.Assert(dir2.Length > 1e-10);
            Debug.Assert(sample.Length > 1e-10);

            sample /= sample.Length;

            var d1 = dir1 * sample;
            var d2 = dir2 * sample;

            if (Math.Abs(d1) > Math.Abs(d2))
                return d1 > 0 ? u1 * stepSize : -u1 * stepSize;
            else
                return d2 > 0 ? u2 * stepSize : -u2 * stepSize;
        }

        protected static Vector3d Choose(Vector3d u1, Vector3d u2, Vector3d dir1, Vector3d dir2, Vector3d sample, double stepSize)
        {
            Debug.Assert(dir1.Length > 1e-10);
            Debug.Assert(dir2.Length > 1e-10);
            Debug.Assert(sample.Length > 1e-10);

            sample /= sample.Length;

            var d1 = dir1 * sample;
            var d2 = dir2 * sample;

            if (Math.Abs(d1) > Math.Abs(d2))
                return d1 > 0 ? u1 * stepSize : -u1 * stepSize;
            else
                return d2 > 0 ? u2 * stepSize : -u2 * stepSize;
        }
    }
}
