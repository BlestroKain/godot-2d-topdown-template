using Godot;

namespace NuevoMMO.GodotClient.UI;

/// <summary>
/// Turns direct child Buttons into an exclusive tab set while keeping the scene fully editable.
/// Data/filter controllers may listen to TabChanged instead of owning presentation state.
/// </summary>
public partial class UiTabBar : HBoxContainer
{
    [Signal] public delegate void TabChangedEventHandler(int index, string tabName);

    private readonly ButtonGroup group = new();

    public override void _Ready()
    {
        Button? first = null;
        var hasSelection = false;
        var tabIndex = 0;

        foreach (var child in GetChildren())
        {
            if (child is not Button button)
                continue;

            first ??= button;
            button.ToggleMode = true;
            button.ButtonGroup = group;
            button.SetMeta("ui_tab_index", tabIndex);
            hasSelection |= button.ButtonPressed;
            button.Pressed += () => EmitTab(button);
            tabIndex++;
        }

        if (!hasSelection && first is not null)
            first.ButtonPressed = true;
    }

    public int SelectedIndex
    {
        get
        {
            var pressed = group.GetPressedButton();
            return pressed is null || !pressed.HasMeta("ui_tab_index")
                ? -1
                : pressed.GetMeta("ui_tab_index").AsInt32();
        }
    }

    private void EmitTab(Button button)
    {
        var index = button.GetMeta("ui_tab_index").AsInt32();
        EmitSignal(SignalName.TabChanged, index, button.Name.ToString());
    }
}
