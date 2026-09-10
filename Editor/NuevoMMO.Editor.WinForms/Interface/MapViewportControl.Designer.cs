#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class MapViewportControl
{
    private IContainer? components;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            animationTimer.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        SuspendLayout();
        BackColor = Color.FromArgb(28, 30, 34);
        Dock = DockStyle.Fill;
        Name = "MapViewportControl";
        TabStop = true;
        ResumeLayout(false);
    }
}
