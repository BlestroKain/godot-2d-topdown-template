using Godot;

namespace NuevoMMO.GodotClient;

/// <summary>
/// Registers window-only shortcuts that do not belong to gameplay/combat input.
/// Keeping them here avoids coupling the editable UI layer to MmoGame.
/// </summary>
public partial class UiHotkeyRouter : Node
{
    private GameHud hud = null!;

    public override void _Ready()
    {
        hud = GetParent<GameHud>();
        RegisterAction("mmo_quests", Key.L);
        RegisterAction("mmo_techniques", Key.K);
    }

    public override void _Process(double delta)
    {
        if (!hud.Visible || GetViewport().GuiGetFocusOwner() is LineEdit)
            return;

        if (Input.IsActionJustPressed("mmo_quests"))
            hud.ToggleQuests();
        if (Input.IsActionJustPressed("mmo_techniques"))
            hud.ToggleTechniques();
    }

    private static void RegisterAction(string action, params Key[] keys)
    {
        if (InputMap.HasAction(action))
            return;

        InputMap.AddAction(action, 0.2f);
        foreach (var key in keys)
            InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }
}
