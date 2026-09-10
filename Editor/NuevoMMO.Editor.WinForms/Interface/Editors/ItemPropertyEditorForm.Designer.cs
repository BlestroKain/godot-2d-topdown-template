#nullable enable
using System.ComponentModel;
using NuevoMMO.Core;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class ItemPropertyEditorForm
{
    private IContainer? components;
    private NumericUpDown minimumNumeric = null!;
    private NumericUpDown maximumNumeric = null!;
    private ComboBox modifierCombo = null!;
    private ComboBox statCombo = null!;
    private CheckBox appliesItemsCheck = null!;
    private CheckBox appliesResourcesCheck = null!;
    private TextBox unitTextBox = null!;
    private NumericUpDown precisionNumeric = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        minimumNumeric = new NumericUpDown { DecimalPlaces = 2, Minimum = -1_000_000, Maximum = 1_000_000 };
        maximumNumeric = new NumericUpDown { DecimalPlaces = 2, Minimum = -1_000_000, Maximum = 1_000_000 };
        modifierCombo = new ComboBox();
        statCombo = new ComboBox();
        appliesItemsCheck = new CheckBox { AutoSize = true, Text = "Aplica a items", Checked = true };
        appliesResourcesCheck = new CheckBox { AutoSize = true, Text = "Aplica a recursos" };
        unitTextBox = new TextBox();
        precisionNumeric = new NumericUpDown { Maximum = 8 };
        SuspendLayout();
        Name = "ItemPropertyEditorForm";
        Text = "Propiedades de item";
        specificTabPage.Text = "Propiedad";
        FillEnum<ModifierType>(modifierCombo);
        FillEnum<StatId>(statCombo);
        AddSpecificRow(0, "Mínimo", minimumNumeric);
        AddSpecificRow(1, "Máximo", maximumNumeric);
        AddSpecificRow(2, "Modificador", modifierCombo);
        AddSpecificRow(3, "Stat", statCombo);
        AddSpecificRow(4, "Items", appliesItemsCheck);
        AddSpecificRow(5, "Recursos", appliesResourcesCheck);
        AddSpecificRow(6, "Unidad", unitTextBox);
        AddSpecificRow(7, "Precisión", precisionNumeric);
        ResumeLayout(false);
    }
}
