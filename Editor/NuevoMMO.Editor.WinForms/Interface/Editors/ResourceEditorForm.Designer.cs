#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class ResourceEditorForm
{
    private IContainer? components;
    private TextBox visualKeyTextBox = null!;
    private TextBox exhaustedVisualTextBox = null!;
    private ComboBox lootTableCombo = null!;
    private ComboBox professionCombo = null!;
    private NumericUpDown professionLevelNumeric = null!;
    private TextBox toolKeyTextBox = null!;
    private NumericUpDown respawnNumeric = null!;
    private CheckBox blockAvailableCheck = null!;
    private CheckBox blockExhaustedCheck = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        visualKeyTextBox = new TextBox();
        exhaustedVisualTextBox = new TextBox();
        lootTableCombo = new ComboBox();
        professionCombo = new ComboBox();
        professionLevelNumeric = new NumericUpDown { Maximum = 10000 };
        toolKeyTextBox = new TextBox();
        respawnNumeric = new NumericUpDown { Maximum = 86_400_000, Increment = 1000 };
        blockAvailableCheck = new CheckBox { AutoSize = true, Text = "Bloquea movimiento (disponible)" };
        blockExhaustedCheck = new CheckBox { AutoSize = true, Text = "Bloquea movimiento (agotado)" };
        SuspendLayout();
        Name = "ResourceEditorForm";
        Text = "Recursos";
        specificTabPage.Text = "Recurso";
        AddSpecificRow(0, "VisualKey", visualKeyTextBox);
        AddSpecificRow(1, "Visual agotado", exhaustedVisualTextBox);
        AddSpecificRow(2, "Loot table", lootTableCombo);
        AddSpecificRow(3, "Profesión", professionCombo);
        AddSpecificRow(4, "Nivel profesión", professionLevelNumeric);
        AddSpecificRow(5, "Herramienta", toolKeyTextBox);
        AddSpecificRow(6, "Respawn ms", respawnNumeric);
        AddSpecificRow(7, "Bloqueo disponible", blockAvailableCheck);
        AddSpecificRow(8, "Bloqueo agotado", blockExhaustedCheck);
        ResumeLayout(false);
    }
}
