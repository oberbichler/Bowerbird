﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace Bowerbird.Crafting
{
    class Radial
    {
        protected Radial(Mesh mesh, Plane plane, double thickness, double deeper, double radius, int countA, int countZ, double unit, bool project, double projectSpace)
        {
            var box = mesh.Box(plane);

            var unitX = box.Plane.XAxis;
            var unitY = box.Plane.YAxis;
            var unitZ = box.Plane.ZAxis;
            var origin = box.Plane.Origin + box.Z.Min * unitZ;

            var corners = box.GetCorners();

            var valA = Enumerable.Range(0, countA).Select(o => Math.PI * 2.0 / countA * o);
            var rayA = valA.Select(o => unitX * Math.Cos(o) + unitY * Math.Sin(o)).ToArray();
            var plnA = rayA.Select(o => new Plane(origin, o, unitZ)).ToArray();
            var sltA = plnA.Select(o => new SlitPlane(mesh, o, unit)).ToArray();

            var valZ = Enumerable.Range(1, countZ).Select(o => o * box.Z.Length / (countZ + 1));
            var pntZ = valZ.Select(o => origin + o * unitZ).ToArray();
            var plnZ = pntZ.Select(o => new Plane(o, unitX, unitY)).ToArray();
            var sltZ = plnZ.Select(o => new SlitPlane(mesh, o, unit)).ToArray();

            var r1 = Math.Max(radius, thickness / 2.0 / Math.Tan(Math.PI / rayA.Length));
            var r2 = Enumerable.Range(0, 4).Select(i => corners[i].DistanceTo(origin)).Max();

            for (var i = 0; i < sltA.Length; i++)
            {
                sltA[i].Clip(r1, r2, -2.0 * unit, box.Z.Length + 2.0 * unit);

                for (var j = 0; j < sltZ.Length; j++)
                {
                    var p0 = pntZ[j] + rayA[i] * (r1 - unit);
                    var p1 = pntZ[j] + rayA[i] * r2;

                    sltA[i].AddSlit(p0, p1, thickness, deeper);
                    sltZ[j].AddSlit(p1, p0, thickness, deeper);
                }
            }

            CurvesA = sltA.Select(o => o.GetResult()).ToArray();
            CurvesZ = sltZ.Select(o => o.GetResult()).ToArray();

            if (project)
            {
                var dx1 = (r2 - r1) + projectSpace;

                for (var i = 0; i < CurvesA.Length; i++)
                {
                    var target = new Plane(new Point3d(i * dx1, 0, 0), Vector3d.XAxis, Vector3d.YAxis);
                    var transform = Transform.PlaneToPlane(plnA[i], target);

                    TransformCurves(CurvesA[i], transform);
                }

                var dx2 = Math.Abs(box.X.Length) + projectSpace;
                var y2 = Math.Abs(box.Z.Length - box.Y.Min) + projectSpace;

                for (var i = 0; i < CurvesZ.Length; i++)
                {
                    var target = new Plane(new Point3d(i * dx2, y2, 0), Vector3d.XAxis, Vector3d.YAxis);
                    var transform = Transform.PlaneToPlane(plnZ[i], target);

                    TransformCurves(CurvesZ[i], transform);
                }
            }
            
            PlanesA = plnA;
            PlanesZ = plnZ;
        }

        public void TransformCurves(IEnumerable<Curve> curves, Transform transform)
        {
            foreach (var curve in curves)
            {
                curve.Transform(transform);
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
