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
        ConfigureVisual(AssetKind.Item);
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not ItemDefinition item) return;
        BindVisualPicker(item.VisualKey);
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
        SetNumeric(restoreHealthNumeric, (decimal)(item.Consumable?.FlatVitals.GetValueOrDefault(VitalId.Health) ?? 0));
        SetNumeric(restoreManaNumeric, (decimal)(item.Consumable?.FlatVitals.GetValueOrDefault(VitalId.Mana) ?? 0));
        SetNumeric(strengthNumeric, (decimal)(item.Equipment?.FlatStats.GetValueOrDefault(StatId.Strength) ?? 0));
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
            var stats = new Dictionary<StatId, float>(item.Equipment?.FlatStats ?? []);
            if (strengthNumeric.Value != 0) stats[StatId.Strength] = (float)strengthNumeric.Value;
            else stats.Remove(StatId.Strength);
            equipment = new ItemEquipmentDefinition(
                slot,
                ReadEnum(weaponFamilyCombo, WeaponFamily.None),
                twoHandedCheck.Checked,
                durability,
                stats.Count == 0 ? null : stats,
                item.Equipment?.PercentStats,
                item.Equipment?.CombatModifiers);
        }

        ItemConsumableDefinition? consumable = item.Consumable;
        if (kind == ItemKind.Consumable || restoreHealthNumeric.Value > 0 || restoreManaNumeric.Value > 0)
        {
            var vitals = new Dictionary<VitalId, float>(item.Consumable?.FlatVitals ?? []);
            if (restoreHealthNumeric.Value > 0) vitals[VitalId.Health] = (float)restoreHealthNumeric.Value;
            else vitals.Remove(VitalId.Health);
            if (restoreManaNumeric.Value > 0) vitals[VitalId.Mana] = (float)restoreManaNumeric.Value;
            else vitals.Remove(VitalId.Mana);
            consumable = new ItemConsumableDefinition(vitals, item.Consumable?.PercentVitals, item.Consumable?.VitalRegeneration,
                item.Consumable?.EffectIds, item.Consumable?.Parameters);
        }

        return new ItemDefinition(
            id, key, name, description, enabled, version, tags,
            ReadVisualPicker(),
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
            consumable,
            item.Requirements,
            (float)dropChanceNumeric.Value,
            item.ToolKey,
            item.PassiveEffectIds,
            item.References,
            item.VisualOverrides);
    }
}
