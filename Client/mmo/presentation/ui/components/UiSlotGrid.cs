using Godot;

namespace NuevoMMO.GodotClient.UI;

/// <summary>Editable grid that instantiates the shared UiSlot scene. Configure Columns and SlotCount in the inspector.</summary>
public partial class UiSlotGrid : GridContainer
{
    [Export(PropertyHint.Range, "1,16,1")] public int GridColumns { get; set; } = 5;
    [Export(PropertyHint.Range, "0,200,1")] public int SlotCount { get; set; } = 20;
    [Export] public PackedScene? SlotScene { get; set; }

    public override void _Ready()
    {
        Columns = GridColumns;
        SlotScene ??= GD.Load<PackedScene>("res://mmo/presentation/ui/components/ui_slot.tscn");
        Rebuild();
    }

    public void Rebuild()
    {
        foreach (var child in GetChildren()) child.QueueFree();
        if (SlotScene is null) return;
        for (var i = 0; i < SlotCount; i++)
        {
            var slot = SlotScene.Instantiate<UiSlot>();
            slot.SlotIndex = i;
            AddChild(slot);
        }
    }

    public UiSlot? GetSlot(int index) => index >= 0 && index < GetChildCount() ? GetChild<UiSlot>(index) : null;
}
