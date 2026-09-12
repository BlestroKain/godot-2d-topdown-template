#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class ResourceEditorForm
{
    private IContainer? components;
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
        var harvest = AddGroup("Recolección");
        AddGroupRow(harvest, "Visual agotado", exhaustedVisualTextBox);
        AddGroupRow(harvest, "Loot table", lootTableCombo);
        AddGroupRow(harvest, "Profesión", professionCombo);
        AddGroupRow(harvest, "Nivel profesión", professionLevelNumeric);
        AddGroupRow(harvest, "Herramienta", toolKeyTextBox);
        AddGroupRow(harvest, "Respawn ms", respawnNumeric);
        AddGroupRow(harvest, "Bloqueo disponible", blockAvailableCheck);
        AddGroupRow(harvest, "Bloqueo agotado", blockExhaustedCheck);
        ResumeLayout(false);
    }
}
