using Bowerbird.Curvature;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;

namespace Bowerbird.Types
{
    public class GH_CurveOnSurface : GH_GeometricGoo<IOrientableCurve>, IGH_PreviewData, IGH_BakeAwareData
    {
        public GH_CurveOnSurface() : base()
        {
        }

        public GH_CurveOnSurface(IOrientableCurve value) : base(value)
        {
        }

        public void UpdateGeometry()
        {
            Geometry = Value.ToCurve(RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
            ClippingBox = Geometry?.GetBoundingBox(false) ?? BoundingBox.Empty;
        }

        public Curve Geometry { get; private set; }

        public override bool IsValid => Value.IsValid;

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
                case IOrientableCurve curveOnSurface:
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
            if (typeof(Q).IsAssignableFrom(m_value.GetType()))
            {
                var obj = (object)m_value;
                target = (Q)obj;
                return true;
            }

            return false;
        }

        public override string ToString() => Value.ToString();

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            if (Geometry == null)
                UpdateGeometry();

            if (Geometry != null)
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
            return new GH_CurveOnSurface(Value.Transform(xform));
        }

        public override IGH_GeometricGoo Morph(SpaceMorph xmorph)
        {
            return new GH_CurveOnSurface(Value.Morph(xmorph));
        }
    }
}
