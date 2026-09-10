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
        Dock = DockStyle.Fill
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

    public void SetLayers(IEnumerable<string> names)
    {
        layers.BeginUpdate();
        layers.Items.Clear();
        foreach (var name in names) layers.Items.Add(name);
        if (layers.Items.Count > 0 && layers.SelectedIndex < 0) layers.SelectedIndex = 0;
        layers.EndUpdate();
    }
}
