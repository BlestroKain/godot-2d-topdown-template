#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class TilesetEditorForm
{
    private IContainer? components;
    private TextBox textureKeyTextBox = null!;
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
        textureKeyTextBox = new TextBox();
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
        AddSpecificRow(0, "TextureKey", textureKeyTextBox);
        AddSpecificRow(1, "Tile ancho", tileWidthNumeric);
        AddSpecificRow(2, "Tile alto", tileHeightNumeric);
        AddSpecificRow(3, "Frames autotile", autotileFramesNumeric);
        AddSpecificRow(4, "Autotile ms", autotileMsNumeric);
        AddSpecificRow(5, "Frames waterfall", waterfallFramesNumeric);
        AddSpecificRow(6, "Waterfall ms", waterfallMsNumeric);
        ResumeLayout(false);
    }
}
