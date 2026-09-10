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
    Event
}

public sealed class MapToolsDock : DockContent
{
    private readonly ListBox tools = new()
    {
        Dock = DockStyle.Top,
        Height = 250
    };

    private readonly ListBox layers = new()
    {
        Dock = DockStyle.Fill,
        DisplayMember = nameof(LayerEntry.Label)
    };

    public MapToolsDock()
    {
        Text = "Mapa / Capas";
        TabText = Text;
        HideOnClose = true;

        tools.Items.AddRange(Enum.GetNames<MapEditorTool>());
        tools.SelectedIndex = 0;
        tools.SelectedIndexChanged += (_, _) =>
        {
            if (tools.SelectedIndex >= 0)
                ToolSelected?.Invoke((MapEditorTool)tools.SelectedIndex);
        };
        layers.SelectedIndexChanged += (_, _) =>
        {
            if (layers.SelectedItem is LayerEntry entry)
                LayerSelected?.Invoke(entry.Key);
        };

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 260
        };
        split.Panel1.Controls.Add(tools);
        split.Panel2.Controls.Add(layers);
        Controls.Add(split);
    }

    public event Action<MapEditorTool>? ToolSelected;
    public event Action<string>? LayerSelected;

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

    private sealed record LayerEntry(string Key, string Label);
}
