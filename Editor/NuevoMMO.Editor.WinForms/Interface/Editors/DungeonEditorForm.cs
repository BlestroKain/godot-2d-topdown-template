using NuevoMMO.Core;

namespace NuevoMMO.Editor;

[System.ComponentModel.DesignerCategory("Form")]
public partial class DungeonEditorForm : DefinitionEditorForm
{
    public DungeonEditorForm() => InitializeComponent();

    public DungeonEditorForm(EditorApplication application)
        : base(application, DefinitionEditorDescriptors.Dungeons)
    {
        InitializeComponent();
        FinishSetup();
    }

    protected override void BindSpecific(GameDefinition definition)
    {
        if (definition is not DungeonDefinition dungeon) return;
        BindDefinitionCombo<MapDefinition>(mapCombo, dungeon.MapId, optional: false);
        SelectEnum(instanceModeCombo, dungeon.InstanceMode);
        SetNumeric(resetNumeric, dungeon.ResetMilliseconds);
        SetNumeric(minPartyNumeric, dungeon.MinimumPartySize);
        SetNumeric(maxPartyNumeric, dungeon.MaximumPartySize);
    }

    protected override GameDefinition? TryBuildFromFields(
        DefinitionId id, ContentKey key, string name, string description,
        bool enabled, int version, string[] tags, GameDefinition current)
    {
        var dungeon = current as DungeonDefinition ?? throw new InvalidOperationException("La selección no es un Dungeon.");
        return new DungeonDefinition(
            id, key, name, description, enabled, version, tags,
            RequireDefinitionId(mapCombo, "Mapa de entrada"),
            dungeon.MapIds,
            dungeon.Stages,
            ReadEnum(instanceModeCombo, dungeon.InstanceMode),
            dungeon.EntryRequirements,
            dungeon.EntryEventId,
            dungeon.CompletionEventId,
            (int)resetNumeric.Value,
            (int)minPartyNumeric.Value,
            (int)maxPartyNumeric.Value,
            dungeon.Parameters,
            dungeon.Metadata);
    }
}
