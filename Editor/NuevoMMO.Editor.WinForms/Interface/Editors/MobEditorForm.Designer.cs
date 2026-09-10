#nullable enable
using System.ComponentModel;
using NuevoMMO.Core;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class MobEditorForm
{
    private IContainer? components;
    private TextBox visualKeyTextBox = null!;
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
        visualKeyTextBox = new TextBox();
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

        AddSpecificRow(0, "VisualKey", visualKeyTextBox);
        AddSpecificRow(1, "Loot table", lootTableCombo);
        AddSpecificRow(2, "Loot mode", lootModeCombo);
        AddSpecificRow(3, "Agresivo", aggressiveCheck);
        AddSpecificRow(4, "Ataca aliados", attackAlliesCheck);
        AddSpecificRow(5, "Enjambre", swarmCheck);
        AddSpecificRow(6, "NPC vs NPC", npcVsNpcCheck);
        AddSpecificRow(7, "Movimiento", movementCombo);
        AddSpecificRow(8, "Prioridad", targetPriorityCombo);
        AddSpecificRow(9, "Huida % vida", fleeNumeric);
        AddSpecificRow(10, "Visión", sightNumeric);
        AddSpecificRow(11, "Radio reset", resetNumeric);
        AddSpecificRow(12, "Nivel", levelNumeric);
        AddSpecificRow(13, "Experiencia", experienceNumeric);
        AddSpecificRow(14, "Daño base", damageNumeric);
        AddSpecificRow(15, "Elemento", elementCombo);
        AddSpecificRow(16, "Crítico %", critChanceNumeric);
        AddSpecificRow(17, "Mult. crítico", critMultiplierNumeric);
        AddSpecificRow(18, "Tenacidad", tenacityNumeric);
        AddSpecificRow(19, "Intervalo ataque ms", attackIntervalNumeric);
        AddSpecificRow(20, "Vida", healthNumeric);
        AddSpecificRow(21, "Maná", manaNumeric);
        ResumeLayout(false);
    }
}
