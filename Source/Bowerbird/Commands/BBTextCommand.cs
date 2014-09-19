using System.Linq;
using Bowerbird.Dialogs;
using Bowerbird.Text;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;

namespace Bowerbird.Commands
{
    [System.Runtime.InteropServices.Guid("F9E4B042-86AD-43C9-9707-E623AB8D6A01")]
    public class BBTextCommand : Command
    {
        public BBTextCommand()
        {
            Instance = this;
        }

        public static BBTextCommand Instance { get; private set; }
        
        public override string EnglishName
        {
            get { return "BBText"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var gp = new GetPoint();
            gp.SetCommandPrompt("Start:");
            
            if (gp.Get() != GetResult.Point)
            {
                RhinoApp.WriteLine("No point selected.");
                return gp.CommandResult();
            }

            var location = gp.Point();

            var dialog = new BBTextDialog();

            if (dialog.ShowDialog()!= true)
                return Result.Cancel;

            var typeWriter = dialog.Bold ? Typewriter.Bold : Typewriter.Regular;

            var x = doc.Views.ActiveView.ActiveViewport.CameraX;
            var y = doc.Views.ActiveView.ActiveViewport.CameraY;

            var unitX = x * dialog.Size;
            var unitY = y * dialog.Size;

            var curves = typeWriter.Write(dialog.Text, location, unitX, unitY, dialog.HAlign, dialog.VAlign);
            
            doc.Groups.Add(curves.Select(curve => doc.Objects.AddCurve(curve)));

            doc.Views.Redraw();

            return Result.Success;
        }
    }
}
