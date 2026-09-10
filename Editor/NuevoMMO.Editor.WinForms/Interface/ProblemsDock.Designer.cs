#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

partial class ProblemsDock
{
    private IContainer? components;
    private ListBox list = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        list = new ListBox();
        SuspendLayout();

        list.Dock = DockStyle.Fill;
        list.HorizontalScrollbar = true;
        list.IntegralHeight = false;
        list.Name = "list";

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(960, 180);
        Controls.Add(list);
        HideOnClose = true;
        Name = "ProblemsDock";
        ShowHint = DockState.DockBottom;
        TabText = "Problemas";
        Text = "Problemas";

        ResumeLayout(false);
    }
}
