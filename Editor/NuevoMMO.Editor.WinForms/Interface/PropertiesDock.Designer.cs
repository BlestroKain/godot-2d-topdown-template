#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

partial class PropertiesDock
{
    private IContainer? components;
    private PropertyGrid propertyGrid = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        propertyGrid = new PropertyGrid();
        SuspendLayout();

        propertyGrid.Dock = DockStyle.Fill;
        propertyGrid.HelpVisible = true;
        propertyGrid.Name = "propertyGrid";
        propertyGrid.ToolbarVisible = true;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(320, 480);
        Controls.Add(propertyGrid);
        HideOnClose = true;
        Name = "PropertiesDock";
        ShowHint = DockState.DockRight;
        TabText = "Propiedades";
        Text = "Propiedades";

        ResumeLayout(false);
    }
}
