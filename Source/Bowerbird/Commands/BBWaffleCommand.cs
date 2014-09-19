using System;
using System.Drawing;
using System.Linq;
using Bowerbird.Crafting;
using Bowerbird.Getters;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace Bowerbird.Commands
{
    [System.Runtime.InteropServices.Guid("E73414A7-1D9D-4999-A5D5-A90BD3371B48")]
    public class BBWaffleCommand : Command
    {
        public BBWaffleCommand()
        {
            Instance = this;
        }

        public static BBWaffleCommand Instance { get; private set; }

        public override string EnglishName
        {
            get { return "BBWaffle"; }
        }

        private Mesh _mesh;
        private Plane _plane = Plane.WorldXY;
        private double _unit;
        private OptionInteger _countX = new OptionInteger(3);
        private OptionInteger _countY = new OptionInteger(3);
        private OptionDouble _thickness = new OptionDouble(1.0);
        private OptionDouble _deeper = new OptionDouble(0.0);
        private OptionToggle _deleteSource = new OptionToggle(false, "No", "Yes");
        private OptionToggle _project = new OptionToggle(true, "No", "Yes");
        private OptionDouble _projectSpace = new OptionDouble(1.0);

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // --- Set tolerance

            _unit = doc.ModelAbsoluteTolerance;


            // --- Get Mesh

            var getMesh = new GetObject { GeometryFilter = ObjectType.Mesh };
            getMesh.SetCommandPrompt("Select mesh");

            while (true)
            {
                var getMeshResult = getMesh.Get();

                if (getMesh.CommandResult() != Result.Success)
                    return getMesh.CommandResult();

                if (getMeshResult == GetResult.Object)
                    break;
            }

            _mesh = (Mesh)getMesh.Object(0).Geometry();

            doc.Objects.UnselectAll();


            // --- Start preview
            
            DisplayPipeline.DrawOverlay += DisplayPipelineOnDrawOverlay;


            // --- Get options

            var getOptions = new GetOption();
            getOptions.SetCommandPrompt("Settings:");
            getOptions.AddOptionInteger("CountX", ref _countX);
            getOptions.AddOptionInteger("CountY", ref _countY);
            getOptions.AddOptionDouble("Thickness", ref _thickness);
            getOptions.AddOption("Plane");
            getOptions.AddOptionDouble("Deeper", ref _deeper);
            getOptions.AddOptionToggle("DeleteInput", ref _deleteSource);
            getOptions.AddOptionToggle("Project", ref _project);
            getOptions.AddOptionDouble("ProjectSpace", ref _projectSpace);
            getOptions.AcceptNothing(true);

            do
            {
                doc.Views.Redraw();

                getOptions.Get();

                switch (getOptions.Result())
                {
                    case GetResult.Option:
                        switch (getOptions.Option().EnglishName)
                        {
                            case "Plane":
                                _plane = PlaneGetter.GetPlane() ?? _plane;
                                break;
                        }
                        break;
                }
            } while (getOptions.CommandResult() == Result.Success && getOptions.Result() != GetResult.Nothing);


            // --- Stop preview

            DisplayPipeline.DrawOverlay -= DisplayPipelineOnDrawOverlay;


            // --- Check for cancel with ESC

            if (getOptions.Result() == GetResult.Cancel)
                return Result.Cancel;


            // --- Execute

            var result = Waffle.Create(_mesh, _plane, _thickness.CurrentValue, _deeper.CurrentValue, _countX.CurrentValue, _countY.CurrentValue, _unit, _project.CurrentValue, _projectSpace.CurrentValue);


            // --- Add objects

            foreach (var curves in result.CurvesX)
                doc.Groups.Add(curves.Select(o => doc.Objects.AddCurve(o)));

            foreach (var curves in result.CurvesY)
                doc.Groups.Add(curves.Select(o => doc.Objects.AddCurve(o)));


            // --- Delete input

            if (_deleteSource.CurrentValue)
                doc.Objects.Delete(getMesh.Object(0).ObjectId, true);


            // --- Update views

            doc.Views.Redraw();


            return Result.Success;
        }

        private void DisplayPipelineOnDrawOverlay(object sender, DrawEventArgs drawEventArgs)
        {
            if (_mesh == null)
                return;

            var di = drawEventArgs.Display;

            var result = Waffle.Create(_mesh, _plane, _thickness.CurrentValue, _deeper.CurrentValue, _countX.CurrentValue, _countY.CurrentValue, _unit, _project.CurrentValue, _projectSpace.CurrentValue);

            foreach (var curve in result.CurvesX.SelectMany(o => o))
                di.DrawCurve(curve, Color.White);

            foreach (var curve in result.CurvesY.SelectMany(o => o))
                di.DrawCurve(curve, Color.White);
        }
    }
}
