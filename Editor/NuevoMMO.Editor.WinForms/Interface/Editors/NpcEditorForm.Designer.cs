#nullable enable
using System.ComponentModel;
using NuevoMMO.Core;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class NpcEditorForm
{
    private IContainer? components;
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

        var identity = AddGroup("NPC");
        AddGroupRow(identity, "Combate", combatEnabledCheck);
        AddGroupRow(identity, "Agresivo", aggressiveCheck);
        AddGroupRow(identity, "Movimiento", movementCombo);
        AddGroupRow(identity, "Visión", sightNumeric);
        AddGroupRow(identity, "Radio reset", resetNumeric);

        var combat = AddGroup("Combate");
        AddGroupRow(combat, "Nivel", levelNumeric);
        AddGroupRow(combat, "Vida", healthNumeric);
        AddGroupRow(combat, "Maná", manaNumeric);
        AddGroupRow(combat, "Daño base", damageNumeric);
        AddGroupRow(combat, "Elemento", elementCombo);
        AddGroupRow(combat, "Experiencia", experienceNumeric);

        var loot = AddGroup("Botín");
        AddGroupRow(loot, "Loot table", lootTableCombo);
        AddGroupRow(loot, "Loot mode", lootModeCombo);
        ResumeLayout(false);
    }
}
