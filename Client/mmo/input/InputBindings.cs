namespace NuevoMMO.GodotClient;

public sealed class InputBindings
{
    public string Left { get; init; } = "mmo_left";
    public string Right { get; init; } = "mmo_right";
    public string Up { get; init; } = "mmo_up";
    public string Down { get; init; } = "mmo_down";
    public string Attack { get; init; } = CombatInput.AttackAction;
    public string Hotkey1 { get; init; } = "mmo_hotkey_1";
    public string Hotkey2 { get; init; } = "mmo_hotkey_2";
    public string Hotkey3 { get; init; } = "mmo_hotkey_3";
    public string Hotkey4 { get; init; } = "mmo_hotkey_4";
    public string Hotkey5 { get; init; } = "mmo_hotkey_5";
    public string Hotkey6 { get; init; } = "mmo_hotkey_6";
}
