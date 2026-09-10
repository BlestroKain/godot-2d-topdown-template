#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

partial class MapToolsDock
{
    private IContainer? components;
    private SplitContainer split = null!;
    private ListBox tools = null!;
    private ListBox layers = null!;
    private Label toolsLabel = null!;
    private Label layersLabel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        split = new SplitContainer();
        toolsLabel = new Label();
        tools = new ListBox();
        layersLabel = new Label();
        layers = new ListBox();

        ((ISupportInitialize)split).BeginInit();
        split.Panel1.SuspendLayout();
        split.Panel2.SuspendLayout();
        split.SuspendLayout();
        SuspendLayout();

        toolsLabel.Dock = DockStyle.Top;
        toolsLabel.Height = 22;
        toolsLabel.Name = "toolsLabel";
        toolsLabel.Padding = new Padding(6, 4, 0, 0);
        toolsLabel.Text = "Herramientas";

        tools.Dock = DockStyle.Fill;
        tools.IntegralHeight = false;
        tools.Name = "tools";

        layersLabel.Dock = DockStyle.Top;
        layersLabel.Height = 22;
        layersLabel.Name = "layersLabel";
        layersLabel.Padding = new Padding(6, 4, 0, 0);
        layersLabel.Text = "Capas";

        layers.Dock = DockStyle.Fill;
        layers.DisplayMember = "Label";
        layers.IntegralHeight = false;
        layers.Name = "layers";

        split.Dock = DockStyle.Fill;
        split.Name = "split";
        split.Orientation = Orientation.Horizontal;
        split.SplitterDistance = 260;
        split.Panel1.Controls.Add(tools);
        split.Panel1.Controls.Add(toolsLabel);
        split.Panel2.Controls.Add(layers);
        split.Panel2.Controls.Add(layersLabel);

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(240, 640);
        Controls.Add(split);
        HideOnClose = true;
        Name = "MapToolsDock";
        ShowHint = DockState.DockLeft;
        TabText = "Mapa / Capas";
        Text = "Mapa / Capas";

        split.Panel1.ResumeLayout(false);
        split.Panel2.ResumeLayout(false);
        ((ISupportInitialize)split).EndInit();
        split.ResumeLayout(false);
        ResumeLayout(false);
    }
}
