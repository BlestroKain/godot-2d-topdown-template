#nullable enable
using System.ComponentModel;
using NuevoMMO.Core;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class EffectEditorForm
{
    private IContainer? components;
    private TextBox visualKeyTextBox = null!;
    private ComboBox dispositionCombo = null!;
    private NumericUpDown durationNumeric = null!;
    private NumericUpDown tickNumeric = null!;
    private ComboBox stackPolicyCombo = null!;
    private NumericUpDown maxStacksNumeric = null!;
    private CheckBox dispellableCheck = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        visualKeyTextBox = new TextBox();
        dispositionCombo = new ComboBox();
        durationNumeric = new NumericUpDown { Maximum = 3_600_000, Increment = 100 };
        tickNumeric = new NumericUpDown { Maximum = 3_600_000, Increment = 100 };
        stackPolicyCombo = new ComboBox();
        maxStacksNumeric = new NumericUpDown { Minimum = 1, Maximum = 99, Value = 1 };
        dispellableCheck = new CheckBox { AutoSize = true, Text = "Disipable", Checked = true };
        SuspendLayout();
        Name = "EffectEditorForm";
        Text = "Efectos";
        specificTabPage.Text = "Efecto";
        FillEnum<EffectDisposition>(dispositionCombo);
        FillEnum<EffectStackPolicy>(stackPolicyCombo);
        AddSpecificRow(0, "VisualKey", visualKeyTextBox);
        AddSpecificRow(1, "Disposición", dispositionCombo);
        AddSpecificRow(2, "Duración ms", durationNumeric);
        AddSpecificRow(3, "Tick ms", tickNumeric);
        AddSpecificRow(4, "Stacks", stackPolicyCombo);
        AddSpecificRow(5, "Máx. stacks", maxStacksNumeric);
        AddSpecificRow(6, "Disipable", dispellableCheck);
        ResumeLayout(false);
    }
}
