using Bowerbird.Parameters;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Bowerbird
{
    public class IntegrateComponent : GH_Component
    {
        public IntegrateComponent() : base("BB Integrate CurveOnSurface", "BBIntegrate", "Beta! Interface might change!", "Bowerbird", "Curve on Surface")
        {
            ValueType = ValueTypes.NormalCurvature;
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new CurveOnSurfaceParameter(), "Curve on Surface", "C", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("Start Parameter", "A", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("End Parameter", "B", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("Tolerance", "T", "", GH_ParamAccess.item, 1e-6);
            pManager.AddIntegerParameter("Maximum Iterations", "N", "", GH_ParamAccess.item, 1000);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Integral", "V", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("Integral of squared", "V2", "", GH_ParamAccess.item);
        }

        Func<double, double> Evaluate(IOrientableCurve curve, bool squared)
        {
            if (!squared)
            {
                switch (ValueType)
                {
                    case ValueTypes.NormalCurvature:
                        return o => curve.NormalCurvatureAt(o).Length;
                    case ValueTypes.GeodesicCurvature:
                        return o => curve.GeodesicCurvatureAt(o).Length;
                    case ValueTypes.GeodesicTorsion:
                        return o => curve.GeodesicTorsionAt(o).Length;
                    default:
                        throw new Exception();
                }
            }
            else
            {
                switch (ValueType)
                {
                    case ValueTypes.NormalCurvature:
                        return o => curve.NormalCurvatureAt(o).SquareLength;
                    case ValueTypes.GeodesicCurvature:
                        return o => curve.GeodesicCurvatureAt(o).SquareLength;
                    case ValueTypes.GeodesicTorsion:
                        return o => curve.GeodesicTorsionAt(o).SquareLength;
                    default:
                        throw new Exception();
                }
            }
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // --- Input

            var curveOnSurface = default(IOrientableCurve);
            var a = default(double);
            var b = default(double);
            var tolerance = default(double);
            var maximumIterations = default(int);

            if (!DA.GetData(0, ref curveOnSurface)) return;
            if (!DA.GetData(1, ref a))
                a = curveOnSurface.Domain.T0;
            if (!DA.GetData(2, ref b))
                b = curveOnSurface.Domain.T1;
            if (!DA.GetData(3, ref tolerance)) return;
            if (!DA.GetData(4, ref maximumIterations)) return;

            // --- Execute

            var value = Integrate.Romberg(Evaluate(curveOnSurface, false), a, b, tolerance, maximumIterations);
            var value2 = Integrate.Romberg(Evaluate(curveOnSurface, true), a, b, tolerance, maximumIterations);

            // --- Output

            DA.SetData(0, value);
            DA.SetData(1, value2);
        }

        protected override Bitmap Icon => Properties.Resources.icon_curve_on_surface_integrate;

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        public override Guid ComponentGuid => new Guid("{6DADDD29-0D28-406F-8354-35179502ED43}");

        public enum ValueTypes
        {
            NormalCurvature = 0,
            GeodesicCurvature = 1,
            GeodesicTorsion = 2
        }

        private ValueTypes _valueType;

        public ValueTypes ValueType
        {
            get
            {
                return _valueType;
            }
            set
            {
                _valueType = value;
                UpdateMessage();
            }
        }

        private void UpdateMessage()
        {
            Message = ValueType.ToString();
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            Utility.SetMenuList(this, menu, "", () => ValueType, o => ValueType = o);
        }

        public override bool Write(GH_IWriter writer)
        {
            writer.Set("ValueType", ValueType);

            return base.Write(writer);
        }

        public override bool Read(GH_IReader reader)
        {
            ValueType = reader.GetOrDefault("ValueType", ValueTypes.NormalCurvature);

            return base.Read(reader);
        }
    }
}