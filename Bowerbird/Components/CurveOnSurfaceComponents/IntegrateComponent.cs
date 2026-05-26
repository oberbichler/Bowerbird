using Bowerbird.Curvature;
using Bowerbird.Parameters;
using Bowerbird.Types;
using Bowerbird.Components;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using System;
using System.Windows.Forms;

namespace Bowerbird.Components.CurveOnSurfaceComponents;

public enum ValueTypes
{
    NormalCurvature = 0,
    GeodesicCurvature = 1,
    GeodesicTorsion = 2
}

public class IntegrateComponent : GH_Component
{
    public IntegrateComponent() 
        : base("BB Integrate CurveOnSurface", "BBIntegrate", "Integrate the curvature of an embedded curve over a specified domain.", "Bowerbird", "Curve on Surface")
    {
        UpdateMessage();
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
        pManager.AddParameter(new CurveOnSurfaceParameter(), "Curve on Surface", "C", "Embedded curve to evaluate", GH_ParamAccess.item);
        pManager.AddNumberParameter("Start Parameter", "A", "Start of the integration domain (default: start of the curve domain)", GH_ParamAccess.item);
        pManager.AddNumberParameter("End Parameter", "B", "End of the integration domain (default: end of the curve domain)", GH_ParamAccess.item);
        pManager.AddNumberParameter("Tolerance", "T", "Tolerance for the adaptive numerical integration", GH_ParamAccess.item, 1e-6);
        pManager.AddIntegerParameter("Maximum Iterations", "N", "Iteration limit for the numerical integration", GH_ParamAccess.item, 1000);

        pManager[1].Optional = true;
        pManager[2].Optional = true;
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
        pManager.AddNumberParameter("Integral", "V", "Integral of the selected value", GH_ParamAccess.item);
        pManager.AddNumberParameter("Integral of squared", "V2", "Integral of the squared selected value", GH_ParamAccess.item);
    }

    private ValueTypes _valueType = ValueTypes.NormalCurvature;

    public ValueTypes ValueType
    {
        get => _valueType;
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
        GHUtility.SetMenuList(this, menu, "Change value type", () => ValueType, o => ValueType = o);
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

    private Func<double, double> Evaluate(IOrientableCurve curve, ValueTypes valueType, bool squared)
    {
        if (!squared)
        {
            return valueType switch
            {
                ValueTypes.NormalCurvature => o => curve.NormalCurvatureAt(o).Length * curve.Ds(o),
                ValueTypes.GeodesicCurvature => o => curve.GeodesicCurvatureAt(o).Length * curve.Ds(o),
                ValueTypes.GeodesicTorsion => o => curve.GeodesicTorsionAt(o).Length * curve.Ds(o),
                _ => throw new ArgumentException("Invalid value type.")
            };
        }
        else
        {
            return valueType switch
            {
                ValueTypes.NormalCurvature => o => curve.NormalCurvatureAt(o).SquareLength * curve.Ds(o),
                ValueTypes.GeodesicCurvature => o => curve.GeodesicCurvatureAt(o).SquareLength * curve.Ds(o),
                ValueTypes.GeodesicTorsion => o => curve.GeodesicTorsionAt(o).SquareLength * curve.Ds(o),
                _ => throw new ArgumentException("Invalid value type.")
            };
        }
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
        var ghCurveOnSurface = default(GH_CurveOnSurface);
        var a = default(double);
        var b = default(double);
        var tolerance = default(double);
        var maximumIterations = default(int);

        if (!DA.GetData(0, ref ghCurveOnSurface)) return;
        if (ghCurveOnSurface == null || ghCurveOnSurface.Value == null) return;

        var curveOnSurface = ghCurveOnSurface.Value;

        // Default parameters from domain
        if (!DA.GetData(1, ref a)) a = curveOnSurface.Domain.T0;
        if (!DA.GetData(2, ref b)) b = curveOnSurface.Domain.T1;
        if (!DA.GetData(3, ref tolerance)) return;
        if (!DA.GetData(4, ref maximumIterations)) return;

        var value = Integrate.Romberg(Evaluate(curveOnSurface, ValueType, false), a, b, tolerance, maximumIterations);
        var value2 = Integrate.Romberg(Evaluate(curveOnSurface, ValueType, true), a, b, tolerance, maximumIterations);

        DA.SetData(0, value);
        DA.SetData(1, value2);
    }

    protected override System.Drawing.Bitmap? Icon => Bowerbird.Properties.Resources.icon_curve_on_surface_integrate;

    public override GH_Exposure Exposure => GH_Exposure.secondary;

    public override Guid ComponentGuid => new("{6DADDD29-0D28-406F-8354-35179502ED43}");
}
