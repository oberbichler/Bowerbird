using Bowerbird.Components;
using Bowerbird.Curvature;
using Bowerbird.Parameters;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Bowerbird.Components.PathfinderComponents;

public class EvaluatePath : GH_Component
{
    public EvaluatePath() 
        : base("BB Evaluate Path", "BBEvalPath", "Evaluate directions for a given path type at a starting point.", "Bowerbird", "Paths")
    {
        UpdateMessage();
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new PathParameter(), "Path Type", "T", "Path type to evaluate", GH_ParamAccess.item);
        pManager.AddBrepParameter("Surface", "S", "Surface to evaluate", GH_ParamAccess.item);
        pManager.AddVectorParameter("Point", "P", "Start point for evaluation", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddPointParameter("Point", "P", "Evaluation point coordinates", GH_ParamAccess.item);
        pManager.AddPointParameter("Direction 1", "D1", "First evaluation 3D direction", GH_ParamAccess.item);
        pManager.AddPointParameter("Direction 2", "D2", "Second evaluation 3D direction", GH_ParamAccess.item);
    }

    private StartPointTypes _startPointType = StartPointTypes.XYZ;

    public StartPointTypes StartPointType
    {
        get => _startPointType;
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
        GHUtility.SetMenuList(this, menu, "Change start point type", () => StartPointType, o => StartPointType = o);
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

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var path = default(Bowerbird.Curvature.Path);
        var brep = default(Brep);
        var startingPoint = default(Vector3d);

        if (!DA.GetData(0, ref path)) return;
        if (!DA.GetData(1, ref brep)) return;
        if (!DA.GetData(2, ref startingPoint)) return;

        if (path == null || brep == null) return;

        if (brep.Faces.Count > 1)
            throw new Exception("Multipatches not yet supported");

        var tolerance = DocumentTolerance();
        brep = brep.DuplicateBrep();
        var face = brep.Faces[0];

        // Normalize parameter space. This allows hard-coded tolerances.
        face.SetDomain(0, new Interval(0, 1));
        face.SetDomain(1, new Interval(0, 1));

        var surface = face.UnderlyingSurface();
        Vector2d uv;

        if (StartPointType == StartPointTypes.UV)
        {
            var u = face.Domain(0).NormalizedParameterAt(startingPoint.X);
            var v = face.Domain(1).NormalizedParameterAt(startingPoint.Y);

            if (face.IsPointOnFace(u, v) == PointFaceRelation.Exterior)
                throw new Exception("UV-coordinates are outside face");

            uv = new Vector2d(u, v);
        }
        else // startPointType == StartPointTypes.XYZ
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
                    if (curve == null) continue;

                    if (!curve.ClosestPoint(sample, out var t, closestDistance))
                        continue;

                    var loopPoint = curve.PointAt(t);

                    if (!surface.ClosestPoint(loopPoint, out var loopU, out var loopV))
                        continue;

                    var loopDistance = loopPoint.DistanceTo(sample);

                    if (loopDistance < closestDistance || closestDistance == 0)
                    {
                        closestU = loopU;
                        closestV = loopV;
                        closestDistance = loopDistance;
                    }

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

        DA.SetData(0, surface.PointAt(uv.X, uv.Y));
        DA.SetData(1, d1);
        DA.SetData(2, d2);
    }

    protected override System.Drawing.Bitmap? Icon => Bowerbird.Properties.Resources.icon_pathfinder;

    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public override Guid ComponentGuid => new("{C4549D21-3262-45B9-A15E-F7CBED1111FB}");
}
