using Bowerbird.Curvature;
using Bowerbird.Parameters;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Bowerbird.Components.PathfinderComponents
{
    public class EvaluatePath : GH_Component
    {
        public EvaluatePath() : base("BB Evaluate Path", "BBEvalPath", "Beta! Interface might change!", "Bowerbird", "Paths")
        {
            UpdateMessage();
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new PathParameter(), "Path Type", "T", "", GH_ParamAccess.item);
            pManager.AddBrepParameter("Surface", "S", "", GH_ParamAccess.item);
            pManager.AddVectorParameter("Point", "P", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "P", "", GH_ParamAccess.item);
            pManager.AddPointParameter("Direction 1", "D1", "", GH_ParamAccess.item);
            pManager.AddPointParameter("Direction 2", "D2", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var path = default(Path);
            var brep = default(Brep);
            var startingPoint = default(Vector3d);

            if (!DA.GetData(0, ref path)) return;
            if (!DA.GetData(1, ref brep)) return;
            if (!DA.GetData(2, ref startingPoint)) return;

            if (brep.Faces.Count > 1)
                throw new Exception("Multipatches not yet supported");


            // --- Execute

            var tolerance = DocumentTolerance();

            brep = brep.DuplicateBrep();

            var face = brep.Faces[0];

            // normalize parameter space. This allows hard-coded tolerances.
            face.SetDomain(0, new Interval(0, 1)).AssertTrue();
            face.SetDomain(1, new Interval(0, 1)).AssertTrue();

            var surface = face.UnderlyingSurface();

            Vector2d uv;

            if (StartPointType == StartPointTypes.UV)
            {
                // convert UV to normalized parameter space
                var u = face.Domain(0).NormalizedParameterAt(startingPoint.X);
                var v = face.Domain(1).NormalizedParameterAt(startingPoint.Y);

                if (face.IsPointOnFace(u, v) == PointFaceRelation.Exterior)
                    throw new Exception("UV-coordinates are outside face");

                uv = new Vector2d(u, v);
            }
            else // StartPointType == StartPointTypes.XYZ
            {
                var sample = (Point3d)startingPoint;

                face.ClosestPoint(sample, out double u, out double v);

                // If untrimmed CP is outside boundaries -> compute boundary CP
                if (face.IsPointOnFace(u, v) == PointFaceRelation.Exterior)
                {
                    var closestU = 0.0;
                    var closestV = 0.0;
                    var closestDistance = 0.0;

                    foreach (var loop in face.Loops)
                    {
                        var curve = loop.To3dCurve();

                        if (!curve.ClosestPoint(sample, out var t, closestDistance))
                            continue;

                        var loopPoint = curve.PointAt(t);

                        if (!surface.ClosestPoint(loopPoint, out var loopU, out var loopV))
                            continue;

                        var loopDistance = loopPoint.DistanceTo(sample);

                        // Update result if new point is closer
                        if (loopDistance < closestDistance || closestDistance == 0)
                        {
                            closestU = loopU;
                            closestV = loopV;
                            closestDistance = loopDistance;
                        }

                        // Break if point is on boundary
                        if (closestDistance < tolerance)
                            break;
                    }

                    u = closestU;
                    v = closestV;
                }

                uv = new Vector2d(u, v);
            }

            var d1 = path.InitialDirection(surface, uv, false);
            var d2 = path.InitialDirection(surface, uv, true);


            // --- Output

            DA.SetData(0, surface.PointAt(startingPoint.X, startingPoint.Y));
            DA.SetData(1, d1);
            DA.SetData(2, d2);
        }

        public enum StartPointTypes
        {
            XYZ = 0,
            UV = 1
        }

        private StartPointTypes _startPointType;

        public StartPointTypes StartPointType
        {
            get
            {
                return _startPointType;
            }
            set
            {
                _startPointType = value;
                UpdateMessage();
            }
        }

        private void UpdateMessage()
        {
            Message = StartPointType.ToString();
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Utility.SetMenuList(this, menu, "", () => StartPointType, o => StartPointType = o);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.Set("StartPointType", StartPointType);

            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            StartPointType = reader.GetOrDefault("StartPointType", StartPointTypes.XYZ);

            return base.Read(reader);
        }

        protected override Bitmap Icon => Properties.Resources.icon_pathfinder;

        public override GH_Exposure Exposure => Info.Experimental ? GH_Exposure.primary : GH_Exposure.hidden;

        public override Guid ComponentGuid => new Guid("{C4549D21-3262-45B9-A15E-F7CBED1111FB}");
    }
}
