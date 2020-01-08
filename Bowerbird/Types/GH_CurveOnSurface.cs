using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bowerbird.Types
{
    public class GH_CurveOnSurface : GH_GeometricGoo<CurveOnSurface>, IGH_PreviewData, IGH_BakeAwareData
    {
        public GH_CurveOnSurface() : base()
        {
        }

        public GH_CurveOnSurface(CurveOnSurface value) : base(value)
        {
        }

        public void UpdateGeometry()
        {
            Geometry = Value.ToCurve(RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
            ClippingBox = Geometry.GetBoundingBox(false);
        }

        public Curve Geometry { get; private set; }

        public override bool IsValid => Value != null;

        public override string TypeName => "BBCurveOnSurface";

        public override string TypeDescription => "";

        public BoundingBox ClippingBox { get; private set; }

        public override BoundingBox Boundingbox => ClippingBox;

        public override IGH_Goo Duplicate()
        {
            return new GH_CurveOnSurface(Value);
        }

        public override bool CastFrom(object source)
        {
            switch (source)
            {
                case CurveOnSurface curveOnSurface:
                    Value = curveOnSurface;
                    return true;
                case GH_CurveOnSurface ghCurveOnSurface:
                    Value = ghCurveOnSurface.Value;
                    return true;
                default:
                    return base.CastFrom(source);
            }
        }

        public override bool CastTo<Q>(ref Q target)
        {
            if (!typeof(Q).IsAssignableFrom(m_value.GetType()))
                return false;

            var obj = (object)m_value;
            target = (Q)obj;

            return true;
        }

        public override string ToString() => Value.ToString();

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            if (Geometry == null)
                UpdateGeometry();

            args.Pipeline.DrawCurve(Geometry, args.Color, args.Thickness);
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
        }

        public bool BakeGeometry(RhinoDoc doc, ObjectAttributes att, out Guid obj_guid)
        {
            obj_guid = doc.Objects.AddCurve(Geometry, att);
            return obj_guid != Guid.Empty;
        }

        public override IGH_GeometricGoo DuplicateGeometry()
        {
            return new GH_CurveOnSurface(Value);
        }

        public override BoundingBox GetBoundingBox(Transform xform)
        {
            return xform.TransformBoundingBox(ClippingBox);
        }

        public override IGH_GeometricGoo Transform(Transform xform)
        {
            var surface = (Surface)Value.Surface.Duplicate();
            var curve = Value.Curve;
            surface.Transform(xform);
            return new GH_CurveOnSurface(CurveOnSurface.Create(surface, curve));
        }

        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            var surface = (Surface)Value.Surface.Duplicate();
            var curve = Value.Curve;
            xmorph.Morph(surface);
            return new GH_CurveOnSurface(CurveOnSurface.Create(surface, curve));
        }
    }
}
