#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

partial class TilesetPaletteDock
{
    private IContainer? components;
    private FlowLayoutPanel toolbar = null!;
    private ComboBox tilesets = null!;
    private Label autotileLabel = null!;
    private ComboBox autotile = null!;
    private Label zoomLabel = null!;
    private NumericUpDown zoom = null!;
    private Label selection = null!;
    private Panel scroller = null!;
    private TilesetPaletteSurface surface = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        toolbar = new FlowLayoutPanel();
        tilesets = new ComboBox();
        autotileLabel = new Label();
        autotile = new ComboBox();
        zoomLabel = new Label();
        zoom = new NumericUpDown();
        selection = new Label();
        scroller = new Panel();
        surface = new TilesetPaletteSurface();

        toolbar.SuspendLayout();
        scroller.SuspendLayout();
        ((ISupportInitialize)zoom).BeginInit();
        SuspendLayout();

        tilesets.DropDownStyle = ComboBoxStyle.DropDownList;
        tilesets.Name = "tilesets";
        tilesets.Width = 190;

        autotileLabel.AutoSize = true;
        autotileLabel.Name = "autotileLabel";
        autotileLabel.Padding = new Padding(5, 7, 0, 0);
        autotileLabel.Text = "Modo:";

        autotile.DropDownStyle = ComboBoxStyle.DropDownList;
        autotile.Name = "autotile";
        autotile.Width = 130;

        zoomLabel.AutoSize = true;
        zoomLabel.Name = "zoomLabel";
        zoomLabel.Padding = new Padding(5, 7, 0, 0);
        zoomLabel.Text = "Zoom:";

        zoom.Maximum = 4;
        zoom.Minimum = 1;
        zoom.Name = "zoom";
        zoom.Value = 1;
        zoom.Width = 48;

        selection.AutoSize = true;
        selection.Name = "selection";
        selection.Padding = new Padding(6, 7, 0, 0);

        toolbar.Controls.Add(tilesets);
        toolbar.Controls.Add(autotileLabel);
        toolbar.Controls.Add(autotile);
        toolbar.Controls.Add(zoomLabel);
        toolbar.Controls.Add(zoom);
        toolbar.Controls.Add(selection);
        toolbar.Dock = DockStyle.Top;
        toolbar.Height = 34;
        toolbar.Name = "toolbar";
        toolbar.Padding = new Padding(3);
        toolbar.WrapContents = false;

        surface.Location = Point.Empty;
        surface.Name = "surface";
        surface.Size = new Size(1, 1);

        scroller.AutoScroll = true;
        scroller.BackColor = Color.FromArgb(24, 26, 30);
        scroller.Controls.Add(surface);
        scroller.Dock = DockStyle.Fill;
        scroller.Name = "scroller";

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(360, 640);
        Controls.Add(scroller);
        Controls.Add(toolbar);
        HideOnClose = true;
        Name = "TilesetPaletteDock";
        ShowHint = DockState.DockLeft;
        TabText = "Tilesets";
        Text = "Tilesets";

        ((ISupportInitialize)zoom).EndInit();
        toolbar.ResumeLayout(false);
        toolbar.PerformLayout();
        scroller.ResumeLayout(false);
        ResumeLayout(false);
    }
}
