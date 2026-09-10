using NuevoMMO.Core;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

public sealed class DefinitionEditorCatalog
{
    private readonly EditorApplication application;
    private readonly Dictionary<Type, DefinitionEditorForm> editors = [];

    public DefinitionEditorCatalog(EditorApplication application)
        => this.application = application ?? throw new ArgumentNullException(nameof(application));

    public event Action<GameDefinition?>? ContentChanged;

    public DefinitionEditorForm Open(Type definitionType, DockPanel dockPanel)
    {
        ArgumentNullException.ThrowIfNull(definitionType);
        ArgumentNullException.ThrowIfNull(dockPanel);

        if (!editors.TryGetValue(definitionType, out var editor) || editor.IsDisposed)
        {
            editor = Create(definitionType);
            editors[definitionType] = editor;
        }

        editor.RefreshDefinitions();
        editor.Show(dockPanel, DockState.Document);
        editor.Activate();
        return editor;
    }

    public DefinitionEditorForm Open(GameDefinition definition, DockPanel dockPanel)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var editor = Open(definition.GetType(), dockPanel);
        editor.SelectDefinition(definition.Id);
        return editor;
    }

    public void RefreshOpenEditors()
    {
        foreach (var editor in editors.Values.Where(static value => !value.IsDisposed))
            editor.RefreshDefinitions();
    }

    private DefinitionEditorForm Create(Type definitionType)
    {
        DefinitionEditorForm editor = definitionType switch
        {
            var type when type == typeof(ItemDefinition) => new ItemEditorForm(application),
            var type when type == typeof(MobDefinition) => new MobEditorForm(application),
            var type when type == typeof(NpcDefinition) => new NpcEditorForm(application),
            var type when type == typeof(ResourceDefinition) => new ResourceEditorForm(application),
            var type when type == typeof(TechniqueDefinition) => new TechniqueEditorForm(application),
            var type when type == typeof(EffectDefinition) => new EffectEditorForm(application),
            var type when type == typeof(TraditionDefinition) => new TraditionEditorForm(application),
            var type when type == typeof(ProfessionDefinition) => new ProfessionEditorForm(application),
            var type when type == typeof(RecipeDefinition) => new RecipeEditorForm(application),
            var type when type == typeof(LootTableDefinition) => new LootTableEditorForm(application),
            var type when type == typeof(SpawnTableDefinition) => new SpawnTableEditorForm(application),
            var type when type == typeof(QuestDefinition) => new QuestEditorForm(application),
            var type when type == typeof(EventDefinition) => new EventEditorForm(application),
            var type when type == typeof(DungeonDefinition) => new DungeonEditorForm(application),
            var type when type == typeof(ItemPropertyDefinition) => new ItemPropertyEditorForm(application),
            var type when type == typeof(TilesetDefinition) => new TilesetEditorForm(application),
            _ => throw new NotSupportedException($"No existe un editor WinForms para {definitionType.Name}.")
        };

        editor.ContentChanged += definition => ContentChanged?.Invoke(definition);

        return editor;
    }
}
