using NuevoMMO.Core;

namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class ItemEditorForm : DefinitionEditorForm
{
    public ItemEditorForm()
    {
        InitializeComponent();
    }

    public ItemEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Items)
    {
        InitializeComponent();
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not ItemDefinition item) return;
        visualKeyTextBox.Text = item.VisualKey.Value;
        SelectEnum(kindCombo, item.Kind);
        SetNumeric(rarityNumeric, item.Rarity);
        SetNumeric(priceNumeric, item.BasePrice);
        stackableCheck.Checked = item.Stacking.Stackable;
        SetNumeric(inventoryStackNumeric, item.Stacking.MaxInventoryStack);
        SetNumeric(bankStackNumeric, item.Stacking.MaxBankStack);
        canDropCheck.Checked = item.Permissions.CanDrop;
        canTradeCheck.Checked = item.Permissions.CanTrade;
        canSellCheck.Checked = item.Permissions.CanSell;
        canBankCheck.Checked = item.Permissions.CanBank;
        SetNumeric(dropChanceNumeric, (decimal)item.DropChanceOnDeathPercent);
        SetNumeric(groundDespawnNumeric, item.GroundDespawnMilliseconds);
        SelectEnum(slotCombo, item.Equipment?.Slot ?? EquipmentSlot.None);
        SelectEnum(weaponFamilyCombo, item.Equipment?.WeaponFamily ?? WeaponFamily.None);
        twoHandedCheck.Checked = item.Equipment?.TwoHanded == true;
        SetNumeric(durabilityNumeric, item.Equipment?.MaxDurability ?? 0);
        UpdateEquipmentEnabled();
    }

    private void UpdateEquipmentEnabled()
    {
        var equipment = ReadEnum(kindCombo, ItemKind.Generic) == ItemKind.Equipment;
        slotCombo.Enabled = equipment;
        weaponFamilyCombo.Enabled = equipment;
        twoHandedCheck.Enabled = equipment;
        durabilityNumeric.Enabled = equipment;
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        var item = current as ItemDefinition ?? throw new InvalidOperationException("La selección no es un Item.");
        var kind = ReadEnum(kindCombo, item.Kind);
        var stackable = stackableCheck.Checked;
        var inventory = stackable ? (int)inventoryStackNumeric.Value : 1;
        var bank = stackable ? (int)bankStackNumeric.Value : 1;
        ItemEquipmentDefinition? equipment = null;
        if (kind == ItemKind.Equipment)
        {
            var slot = ReadEnum(slotCombo, EquipmentSlot.None);
            if (slot == EquipmentSlot.None)
                throw new InvalidOperationException("Un objeto de equipo requiere slot.");
            var durability = durabilityNumeric.Value > 0 ? (int?)durabilityNumeric.Value : null;
            equipment = new ItemEquipmentDefinition(
                slot,
                ReadEnum(weaponFamilyCombo, WeaponFamily.None),
                twoHandedCheck.Checked,
                durability,
                item.Equipment?.FlatStats,
                item.Equipment?.PercentStats,
                item.Equipment?.CombatModifiers);
        }

        return new ItemDefinition(
            id, key, name, description, enabled, version, tags,
            ReadContentKey(visualKeyTextBox),
            item.PropertyIds,
            kind,
            (int)rarityNumeric.Value,
            (long)priceNumeric.Value,
            new ItemPermissionsDefinition(canDropCheck.Checked, canTradeCheck.Checked, canSellCheck.Checked, canBankCheck.Checked),
            new ItemStackDefinition(stackable, inventory, bank),
            equipment,
            item.Use,
            item.PropertyRanges,
            (long)groundDespawnNumeric.Value,
            item.Metadata,
            item.Combat,
            item.Consumable,
            item.Requirements,
            (float)dropChanceNumeric.Value,
            item.ToolKey,
            item.PassiveEffectIds,
            item.References,
            item.VisualOverrides);
    }
}
