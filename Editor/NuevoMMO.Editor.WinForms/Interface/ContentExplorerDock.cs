using NuevoMMO.Core;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

public sealed class ContentExplorerDock : DockContent
{
    private readonly EditorApplication application;
    private readonly TreeView tree = new() { Dock = DockStyle.Fill, HideSelection = false };

    public ContentExplorerDock(EditorApplication application)
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
        Text = "Contenido";
        TabText = Text;
        HideOnClose = true;
        Controls.Add(tree);
        tree.NodeMouseDoubleClick += (_, e) =>
        {
            if (e.Node.Tag is GameDefinition definition)
                DefinitionActivated?.Invoke(definition);
        };
        RefreshTree();
    }

    public event Action<GameDefinition>? DefinitionActivated;

    public void RefreshTree()
    {
        var selectedId = tree.SelectedNode?.Tag is GameDefinition selected ? selected.Id : DefinitionId.Empty;
        tree.BeginUpdate();
        tree.Nodes.Clear();

        var definitions = application.Content.Snapshot().All().ToArray();
        AddCategory<MapDefinition>("Mapas", definitions);
        AddCategory<TilesetDefinition>("Tilesets", definitions);
        AddCategory<ItemDefinition>("Items", definitions);
        AddCategory<MobDefinition>("Mobs", definitions);
        AddCategory<NpcDefinition>("NPCs", definitions);
        AddCategory<ResourceDefinition>("Recursos", definitions);
        AddCategory<TechniqueDefinition>("Técnicas / Spells", definitions);
        AddCategory<EffectDefinition>("Efectos", definitions);
        AddCategory<TraditionDefinition>("Tradiciones / Clases", definitions);
        AddCategory<ProfessionDefinition>("Profesiones", definitions);
        AddCategory<RecipeDefinition>("Recetas", definitions);
        AddCategory<LootTableDefinition>("Loot Tables", definitions);
        AddCategory<SpawnTableDefinition>("Spawn Tables", definitions);
        AddCategory<QuestDefinition>("Quests", definitions);
        AddCategory<EventDefinition>("Eventos", definitions);
        AddCategory<DungeonDefinition>("Dungeons", definitions);

        tree.ExpandAll();
        if (!selectedId.IsEmpty)
            SelectById(tree.Nodes, selectedId);
        tree.EndUpdate();
    }

    private void AddCategory<T>(string title, IEnumerable<GameDefinition> definitions)
        where T : GameDefinition
    {
        var root = tree.Nodes.Add(title);
        foreach (var definition in definitions.OfType<T>().OrderBy(static value => value.Name))
            root.Nodes.Add(new TreeNode(definition.Name) { Tag = definition });
    }

    private static bool SelectById(TreeNodeCollection nodes, DefinitionId id)
    {
        foreach (TreeNode node in nodes)
        {
            if (node.Tag is GameDefinition definition && definition.Id == id)
            {
                node.TreeView.SelectedNode = node;
                return true;
            }
            if (SelectById(node.Nodes, id)) return true;
        }
        return false;
    }
}
