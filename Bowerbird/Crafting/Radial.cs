using System;
using System.Collections.Generic;
using System.Linq;
using Rhino.Geometry;

namespace Bowerbird.Crafting
{
    class Radial : CraftingBase
    {
        protected Radial(Mesh mesh, Plane plane, double thickness, double deeper, double radius, int countA, int countZ, double unit, bool project, double projectSpace)
        {
            var box = mesh.Box(plane);

            var unitX = box.Plane.XAxis;
            var unitY = box.Plane.YAxis;
            var unitZ = box.Plane.ZAxis;
            var center = box.Plane.Origin + box.Z.Min * unitZ;

            var corners = box.GetCorners();

            var r1 = Math.Max(radius, thickness / 2.0 / Math.Tan(Math.PI / countA));
            var r2 = Enumerable.Range(0, 4).Select(i => corners[i].DistanceTo(center)).Max();


            var lenX = Math.Abs(box.X.Length);
            var lenZ = Math.Abs(box.Z.Length);
            var lenR = r2 - r1;

            var valA = Enumerable.Range(0, countA).Select(o => Math.PI * 2.0 / countA * o);
            var rayA = valA.Select(o => unitX * Math.Cos(o) + unitY * Math.Sin(o)).ToArray();
            var plnA = rayA.Select(o => new Plane(center + o * r1, o, unitZ)).ToArray();
            var sltA = plnA.Select(o => new SlitPlane(mesh, o, unit)).ToArray();

            var valZ = Enumerable.Range(1, countZ).Select(o => o * lenZ / (countZ + 1)).ToArray();
            var pntZ = valZ.Select(o => corners[0] + o * unitZ).ToArray();
            var plnZ = pntZ.Select(o => new Plane(o, unitX, unitY)).ToArray();
            var sltZ = plnZ.Select(o => new SlitPlane(mesh, o, unit)).ToArray();

            for (var i = 0; i < sltA.Length; i++)
            {
                sltA[i].Clip(-unit, (lenR) + unit, -unit, lenZ + unit);

                for (var j = 0; j < sltZ.Length; j++)
                {
                    var c = center + valZ[j] * unitZ;

                    var p0 = c + rayA[i] * (r1 - unit);
                    var p1 = c + rayA[i] * r2;

                    sltA[i].AddSlit(p0, p1, thickness, deeper);
                    sltZ[j].AddSlit(p1, p0, thickness, deeper);
                }
            }

            CurvesA = sltA.Select(o => o.GetResult()).ToArray();
            CurvesZ = sltZ.Select(o => o.GetResult()).ToArray();
            PlanesA = plnA;
            PlanesZ = plnZ;

            if (!project)
                return;

            for (var i = 0; i < CurvesA.Length; i++)
            {
                Project(CurvesA[i], PlanesA[i], i * (lenR + projectSpace), 0);
            }

            for (var i = 0; i < CurvesZ.Length; i++)
            {
                Project(CurvesZ[i], PlanesZ[i], i * (lenX + projectSpace), lenZ + projectSpace);
            }
        }

        public static Radial Create(Mesh mesh, Plane plane, double thickness, double deeper, double radius, int countA, int countZ, double unit, bool project = false, double projectSpace = 0.0)
        {
            if (thickness <= 0)
                throw new Exception(@"Thickness must be a positive value!");

            if (deeper < 0)
                throw new Exception(@"Border must be a positive value!");

            if (!mesh.IsValid)
                throw new Exception(@"Mesh is not a valid!");

            if (!mesh.IsClosed)
                throw new Exception(@"Mesh is not closed!");


            return new Radial(mesh, plane, thickness, deeper, radius, countA, countZ, unit, project, projectSpace);
        }


        public List<Curve>[] CurvesA { get; private set; }

        public List<Curve>[] CurvesZ { get; private set; }

        public Plane[] PlanesA { get; private set; }

        public Plane[] PlanesZ { get; private set; }
    }
}
