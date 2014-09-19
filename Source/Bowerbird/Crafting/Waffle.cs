using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace Bowerbird.Crafting
{
    internal class Waffle
    {
        protected Waffle(Mesh mesh, Plane plane, double thickness, double deeper, int countX, int countY, double unit, bool project, double projectSpace)
        {
            var box = mesh.Box(plane);

            var unitX = box.Plane.XAxis;
            var unitY = box.Plane.YAxis;
            var unitZ = box.Plane.ZAxis;

            var origin = box.PointAt(0.0, 0.0, 0.0);

            var lenX = Math.Abs(box.X.Length);
            var lenY = Math.Abs(box.Y.Length);
            var lenZ = Math.Abs(box.Z.Length);

            var valX = Enumerable.Range(1, countX).Select(o => o * lenX / (countX + 1));
            var vecX = valX.Select(o => o * unitX).ToArray();
            var plnX = vecX.Select(o => new Plane(origin + o, unitY, unitZ)).ToArray();
            var sltX = plnX.Select(o => new SlitPlane(mesh, o, unit)).ToArray();

            var valY = Enumerable.Range(1, countY).Select(o => o * lenY / (countY + 1));
            var vecY = valY.Select(o => o * unitY).ToArray();
            var plnY = vecY.Select(o => new Plane(origin + o, unitX, unitZ)).ToArray();
            var sltY = plnY.Select(o => new SlitPlane(mesh, o, unit)).ToArray();

            for (var i = 0; i < sltX.Length; i++)
            {
                for (var j = 0; j < sltY.Length; j++)
                {
                    var p0 = origin + vecX[i] + vecY[j];
                    var p1 = p0 + lenZ * unitZ;

                    sltX[i].AddSlit(p0, p1, thickness, deeper);
                    sltY[j].AddSlit(p1, p0, thickness, deeper);
                }
            }

            CurvesX = sltX.Select(o => o.GetResult()).ToArray();
            CurvesY = sltY.Select(o => o.GetResult()).ToArray();
            PlanesX = plnX;
            PlanesY = plnY;

            if (!project) 
                return;

            for (var i = 0; i < CurvesX.Length; i++)
            {
                var target = new Plane(new Point3d(i * (lenY + projectSpace), 0, 0), Vector3d.XAxis, Vector3d.YAxis);
                var transform = Transform.PlaneToPlane(PlanesX[i], target);

                TransformCurves(CurvesX[i], transform);
            }

            for (var i = 0; i < CurvesY.Length; i++)
            {
                var target = new Plane(new Point3d(i * (lenX + projectSpace), lenZ + projectSpace, 0), Vector3d.XAxis, Vector3d.YAxis);
                var transform = Transform.PlaneToPlane(PlanesY[i], target);

                TransformCurves(CurvesY[i], transform);
            }
        }

        public void TransformCurves(IEnumerable<Curve> curves, Transform transform)
        {
            foreach (var curve in curves)
            {
                curve.Transform(transform);
            }
        }

        public static Waffle Create(Mesh mesh, Plane plane, double thickness, double deeper, int countA, int countZ, double unit, bool project = false, double projectSpace = 0.0)
        {
            if (thickness <= 0)
                throw new Exception(@"Thickness must be a positive value!");

            if (deeper < 0)
                throw new Exception(@"Border must be a positive value!");

            if (!mesh.IsValid)
                throw new Exception(@"Mesh is not a valid!");

            if (!mesh.IsClosed)
                throw new Exception(@"Mesh is not closed!");


            return new Waffle(mesh, plane, thickness, deeper, countA, countZ, unit, project, projectSpace);
        }


        public List<Curve>[] CurvesX { get; private set; }

        public List<Curve>[] CurvesY { get; private set; }

        public Plane[] PlanesX { get; private set; }

        public Plane[] PlanesY { get; private set; }
    }
}