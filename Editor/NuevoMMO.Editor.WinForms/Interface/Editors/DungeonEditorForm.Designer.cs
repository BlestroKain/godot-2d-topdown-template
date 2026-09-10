#nullable enable
using System.ComponentModel;
using NuevoMMO.Core;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class DungeonEditorForm
{
    private IContainer? components;
    private ComboBox mapCombo = null!;
    private ComboBox instanceModeCombo = null!;
    private NumericUpDown resetNumeric = null!;
    private NumericUpDown minPartyNumeric = null!;
    private NumericUpDown maxPartyNumeric = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        mapCombo = new ComboBox();
        instanceModeCombo = new ComboBox();
        resetNumeric = new NumericUpDown { Maximum = 86_400_000, Increment = 1000 };
        minPartyNumeric = new NumericUpDown { Minimum = 1, Maximum = 40, Value = 1 };
        maxPartyNumeric = new NumericUpDown { Maximum = 40 };
        SuspendLayout();
        Name = "DungeonEditorForm";
        Text = "Dungeons";
        specificTabPage.Text = "Dungeon";
        FillEnum<DungeonInstanceMode>(instanceModeCombo);
        AddSpecificRow(0, "Mapa de entrada", mapCombo);
        AddSpecificRow(1, "Instancia", instanceModeCombo);
        AddSpecificRow(2, "Reset ms", resetNumeric);
        AddSpecificRow(3, "Party mín", minPartyNumeric);
        AddSpecificRow(4, "Party máx", maxPartyNumeric);
        ResumeLayout(false);
    }
}
