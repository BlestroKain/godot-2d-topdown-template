using NuevoMMO.Core;

namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class SpawnTableEditorForm : DefinitionEditorForm
{
    public SpawnTableEditorForm() => InitializeComponent();

    public SpawnTableEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.SpawnTables)
    {
        InitializeComponent();
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not SpawnTableDefinition spawn) return;
        SelectEnum(selectionModeCombo, spawn.SelectionMode);
        SetNumeric(maxAliveNumeric, spawn.MaximumTotalAlive);
        entriesTextBox.Text = string.Join(Environment.NewLine, spawn.Entries.Select(entry =>
        {
            var key = entry.Kind switch
            {
                SpawnEntityKind.Mob => NameOf<MobDefinition>(entry.DefinitionId),
                SpawnEntityKind.Npc => NameOf<NpcDefinition>(entry.DefinitionId),
                SpawnEntityKind.Resource => NameOf<ResourceDefinition>(entry.DefinitionId),
                _ => entry.DefinitionId.ToString()
            };
            return $"{entry.Kind}, {key}, {entry.Weight:0.##}, {entry.MaximumAlive}";
        }));
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        var spawn = current as SpawnTableDefinition ?? throw new InvalidOperationException("La selección no es una Spawn Table.");
        var entries = new List<SpawnEntryDefinition>();
        foreach (var line in entriesTextBox.Text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length < 2) continue;
            var kind = Enum.Parse<SpawnEntityKind>(parts[0], ignoreCase: true);
            var definitionId = kind switch
            {
                SpawnEntityKind.Mob => ResolveDefinition<MobDefinition>(parts[1], "Spawn"),
                SpawnEntityKind.Npc => ResolveDefinition<NpcDefinition>(parts[1], "Spawn"),
                SpawnEntityKind.Resource => ResolveDefinition<ResourceDefinition>(parts[1], "Spawn"),
                _ => throw new InvalidOperationException($"Kind de spawn no soportado: {kind}.")
            };
            var weight = parts.Length > 2 ? float.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture) : 1f;
            var maxAlive = parts.Length > 3 ? int.Parse(parts[3], System.Globalization.CultureInfo.InvariantCulture) : 1;
            entries.Add(new SpawnEntryDefinition(kind, definitionId, weight, maximumAlive: maxAlive));
        }

        return new SpawnTableDefinition(
            id, key, name, description, enabled, version, tags,
            [],
            entries.ToArray(),
            ReadEnum(selectionModeCombo, spawn.SelectionMode),
            (int)maxAliveNumeric.Value,
            spawn.Parameters);
    }

    private string NameOf<T>(DefinitionId id) where T : GameDefinition
        => Editor.Definitions.TryGet<T>(id, out var definition) && definition is not null
            ? definition.Key.Value
            : id.ToString();
}
