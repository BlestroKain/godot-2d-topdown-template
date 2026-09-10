#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

partial class ContentExplorerDock
{
    private IContainer? components;
    private TreeView tree = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        tree = new TreeView();
        SuspendLayout();

        tree.Dock = DockStyle.Fill;
        tree.HideSelection = false;
        tree.Name = "tree";
        tree.TabIndex = 0;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(280, 640);
        Controls.Add(tree);
        HideOnClose = true;
        Name = "ContentExplorerDock";
        ShowHint = DockState.DockRight;
        TabText = "Contenido";
        Text = "Contenido";

        ResumeLayout(false);
    }
}
