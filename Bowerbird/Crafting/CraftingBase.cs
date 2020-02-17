using System.Collections.Generic;
using Rhino.Geometry;

namespace Bowerbird.Crafting
{
    abstract class CraftingBase
    {
        protected static void Project(IEnumerable<Curve> curves, Plane plane, double x, double y)
        {
            var target = new Plane(new Point3d(x, y, 0), Vector3d.XAxis, Vector3d.YAxis);
            var transform = Transform.PlaneToPlane(plane, target);

            TransformCurves(curves, transform);
        }

        protected static void TransformCurves(IEnumerable<Curve> curves, Transform transform)
        {
            foreach (var curve in curves)
            {
                curve.Transform(transform);
            }
        }
    }
}
