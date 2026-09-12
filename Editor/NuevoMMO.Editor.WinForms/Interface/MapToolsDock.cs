using System.ComponentModel;
using NuevoMMO.Core;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

public enum MapEditorTool : byte
{
    Select,
    PaintTile,
    EraseTile,
    Collision,
    Mob,
    Npc,
    Resource,
    SpawnZone,
    Portal,
    Region,
    Light,
    Event,
    Fill,
    Rectangle
}

[DesignerCategory("Form")]
public sealed partial class MapToolsDock : DockContent
{
    private readonly Panel definitionPanel = new() { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(6, 2, 6, 6) };
    private readonly Label definitionLabel = new() { Dock = DockStyle.Top, Height = 20, Text = "Definición" };
    private readonly ComboBox definitions = new()
    {
        Dock = DockStyle.Bottom,
        DropDownStyle = ComboBoxStyle.DropDownList,
        DisplayMember = nameof(DefinitionEntry.Label)
    };
    private bool selectingTool;

    public MapToolsDock()
    {
        InitializeComponent();
        BuildDefinitionPicker();
        PopulateToolGroups();
        WireToolLists();
        EditorTheme.ApplyWindow(this);
        SelectTool(MapEditorTool.Select);
    }

    public event Action<MapEditorTool>? ToolSelected;
    public event Action<string>? LayerSelected;
    public event Action<DefinitionId?>? DefinitionSelected;

    public DefinitionId? SelectedDefinitionId => (definitions.SelectedItem as DefinitionEntry)?.Id;

    public void SelectTool(MapEditorTool tool)
    {
        var target = ToolLists()
            .Select(pair => (pair.Page, pair.List, Index: FindToolIndex(pair.List, tool)))
            .FirstOrDefault(pair => pair.Index >= 0);
        if (target.List is null) return;

        selectingTool = true;
        try
        {
            foreach (var (_, list) in ToolLists())
                list.SelectedIndex = -1;

            modeTabs.SelectedTab = target.Page;
            target.List.SelectedIndex = target.Index;
            ConfigureDefinitionPanel(tool);
        }
        finally
        {
            selectingTool = false;
        }
    }

    public void SetDefinitions(IEnumerable<GameDefinition> values, DefinitionId? selectedId = null)
    {
        ArgumentNullException.ThrowIfNull(values);
        var entries = values
            .OrderBy(static value => value.Name)
            .ThenBy(static value => value.Key.Value, StringComparer.OrdinalIgnoreCase)
            .Select(static value => new DefinitionEntry(
                value.Id,
                string.IsNullOrWhiteSpace(value.Name) ? value.Key.Value : $"{value.Name} · {value.Key.Value}"))
            .ToArray();

        definitions.BeginUpdate();
        definitions.DataSource = null;
        definitions.DataSource = entries;
        definitions.DisplayMember = nameof(DefinitionEntry.Label);
        if (entries.Length > 0)
        {
            var index = selectedId is null ? 0 : Array.FindIndex(entries, entry => entry.Id == selectedId.Value);
            definitions.SelectedIndex = index >= 0 ? index : 0;
        }
        definitions.EndUpdate();
    }

    public void ClearDefinitions()
    {
        definitions.DataSource = null;
        definitions.Items.Clear();
        DefinitionSelected?.Invoke(null);
    }

    public void SetLayers(IEnumerable<MapLayerDefinition> values, string? selectedKey = null)
    {
        var entries = values
            .OrderBy(static layer => layer.Band)
            .ThenBy(static layer => layer.Order)
            .Select(static layer => new LayerEntry(layer.Key, $"[{layer.Band}] {layer.Key}"))
            .ToArray();

        layers.BeginUpdate();
        layers.DataSource = null;
        layers.DataSource = entries;
        layers.DisplayMember = nameof(LayerEntry.Label);
        if (entries.Length > 0)
        {
            var index = string.IsNullOrWhiteSpace(selectedKey)
                ? 0
                : Array.FindIndex(entries, entry => string.Equals(entry.Key, selectedKey, StringComparison.OrdinalIgnoreCase));
            layers.SelectedIndex = index >= 0 ? index : 0;
        }
        layers.EndUpdate();
    }

