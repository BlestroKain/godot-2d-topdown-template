#nullable enable
using System.ComponentModel;
using NuevoMMO.Core;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class SpawnTableEditorForm
{
    private IContainer? components;
    private ComboBox selectionModeCombo = null!;
    private NumericUpDown maxAliveNumeric = null!;
    private TextBox entriesTextBox = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        selectionModeCombo = new ComboBox();
        maxAliveNumeric = new NumericUpDown { Maximum = 10000 };
        entriesTextBox = new TextBox { AcceptsReturn = true, Multiline = true, ScrollBars = ScrollBars.Vertical };
        SuspendLayout();
        Name = "SpawnTableEditorForm";
        Text = "Spawn Tables";
        specificTabPage.Text = "Spawn";
        FillEnum<SpawnSelectionMode>(selectionModeCombo);
        AddSpecificRow(0, "Selección", selectionModeCombo);
        AddSpecificRow(1, "Máx. vivos", maxAliveNumeric);
        AddSpecificRow(2, "Entradas kind,clave,peso,max", entriesTextBox, 160);
        ResumeLayout(false);
    }
}
