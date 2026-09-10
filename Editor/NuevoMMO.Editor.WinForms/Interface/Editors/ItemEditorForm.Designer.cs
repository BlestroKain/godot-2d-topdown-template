#nullable enable
using System.ComponentModel;
using NuevoMMO.Core;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class ItemEditorForm
{
    private IContainer? components;
    private TextBox visualKeyTextBox = null!;
    private ComboBox kindCombo = null!;
    private NumericUpDown rarityNumeric = null!;
    private NumericUpDown priceNumeric = null!;
    private CheckBox stackableCheck = null!;
    private NumericUpDown inventoryStackNumeric = null!;
    private NumericUpDown bankStackNumeric = null!;
    private CheckBox canDropCheck = null!;
    private CheckBox canTradeCheck = null!;
    private CheckBox canSellCheck = null!;
    private CheckBox canBankCheck = null!;
    private NumericUpDown dropChanceNumeric = null!;
    private NumericUpDown groundDespawnNumeric = null!;
    private ComboBox slotCombo = null!;
    private ComboBox weaponFamilyCombo = null!;
    private CheckBox twoHandedCheck = null!;
    private NumericUpDown durabilityNumeric = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        visualKeyTextBox = new TextBox();
        kindCombo = new ComboBox();
        rarityNumeric = new NumericUpDown { Maximum = 100 };
        priceNumeric = new NumericUpDown { Maximum = 1_000_000_000 };
        stackableCheck = new CheckBox { AutoSize = true, Text = "Apilable" };
        inventoryStackNumeric = new NumericUpDown { Minimum = 1, Maximum = 9999, Value = 1 };
        bankStackNumeric = new NumericUpDown { Minimum = 1, Maximum = 9999, Value = 1 };
        canDropCheck = new CheckBox { AutoSize = true, Text = "Se puede tirar", Checked = true };
        canTradeCheck = new CheckBox { AutoSize = true, Text = "Se puede comerciar", Checked = true };
        canSellCheck = new CheckBox { AutoSize = true, Text = "Se puede vender", Checked = true };
        canBankCheck = new CheckBox { AutoSize = true, Text = "Se puede guardar", Checked = true };
        dropChanceNumeric = new NumericUpDown { DecimalPlaces = 1, Maximum = 100 };
        groundDespawnNumeric = new NumericUpDown { Maximum = 86_400_000, Increment = 1000 };
        slotCombo = new ComboBox();
        weaponFamilyCombo = new ComboBox();
        twoHandedCheck = new CheckBox { AutoSize = true, Text = "A dos manos" };
        durabilityNumeric = new NumericUpDown { Maximum = 100000 };

        SuspendLayout();
        Name = "ItemEditorForm";
        Text = "Items";
        specificTabPage.Text = "Item";
        FillEnum<ItemKind>(kindCombo);
        FillEnum<EquipmentSlot>(slotCombo);
        FillEnum<WeaponFamily>(weaponFamilyCombo);
        kindCombo.SelectedIndexChanged += (_, _) => UpdateEquipmentEnabled();

        AddSpecificRow(0, "VisualKey", visualKeyTextBox);
        AddSpecificRow(1, "Tipo", kindCombo);
        AddSpecificRow(2, "Rareza", rarityNumeric);
        AddSpecificRow(3, "Precio base", priceNumeric);
        AddSpecificRow(4, "Apilable", stackableCheck);
        AddSpecificRow(5, "Stack inventario", inventoryStackNumeric);
        AddSpecificRow(6, "Stack banco", bankStackNumeric);
        AddSpecificRow(7, "Tirar", canDropCheck);
        AddSpecificRow(8, "Comerciar", canTradeCheck);
        AddSpecificRow(9, "Vender", canSellCheck);
        AddSpecificRow(10, "Banco", canBankCheck);
        AddSpecificRow(11, "Drop al morir %", dropChanceNumeric);
        AddSpecificRow(12, "Despawn suelo ms", groundDespawnNumeric);
        AddSpecificRow(13, "Slot equipo", slotCombo);
        AddSpecificRow(14, "Familia arma", weaponFamilyCombo);
        AddSpecificRow(15, "Dos manos", twoHandedCheck);
        AddSpecificRow(16, "Durabilidad máx", durabilityNumeric);
        ResumeLayout(false);
    }
}
