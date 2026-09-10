using NuevoMMO.Core;

namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class LootTableEditorForm : DefinitionEditorForm
{
    public LootTableEditorForm() => InitializeComponent();

    public LootTableEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.LootTables)
    {
        InitializeComponent();
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not LootTableDefinition loot) return;
        entriesTextBox.Text = string.Join(Environment.NewLine, loot.Entries.Select(entry =>
        {
            var key = Editor.Definitions.TryGet<ItemDefinition>(entry.ItemId, out var item) && item is not null
                ? item.Key.Value
                : entry.ItemId.ToString();
            return $"{key}, {entry.ChancePercent:0.##}, {entry.MinimumQuantity}, {entry.MaximumQuantity}";
        }));
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        var loot = current as LootTableDefinition ?? throw new InvalidOperationException("La selección no es una Loot Table.");
        var entries = new List<LootEntryDefinition>();
        foreach (var line in entriesTextBox.Text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 1) continue;
            var itemId = ResolveDefinition<ItemDefinition>(parts[0], "Loot");
            var chance = parts.Length > 1 ? float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture) : 100f;
            var min = parts.Length > 2 ? int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture) : 1;
            var max = parts.Length > 3 ? int.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture) : min;
            entries.Add(new LootEntryDefinition(itemId, chance, min, max));
        }

        return new LootTableDefinition(id, key, name, description, enabled, version, tags, [], entries.ToArray(), loot.Parameters, loot.Metadata);
    }
}
