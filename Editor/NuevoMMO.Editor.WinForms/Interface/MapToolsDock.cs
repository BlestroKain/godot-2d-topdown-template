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
    private readonly Panel definitionPanel = new() { Dock = DockStyle.Bottom, Height = 58, Padding = new Padding(6, 2, 6, 6) };
    private readonly Label definitionLabel = new() { Dock = DockStyle.Top, Height = 22, Text = "Definición" };
    private readonly ComboBox definitions = new()
    {
        Dock = DockStyle.Bottom,
        DropDownStyle = ComboBoxStyle.DropDownList,
        DisplayMember = nameof(DefinitionEntry.Label)
    };

    public MapToolsDock()
    {
        InitializeComponent();
        EditorTheme.ApplyWindow(this);
        BuildDefinitionPicker();

        tools.Items.AddRange(Enum.GetNames<MapEditorTool>());
        tools.SelectedIndex = 0;
        tools.SelectedIndexChanged += (_, _) =>
        {
            if (tools.SelectedIndex < 0) return;
            var tool = (MapEditorTool)tools.SelectedIndex;
            ConfigureDefinitionPanel(tool);
            ToolSelected?.Invoke(tool);
        };
        layers.SelectedIndexChanged += (_, _) =>
        {
            if (layers.SelectedItem is LayerEntry entry)
                LayerSelected?.Invoke(entry.Key);
        };
        definitions.SelectedIndexChanged += (_, _) =>
            DefinitionSelected?.Invoke((definitions.SelectedItem as DefinitionEntry)?.Id);

        ConfigureDefinitionPanel(MapEditorTool.Select);
    }

    public event Action<MapEditorTool>? ToolSelected;
    public event Action<string>? LayerSelected;
    public event Action<DefinitionId?>? DefinitionSelected;

    public DefinitionId? SelectedDefinitionId => (definitions.SelectedItem as DefinitionEntry)?.Id;

    public void SelectTool(MapEditorTool tool)
    {
        var index = (int)tool;
        if (index < 0 || index >= tools.Items.Count) return;
        tools.SelectedIndex = index;
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

    private void BuildDefinitionPicker()
    {
        definitionPanel.Controls.Add(definitions);
        definitionPanel.Controls.Add(definitionLabel);
        split.Panel1.Controls.Add(definitionPanel);
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
}
