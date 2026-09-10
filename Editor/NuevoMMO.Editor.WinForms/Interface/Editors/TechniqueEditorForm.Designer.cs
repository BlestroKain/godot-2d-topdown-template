#nullable enable
using System.ComponentModel;
using NuevoMMO.Core;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class TechniqueEditorForm
{
    private IContainer? components;
    private TextBox visualKeyTextBox = null!;
    private ComboBox elementCombo = null!;
    private ComboBox targetModeCombo = null!;
    private NumericUpDown rangeNumeric = null!;
    private NumericUpDown radiusNumeric = null!;
    private NumericUpDown maxTargetsNumeric = null!;
    private CheckBox lineOfSightCheck = null!;
    private NumericUpDown castNumeric = null!;
    private NumericUpDown cooldownNumeric = null!;
    private TextBox cooldownGroupTextBox = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        visualKeyTextBox = new TextBox();
        elementCombo = new ComboBox();
        targetModeCombo = new ComboBox();
        rangeNumeric = new NumericUpDown { DecimalPlaces = 1, Maximum = 100000 };
        radiusNumeric = new NumericUpDown { DecimalPlaces = 1, Maximum = 100000 };
        maxTargetsNumeric = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 1 };
        lineOfSightCheck = new CheckBox { AutoSize = true, Text = "Requiere línea de visión", Checked = true };
        castNumeric = new NumericUpDown { Maximum = 600_000, Increment = 50 };
        cooldownNumeric = new NumericUpDown { Maximum = 600_000, Increment = 50 };
        cooldownGroupTextBox = new TextBox();
        SuspendLayout();
        Name = "TechniqueEditorForm";
        Text = "Técnicas / Spells";
        specificTabPage.Text = "Técnica";
        FillEnum<Element>(elementCombo);
        FillEnum<TechniqueTargetMode>(targetModeCombo);
        AddSpecificRow(0, "VisualKey", visualKeyTextBox);
        AddSpecificRow(1, "Elemento", elementCombo);
        AddSpecificRow(2, "Objetivo", targetModeCombo);
        AddSpecificRow(3, "Alcance", rangeNumeric);
        AddSpecificRow(4, "Radio", radiusNumeric);
        AddSpecificRow(5, "Máx. objetivos", maxTargetsNumeric);
        AddSpecificRow(6, "Línea de visión", lineOfSightCheck);
        AddSpecificRow(7, "Casteo ms", castNumeric);
        AddSpecificRow(8, "Cooldown ms", cooldownNumeric);
        AddSpecificRow(9, "Grupo cooldown", cooldownGroupTextBox);
        ResumeLayout(false);
    }
}
