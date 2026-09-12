#nullable enable
using System.ComponentModel;
using NuevoMMO.Core;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class MobEditorForm
{
    private IContainer? components;
    private ComboBox lootTableCombo = null!;
    private ComboBox lootModeCombo = null!;
    private CheckBox aggressiveCheck = null!;
    private CheckBox attackAlliesCheck = null!;
    private CheckBox swarmCheck = null!;
    private CheckBox npcVsNpcCheck = null!;
    private ComboBox movementCombo = null!;
    private ComboBox targetPriorityCombo = null!;
    private NumericUpDown fleeNumeric = null!;
    private NumericUpDown sightNumeric = null!;
    private NumericUpDown resetNumeric = null!;
    private NumericUpDown levelNumeric = null!;
    private NumericUpDown experienceNumeric = null!;
    private NumericUpDown damageNumeric = null!;
    private ComboBox elementCombo = null!;
    private NumericUpDown critChanceNumeric = null!;
    private NumericUpDown critMultiplierNumeric = null!;
    private NumericUpDown tenacityNumeric = null!;
    private NumericUpDown attackIntervalNumeric = null!;
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
        lootTableCombo = new ComboBox();
        lootModeCombo = new ComboBox();
        aggressiveCheck = new CheckBox { AutoSize = true, Text = "Agresivo" };
        attackAlliesCheck = new CheckBox { AutoSize = true, Text = "Ataca aliados" };
        swarmCheck = new CheckBox { AutoSize = true, Text = "Enjambre" };
        npcVsNpcCheck = new CheckBox { AutoSize = true, Text = "NPC vs NPC" };
        movementCombo = new ComboBox();
        targetPriorityCombo = new ComboBox();
        fleeNumeric = new NumericUpDown { Maximum = 100 };
        sightNumeric = new NumericUpDown { DecimalPlaces = 1, Maximum = 100000, Increment = 0.5M };
        resetNumeric = new NumericUpDown { DecimalPlaces = 1, Maximum = 100000, Increment = 0.5M };
        levelNumeric = new NumericUpDown { Minimum = 1, Maximum = 10000, Value = 1 };
        experienceNumeric = new NumericUpDown { Maximum = 1_000_000_000 };
        damageNumeric = new NumericUpDown { DecimalPlaces = 1, Maximum = 1_000_000 };
        elementCombo = new ComboBox();
        critChanceNumeric = new NumericUpDown { DecimalPlaces = 1, Maximum = 100 };
        critMultiplierNumeric = new NumericUpDown { DecimalPlaces = 2, Maximum = 100, Value = 1.5M };
        tenacityNumeric = new NumericUpDown { DecimalPlaces = 1, Maximum = 1000 };
        attackIntervalNumeric = new NumericUpDown { Maximum = 600_000, Increment = 50 };
        healthNumeric = new NumericUpDown { Maximum = 1_000_000 };
        manaNumeric = new NumericUpDown { Maximum = 1_000_000 };

        SuspendLayout();
        Name = "MobEditorForm";
        Text = "Mobs";
        specificTabPage.Text = "Mob";

        FillEnum<LootMode>(lootModeCombo);
        FillEnum<CreatureMovementMode>(movementCombo);
        FillEnum<CreatureTargetPriority>(targetPriorityCombo);
        FillEnum<Element>(elementCombo);

        var combat = AddGroup("Combate");
        AddGroupRow(combat, "Nivel", levelNumeric);
        AddGroupRow(combat, "Vida", healthNumeric);
        AddGroupRow(combat, "Maná", manaNumeric);
        AddGroupRow(combat, "Daño base", damageNumeric);
        AddGroupRow(combat, "Elemento", elementCombo);
        AddGroupRow(combat, "Experiencia", experienceNumeric);
        AddGroupRow(combat, "Crítico %", critChanceNumeric);
        AddGroupRow(combat, "Mult. crítico", critMultiplierNumeric);
        AddGroupRow(combat, "Tenacidad", tenacityNumeric);
        AddGroupRow(combat, "Intervalo ataque ms", attackIntervalNumeric);

        var ai = AddGroup("Comportamiento");
        AddGroupRow(ai, "Agresivo", aggressiveCheck);
        AddGroupRow(ai, "Ataca aliados", attackAlliesCheck);
        AddGroupRow(ai, "Enjambre", swarmCheck);
        AddGroupRow(ai, "NPC vs NPC", npcVsNpcCheck);
        AddGroupRow(ai, "Movimiento", movementCombo);
        AddGroupRow(ai, "Prioridad", targetPriorityCombo);
        AddGroupRow(ai, "Huida % vida", fleeNumeric);
        AddGroupRow(ai, "Visión", sightNumeric);
        AddGroupRow(ai, "Radio reset", resetNumeric);

        var loot = AddGroup("Botín");
        AddGroupRow(loot, "Loot table", lootTableCombo);
        AddGroupRow(loot, "Loot mode", lootModeCombo);
        ResumeLayout(false);
    }
}
