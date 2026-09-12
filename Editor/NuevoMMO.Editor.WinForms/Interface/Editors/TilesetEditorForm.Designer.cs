#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class TilesetEditorForm
{
    private IContainer? components;
    private NumericUpDown tileWidthNumeric = null!;
    private NumericUpDown tileHeightNumeric = null!;
    private NumericUpDown autotileFramesNumeric = null!;
    private NumericUpDown autotileMsNumeric = null!;
    private NumericUpDown waterfallFramesNumeric = null!;
    private NumericUpDown waterfallMsNumeric = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        tileWidthNumeric = new NumericUpDown { Minimum = 2, Maximum = 256, Value = 32 };
        tileHeightNumeric = new NumericUpDown { Minimum = 2, Maximum = 256, Value = 32 };
        autotileFramesNumeric = new NumericUpDown { Minimum = 1, Maximum = 32, Value = 3 };
        autotileMsNumeric = new NumericUpDown { Minimum = 1, Maximum = 60_000, Value = 600 };
        waterfallFramesNumeric = new NumericUpDown { Minimum = 1, Maximum = 32, Value = 3 };
        waterfallMsNumeric = new NumericUpDown { Minimum = 1, Maximum = 60_000, Value = 500 };
        SuspendLayout();
        Name = "TilesetEditorForm";
        Text = "Tilesets";
        specificTabPage.Text = "Tileset";
        AddSpecificRow(0, "Tile ancho", tileWidthNumeric);
        AddSpecificRow(1, "Tile alto", tileHeightNumeric);
        AddSpecificRow(2, "Frames autotile", autotileFramesNumeric);
        AddSpecificRow(3, "Autotile ms", autotileMsNumeric);
        AddSpecificRow(4, "Frames waterfall", waterfallFramesNumeric);
        AddSpecificRow(5, "Waterfall ms", waterfallMsNumeric);
        ResumeLayout(false);
    }
}
