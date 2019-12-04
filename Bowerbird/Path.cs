using Rhino.Geometry;
using System;

namespace Bowerbird
{
    public abstract class Path
    {
        public enum Type
        {
            First = 1,
            Second = 2,
            Both = 3
        }

        public abstract Vector3d InitialDirection(Surface surface, Vector2d uv, bool type);

        public abstract Vector2d Direction(Surface surface, Vector2d uv, Vector3d lastDirection);

        protected static Vector2d ToUV(Vector3d a1, Vector3d a2, Vector3d d)
        {
            var det = a1.X * a2.Y - a2.X * a1.Y;
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

        protected static Vector3d Choose(Vector3d dir1, Vector3d dir2, Vector3d sample)
        {
            sample.Unitize();

            if (Math.Abs(dir1 * sample) > Math.Abs(dir2 * sample))
                return dir1;
            else
                return dir2;
        }
    }
}
