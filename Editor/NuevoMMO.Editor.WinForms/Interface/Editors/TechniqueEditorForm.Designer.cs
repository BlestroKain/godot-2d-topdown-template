#nullable enable
using System.ComponentModel;
using NuevoMMO.Core;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class TechniqueEditorForm
{
    private IContainer? components;
    private ComboBox elementCombo = null!;
    private ComboBox targetModeCombo = null!;
    private NumericUpDown rangeNumeric = null!;
    private NumericUpDown radiusNumeric = null!;
    private NumericUpDown maxTargetsNumeric = null!;
    private CheckBox lineOfSightCheck = null!;
    private NumericUpDown castNumeric = null!;
    private NumericUpDown cooldownNumeric = null!;
    private TextBox cooldownGroupTextBox = null!;
    private NumericUpDown manaCostNumeric = null!;
    private TextBox actionsTextBox = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        elementCombo = new ComboBox();
        targetModeCombo = new ComboBox();
        rangeNumeric = new NumericUpDown { DecimalPlaces = 1, Maximum = 100000 };
        radiusNumeric = new NumericUpDown { DecimalPlaces = 1, Maximum = 100000 };
        maxTargetsNumeric = new NumericUpDown { Minimum = 1, Maximum = 100, Value = 1 };
        lineOfSightCheck = new CheckBox { AutoSize = true, Text = "Requiere línea de visión", Checked = true };
        castNumeric = new NumericUpDown { Maximum = 600_000, Increment = 50 };
        cooldownNumeric = new NumericUpDown { Maximum = 600_000, Increment = 50 };
        cooldownGroupTextBox = new TextBox();
        manaCostNumeric = new NumericUpDown { Maximum = 100000 };
        actionsTextBox = new TextBox { AcceptsReturn = true, Multiline = true, ScrollBars = ScrollBars.Vertical };
        SuspendLayout();
        Name = "TechniqueEditorForm";
        Text = "Técnicas / Spells";
        specificTabPage.Text = "Técnica";
        FillEnum<Element>(elementCombo);
        FillEnum<TechniqueTargetMode>(targetModeCombo);
        var targeting = AddGroup("Objetivo");
        AddGroupRow(targeting, "Elemento", elementCombo);
        AddGroupRow(targeting, "Objetivo", targetModeCombo);
        AddGroupRow(targeting, "Alcance", rangeNumeric);
        AddGroupRow(targeting, "Radio", radiusNumeric);
        AddGroupRow(targeting, "Máx. objetivos", maxTargetsNumeric);
        AddGroupRow(targeting, "Línea de visión", lineOfSightCheck);

        var timing = AddGroup("Tiempos y coste");
        AddGroupRow(timing, "Casteo ms", castNumeric);
        AddGroupRow(timing, "Cooldown ms", cooldownNumeric);
        AddGroupRow(timing, "Grupo cooldown", cooldownGroupTextBox);
        AddGroupRow(timing, "Coste PM", manaCostNumeric);

        var actions = AddGroup("Acciones");
        AddGroupRow(actions, "Kind, Amount, Element, Moment", actionsTextBox, 90);
        ResumeLayout(false);
    }
}
