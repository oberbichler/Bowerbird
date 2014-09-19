using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace Bowerbird.Getters
{
    public class PlaneGetter
    {
        public static Plane? GetPlane()
        {
            var unset = Plane.Unset;
            var origin = new GetPlaneOrigin();

            switch (origin.Get())
            {
                case GetResult.Option:
                    switch (origin.Option().EnglishName)
                    {
                        case "WorldXY":
                            return Plane.WorldXY;

                        case "WorldZX":
                            return Plane.WorldZX;

                        case "WorldYZ":
                            return Plane.WorldYZ;
                    }
                    break;

                case GetResult.Point:
                    unset = origin.Plane;
                    unset.Origin = origin.Point();
                    break;

                default:
                    return null;
            }

            var p = unset;

            var axisX = new GetPlaneXAxis1(unset);

            switch (axisX.Get())
            {
                case GetResult.Option:
                {
                    var option = axisX.Option().EnglishName;

                    if (option != "ParallelGrid")
                    {
                        Plane plane5;
                        switch (option)
                        {
                            case "ParallelXY":
                                plane5 = new Plane(unset.Origin, Vector3d.XAxis, Vector3d.YAxis);
                                return plane5;

                            case "ParallelYZ":
                                plane5 = new Plane(unset.Origin, Vector3d.YAxis, Vector3d.ZAxis);
                                return plane5;

                            case "ParallelZX":
                                return new Plane(unset.Origin, Vector3d.ZAxis, Vector3d.XAxis);
                        }
                        break;
                    }
                    return unset;
                }
                case GetResult.Point:
                    p = AlignX(unset, axisX.Point());
                    break;

                default:
                    return null;
            }

            var plane = p;
            var axisY = new GetPlaneYAxis2(p);
            switch (axisY.Get())
            {
                case GetResult.Option:
                    break;

                case GetResult.Point:
                    plane = AlignY(p, axisY.Point());
                    break;

                default:
                    return null;
            }
            if (!plane.IsValid)
            {
                return null;
            }
            return plane;
        }

        public static Plane? GetAxis()
        {
            var unset = Plane.Unset;
            var origin = new GetPlaneOrigin();

            switch (origin.Get())
            {
                case GetResult.Option:
                    switch (origin.Option().EnglishName)
                    {
                        case "WorldXY":
                            return Plane.WorldXY;

                        case "WorldZX":
                            return Plane.WorldZX;

                        case "WorldYZ":
                            return Plane.WorldYZ;
                    }
                    break;

                case GetResult.Point:
                    unset = origin.Plane;
                    unset.Origin = origin.Point();
                    break;

                default:
                    return null;
            }

            var p = unset;

            var axisZ = new GetPlaneZAxis1(unset);

            switch (axisZ.Get())
            {
                case GetResult.Option:
                    {
                        var option = axisZ.Option().EnglishName;

                        if (option != "ParallelGrid")
                        {
                            Plane plane5;
                            switch (option)
                            {
                                case "ParallelXY":
                                    plane5 = new Plane(unset.Origin, Vector3d.XAxis, Vector3d.YAxis);
                                    return plane5;

                                case "ParallelYZ":
                                    plane5 = new Plane(unset.Origin, Vector3d.YAxis, Vector3d.ZAxis);
                                    return plane5;

                                case "ParallelZX":
                                    return new Plane(unset.Origin, Vector3d.ZAxis, Vector3d.XAxis);
                            }
                            break;
                        }
                        return unset;
                    }
                case GetResult.Point:
                    p = AlignZ(unset, axisZ.Point());
                    break;

                default:
                    return null;
            }

            var plane = p;
            var axisX = new GetPlaneXAxis2(p);
            switch (axisX.Get())
            {
                case GetResult.Option:
                    break;

                case GetResult.Point:
                    plane = AlignX(p, axisX.Point());
                    break;

                default:
                    return null;
            }
            if (!plane.IsValid)
            {
                return null;
            }
            return plane;
        }


        private static void DrawPlane(DisplayPipeline di, RhinoViewport vp, Plane plane)
        {
            double num;

            if (!vp.GetWorldToScreenScale(plane.Origin, out num))
                return;

            var unit = 10.0;

            if ((unit * num) < 20.0)
                unit = 20.0 / num;

            unit *= 0.2;

            var min = -5;
            var max = 5;

            for (var x = min; x <= max; x++)
            {
                var p0 = plane.PointAt(min * unit, x * unit);
                var p1 = plane.PointAt(max * unit, x * unit);

                if (x == 0)
                {
                    var origin = plane.Origin;
                    di.DrawLine(p0, origin, Color.Gray);
                    di.DrawLine(origin, p1, Color.DarkRed, 3);
                    di.DrawArrowHead(p1, p1 - origin, Color.DarkRed, 0.0, 1.0 * unit);
                }
                else
                {
                    di.DrawLine(p0, p1, Color.Gray);
                }
            }

            for (var y = min; y <= max; y++)
            {
                var p0 = plane.PointAt(y * unit, min * unit);
                var p1 = plane.PointAt(y * unit, max * unit);

                if (y == 0)
                {
                    var origin = plane.Origin;
                    di.DrawLine(p0, origin, Color.Gray);
                    di.DrawLine(origin, p1, Color.DarkGreen, 3);
                    di.DrawArrowHead(p1, p1 - origin, Color.DarkGreen, 0.0, 1.0 * unit);
                }
                else
                {
                    di.DrawLine(p0, p1, Color.Gray);
                }
            }
        }
        

        private static Plane AlignX(Plane plane, Point3d target)
        {
            var axisX = target - plane.Origin;

            if (axisX.IsZero)
                return plane;

            var axisY = Vector3d.CrossProduct(plane.ZAxis, axisX);

            if (axisY.IsZero)
                axisY = Vector3d.CrossProduct(plane.XAxis, axisX);

            var axisZ = Vector3d.CrossProduct(axisX, axisY);

            axisX.Unitize();
            axisY.Unitize();
            axisZ.Unitize();

            plane.XAxis = axisX;
            plane.YAxis = axisY;
            plane.ZAxis = axisZ;

            return plane;
        }

        private static Plane AlignY(Plane plane, Point3d target)
        {
            var axisY = target - plane.Origin;

            if (axisY.IsZero)
                return plane;

            var axisX = Vector3d.CrossProduct(axisY, plane.ZAxis);

            if (axisX.IsZero)
                axisX = Vector3d.CrossProduct(axisY, plane.YAxis);

            var axisZ = Vector3d.CrossProduct(axisX, axisY);

            axisX.Unitize();
            axisY.Unitize();
            axisZ.Unitize();

            plane.XAxis = axisX;
            plane.YAxis = axisY;
            plane.ZAxis = axisZ;

            return plane;
        }

        private static Plane AlignZ(Plane plane, Point3d target)
        {
            var axisZ = target - plane.Origin;

            if (axisZ.IsZero)
                return plane;

            var axisY = Vector3d.CrossProduct(plane.XAxis, axisZ);
            var axisX = Vector3d.CrossProduct(axisY, axisZ);

            axisX.Unitize();
            axisY.Unitize();
            axisZ.Unitize();

            if (plane.XAxis.IsParallelTo(axisX, 1.5707963267948966) < 0)
            {
                plane.XAxis = -axisX;
                plane.YAxis = -axisY;
                plane.ZAxis = axisZ;
                return plane;
            }

            plane.XAxis = axisX;
            plane.YAxis = axisY;
            plane.ZAxis = axisZ;

            return plane;
        }


        private class GetPlaneOrigin : GetPoint
        {
            private Plane _plane = Plane.Unset;

            public GetPlaneOrigin()
            {
                SetCommandPrompt("Origin of plane");
                MouseMove += LocalMouseMove;
                DynamicDraw += LocalDynamicDraw;
                AddOption("WorldXY");
                AddOption("WorldYZ");
                AddOption("WorldZX");
            }

            private void LocalDynamicDraw(object sender, GetPointDrawEventArgs e)
            {
                if (_plane.IsValid)
                    DrawPlane(e.Display, e.Viewport, _plane);
            }

            private void LocalMouseMove(object sender, GetPointMouseEventArgs e)
            {
                _plane = e.Viewport.ConstructionPlane();
                _plane.Origin = e.Point;
            }

            public Plane Plane
            {
                get
                {
                    return _plane;
                }
            }
        }

        private class GetPlaneXAxis1 : GetPoint
        {
            private readonly Plane _original;
            private Plane _plane;

            public GetPlaneXAxis1(Plane p)
            {
                _original = _plane = p;
                SetCommandPrompt("Plane X-Axis direction");
                MouseMove += Getter_MouseMove;
                DynamicDraw += Getter_DynamicDraw;
                SetBasePoint(p.Origin, false);
                AddOption("ParallelGrid");
                AddOption("ParallelXY");
                AddOption("ParallelYZ");
                AddOption("ParallelZX");
            }

            private void Getter_DynamicDraw(object sender, GetPointDrawEventArgs e)
            {
                if (_plane.IsValid)
                    DrawPlane(e.Display, e.Viewport, _plane);
            }

            private void Getter_MouseMove(object sender, GetPointMouseEventArgs e)
            {
                _plane = AlignX(_original, e.Point);
            }
        }

        private class GetPlaneYAxis2 : GetPoint
        {
            private Plane _original = Plane.Unset;
            private Plane _plane;

            public GetPlaneYAxis2(Plane p)
            {
                _original = _plane = p;
                SetCommandPrompt("Plane Y-Axis direction");
                MouseMove += LocalMouseMove;
                DynamicDraw += LocalDynamicDraw;
                SetBasePoint(p.Origin, false);
                var plane = new Plane(p.Origin, p.XAxis);
                Constrain(plane, false);
            }

            private void LocalDynamicDraw(object sender, GetPointDrawEventArgs e)
            {
                if (_plane.IsValid)
                    DrawPlane(e.Display, e.Viewport, _plane);
            }

            private void LocalMouseMove(object sender, GetPointMouseEventArgs e)
            {
                _plane = AlignY(_original, e.Point);
            }
        }

        private class GetPlaneZAxis1 : GetPoint
        {
            private readonly Plane _original;
            private Plane _plane;

            public GetPlaneZAxis1(Plane p)
            {
                _original = _plane = p;
                SetCommandPrompt("Plane Z-Axis direction");
                MouseMove += Getter_MouseMove;
                DynamicDraw += Getter_DynamicDraw;
                SetBasePoint(p.Origin, false);
                AddOption("ParallelGrid");
                AddOption("ParallelXY");
                AddOption("ParallelYZ");
                AddOption("ParallelZX");
            }

            private void Getter_DynamicDraw(object sender, GetPointDrawEventArgs e)
            {
                if (_plane.IsValid)
                    DrawPlane(e.Display, e.Viewport, _plane);
            }

            private void Getter_MouseMove(object sender, GetPointMouseEventArgs e)
            {
                _plane = AlignZ(_original, e.Point);
            }
        }

        private class GetPlaneXAxis2 : GetPoint
        {
            private Plane _original = Plane.Unset;
            private Plane _plane;

            public GetPlaneXAxis2(Plane p)
            {
                _original = _plane = p;
                SetCommandPrompt("Plane X-Axis direction");
                MouseMove += Getter_MouseMove;
                DynamicDraw += Getter_DynamicDraw;
                SetBasePoint(p.Origin, false);
                var plane = new Plane(p.Origin, p.ZAxis);
                Constrain(plane, false);
            }

            private void Getter_DynamicDraw(object sender, GetPointDrawEventArgs e)
            {
                if (_plane.IsValid)
                    DrawPlane(e.Display, e.Viewport, _plane);
            }

            private void Getter_MouseMove(object sender, GetPointMouseEventArgs e)
            {
                _plane = AlignX(_original, e.Point);
            }
        }
    }
}