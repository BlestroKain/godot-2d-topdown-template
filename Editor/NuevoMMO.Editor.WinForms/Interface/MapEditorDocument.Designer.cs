#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

partial class MapEditorDocument
{
    private IContainer? components;
    private MapViewportControl viewport = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        viewport = new MapViewportControl();
        SuspendLayout();

        viewport.Dock = DockStyle.Fill;
        viewport.Name = "viewport";
        viewport.TabIndex = 0;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(960, 640);
        CloseButton = false;
        CloseButtonVisible = false;
        Controls.Add(viewport);
        Name = "MapEditorDocument";
        ShowHint = DockState.Document;
        TabText = "Mapa";
        Text = "Mapa";

        ResumeLayout(false);
    }
}
