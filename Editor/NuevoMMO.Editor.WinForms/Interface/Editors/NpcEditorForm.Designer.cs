#nullable enable
using System.ComponentModel;
using NuevoMMO.Core;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class NpcEditorForm
{
    private IContainer? components;
    private TextBox visualKeyTextBox = null!;
    private CheckBox combatEnabledCheck = null!;
    private ComboBox lootTableCombo = null!;
    private ComboBox lootModeCombo = null!;
    private CheckBox aggressiveCheck = null!;
    private ComboBox movementCombo = null!;
    private NumericUpDown sightNumeric = null!;
    private NumericUpDown resetNumeric = null!;
    private NumericUpDown levelNumeric = null!;
    private NumericUpDown experienceNumeric = null!;
    private NumericUpDown damageNumeric = null!;
    private ComboBox elementCombo = null!;
    private NumericUpDown healthNumeric = null!;
    private NumericUpDown manaNumeric = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        visualKeyTextBox = new TextBox();
        combatEnabledCheck = new CheckBox { AutoSize = true, Text = "Puede combatir" };
        lootTableCombo = new ComboBox();
        lootModeCombo = new ComboBox();
        aggressiveCheck = new CheckBox { AutoSize = true, Text = "Agresivo" };
        movementCombo = new ComboBox();
        sightNumeric = new NumericUpDown { DecimalPlaces = 1, Maximum = 100000, Increment = 0.5M };
        resetNumeric = new NumericUpDown { DecimalPlaces = 1, Maximum = 100000, Increment = 0.5M };
        levelNumeric = new NumericUpDown { Minimum = 1, Maximum = 10000, Value = 1 };
        experienceNumeric = new NumericUpDown { Maximum = 1_000_000_000 };
        damageNumeric = new NumericUpDown { DecimalPlaces = 1, Maximum = 1_000_000 };
        elementCombo = new ComboBox();
        healthNumeric = new NumericUpDown { Maximum = 1_000_000 };
        manaNumeric = new NumericUpDown { Maximum = 1_000_000 };

        SuspendLayout();
        Name = "NpcEditorForm";
        Text = "NPCs";
        specificTabPage.Text = "NPC";
        FillEnum<LootMode>(lootModeCombo);
        FillEnum<CreatureMovementMode>(movementCombo);
        FillEnum<Element>(elementCombo);

        AddSpecificRow(0, "VisualKey", visualKeyTextBox);
        AddSpecificRow(1, "Combate", combatEnabledCheck);
        AddSpecificRow(2, "Loot table", lootTableCombo);
        AddSpecificRow(3, "Loot mode", lootModeCombo);
        AddSpecificRow(4, "Agresivo", aggressiveCheck);
        AddSpecificRow(5, "Movimiento", movementCombo);
        AddSpecificRow(6, "Visión", sightNumeric);
        AddSpecificRow(7, "Radio reset", resetNumeric);
        AddSpecificRow(8, "Nivel", levelNumeric);
        AddSpecificRow(9, "Experiencia", experienceNumeric);
        AddSpecificRow(10, "Daño base", damageNumeric);
        AddSpecificRow(11, "Elemento", elementCombo);
        AddSpecificRow(12, "Vida", healthNumeric);
        AddSpecificRow(13, "Maná", manaNumeric);
        ResumeLayout(false);
    }
}
