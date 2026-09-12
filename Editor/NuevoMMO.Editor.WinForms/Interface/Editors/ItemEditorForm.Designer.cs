#nullable enable
using System.ComponentModel;
using NuevoMMO.Core;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class ItemEditorForm
{
    private IContainer? components;
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
    private NumericUpDown restoreHealthNumeric = null!;
    private NumericUpDown restoreManaNumeric = null!;
    private NumericUpDown strengthNumeric = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
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
        restoreHealthNumeric = new NumericUpDown { Maximum = 100000 };
        restoreManaNumeric = new NumericUpDown { Maximum = 100000 };
        strengthNumeric = new NumericUpDown { Minimum = -1000, Maximum = 1000 };

        SuspendLayout();
        Name = "ItemEditorForm";
        Text = "Items";
        specificTabPage.Text = "Item";
        FillEnum<ItemKind>(kindCombo);
        FillEnum<EquipmentSlot>(slotCombo);
        FillEnum<WeaponFamily>(weaponFamilyCombo);
        kindCombo.SelectedIndexChanged += (_, _) => UpdateEquipmentEnabled();

        var economy = AddGroup("Economía");
        AddGroupRow(economy, "Tipo", kindCombo);
        AddGroupRow(economy, "Rareza", rarityNumeric);
        AddGroupRow(economy, "Precio base", priceNumeric);
        AddGroupRow(economy, "Apilable", stackableCheck);
        AddGroupRow(economy, "Stack inventario", inventoryStackNumeric);
        AddGroupRow(economy, "Stack banco", bankStackNumeric);

        var permissions = AddGroup("Permisos");
        AddGroupRow(permissions, "Tirar", canDropCheck);
        AddGroupRow(permissions, "Comerciar", canTradeCheck);
        AddGroupRow(permissions, "Vender", canSellCheck);
        AddGroupRow(permissions, "Banco", canBankCheck);
        AddGroupRow(permissions, "Drop al morir %", dropChanceNumeric);
        AddGroupRow(permissions, "Despawn suelo ms", groundDespawnNumeric);

        var equipment = AddGroup("Equipo");
        AddGroupRow(equipment, "Slot", slotCombo);
        AddGroupRow(equipment, "Familia arma", weaponFamilyCombo);
        AddGroupRow(equipment, "Dos manos", twoHandedCheck);
        AddGroupRow(equipment, "Durabilidad máx", durabilityNumeric);
        AddGroupRow(equipment, "STR equipo", strengthNumeric);

        var consumable = AddGroup("Consumible");
        AddGroupRow(consumable, "Restaura HP", restoreHealthNumeric);
        AddGroupRow(consumable, "Restaura PM", restoreManaNumeric);
        ResumeLayout(false);
    }
}