    private void PopulateToolGroups()
    {
        BindTools(tileTools,
            new ToolEntry(MapEditorTool.Select, "Seleccionar"),
            new ToolEntry(MapEditorTool.PaintTile, "Pintar tiles"),
            new ToolEntry(MapEditorTool.EraseTile, "Borrar tiles"),
            new ToolEntry(MapEditorTool.Fill, "Rellenar"),
            new ToolEntry(MapEditorTool.Rectangle, "Rectángulo"));

        BindTools(attributeTools,
            new ToolEntry(MapEditorTool.Collision, "Colisión"),
            new ToolEntry(MapEditorTool.Portal, "Portal"),
            new ToolEntry(MapEditorTool.Region, "Región"),
            new ToolEntry(MapEditorTool.SpawnZone, "Zona de spawn"));

        BindTools(lightTools,
            new ToolEntry(MapEditorTool.Light, "Luz"));

        BindTools(eventTools,
            new ToolEntry(MapEditorTool.Event, "Evento"));

        BindTools(entityTools,
            new ToolEntry(MapEditorTool.Mob, "Mob"),
            new ToolEntry(MapEditorTool.Npc, "NPC"),
            new ToolEntry(MapEditorTool.Resource, "Recurso"));
    }

    private static void BindTools(ListBox list, params ToolEntry[] entries)
    {
        list.DataSource = entries;
        list.DisplayMember = nameof(ToolEntry.Label);
    }

    private void WireToolLists()
    {
        foreach (var (_, list) in ToolLists())
            list.SelectedIndexChanged += (_, _) => ActivateTool(list);

        layers.SelectedIndexChanged += (_, _) =>
        {
            if (layers.SelectedItem is LayerEntry entry)
                LayerSelected?.Invoke(entry.Key);
        };
        definitions.SelectedIndexChanged += (_, _) =>
            DefinitionSelected?.Invoke((definitions.SelectedItem as DefinitionEntry)?.Id);
    }

    private void ActivateTool(ListBox source)
    {
        if (selectingTool || source.SelectedItem is not ToolEntry entry) return;

        selectingTool = true;
        try
        {
            foreach (var (_, list) in ToolLists())
            {
                if (!ReferenceEquals(list, source))
                    list.SelectedIndex = -1;
            }
        }
        finally
        {
            selectingTool = false;
        }

        ConfigureDefinitionPanel(entry.Tool);
        ToolSelected?.Invoke(entry.Tool);
    }

    private IEnumerable<(TabPage Page, ListBox List)> ToolLists()
    {
        yield return (tilesPage, tileTools);
        yield return (attributesPage, attributeTools);
        yield return (lightsPage, lightTools);
        yield return (eventsPage, eventTools);
        yield return (entitiesPage, entityTools);
    }

    private static int FindToolIndex(ListBox list, MapEditorTool tool)
    {
        for (var i = 0; i < list.Items.Count; i++)
        {
            if (list.Items[i] is ToolEntry entry && entry.Tool == tool)
                return i;
        }
        return -1;
    }

    private void BuildDefinitionPicker()
    {
        definitionPanel.Controls.Add(definitions);
        definitionPanel.Controls.Add(definitionLabel);
        Controls.Add(definitionPanel);
        definitionPanel.BringToFront();
    }

    private void ConfigureDefinitionPanel(MapEditorTool tool)
    {
        definitionPanel.Visible = tool is MapEditorTool.Mob or MapEditorTool.Npc or MapEditorTool.Resource or MapEditorTool.SpawnZone or MapEditorTool.Portal;
        definitionLabel.Text = tool switch
        {
            MapEditorTool.Mob => "MobDefinition",
            MapEditorTool.Npc => "NpcDefinition",
            MapEditorTool.Resource => "ResourceDefinition",
            MapEditorTool.SpawnZone => "SpawnTableDefinition",
            MapEditorTool.Portal => "Mapa destino",
            _ => "Definición"
        };
    }

    private sealed record LayerEntry(string Key, string Label);
    private sealed record DefinitionEntry(DefinitionId Id, string Label);
    private sealed record ToolEntry(MapEditorTool Tool, string Label);
}
