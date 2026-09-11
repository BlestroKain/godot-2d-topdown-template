using Godot;

namespace NuevoMMO.GodotClient.UI;

/// <summary>Reusable item/skill/reward slot. Visuals live in ui_slot.tscn; data is injected at runtime.</summary>
public partial class UiSlot : Button
{
    [Signal] public delegate void SlotActivatedEventHandler(int index);

    [Export] public int SlotIndex { get; set; }
    [Export] public Texture2D? SlotIcon { get; set; }
    [Export] public int Quantity { get; set; }

    private TextureRect icon = null!;
    private Label quantity = null!;
    private ColorRect selection = null!;

    public override void _Ready()
    {
        FocusMode = FocusModeEnum.None;
        icon = GetNode<TextureRect>("Icon");
        quantity = GetNode<Label>("Quantity");
        selection = GetNode<ColorRect>("Selection");
        Pressed += () => EmitSignal(SignalName.SlotActivated, SlotIndex);
        Refresh();
    }

    public void SetData(Texture2D? texture, int amount = 0, string tooltip = "")
    {
        SlotIcon = texture;
        Quantity = amount;
        TooltipText = tooltip;
        Refresh();
    }

    public void SetSelected(bool selected) => selection.Visible = selected;

    public void Clear()
    {
        SlotIcon = null;
        Quantity = 0;
        TooltipText = string.Empty;
        Refresh();
    }

    private void Refresh()
    {
        if (!IsNodeReady()) return;
        icon.Texture = SlotIcon;
        icon.Visible = SlotIcon is not null;
        quantity.Text = Quantity > 1 ? Quantity.ToString() : string.Empty;
    }
}
