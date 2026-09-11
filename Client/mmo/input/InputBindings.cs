namespace NuevoMMO.GodotClient;

public sealed class InputBindings
{
    public string Left { get; init; } = "mmo_left";
    public string Right { get; init; } = "mmo_right";
    public string Up { get; init; } = "mmo_up";
    public string Down { get; init; } = "mmo_down";
    public string Attack { get; init; } = CombatInput.AttackAction;
    public string Interact { get; init; } = InteractionInput.InteractAction;
    public string ClearTarget { get; init; } = "mmo_clear_target";
    public string CycleTarget { get; init; } = "mmo_cycle_target";
    public string AimLeft { get; init; } = "mmo_aim_left";
    public string AimRight { get; init; } = "mmo_aim_right";
    public string AimUp { get; init; } = "mmo_aim_up";
    public string AimDown { get; init; } = "mmo_aim_down";
    public string Hotkey1 { get; init; } = "mmo_hotkey_1";
    public string Hotkey2 { get; init; } = "mmo_hotkey_2";
    public string Hotkey3 { get; init; } = "mmo_hotkey_3";
    public string Hotkey4 { get; init; } = "mmo_hotkey_4";
    public string Hotkey5 { get; init; } = "mmo_hotkey_5";
    public string Hotkey6 { get; init; } = "mmo_hotkey_6";
    public string Hotkey7 { get; init; } = "mmo_hotkey_7";
    public string Hotkey8 { get; init; } = "mmo_hotkey_8";
    public string Hotkey9 { get; init; } = "mmo_hotkey_9";
    public string Hotkey10 { get; init; } = "mmo_hotkey_10";

    public string Hotkey(int index) => index switch
    {
        0 => Hotkey1,
        1 => Hotkey2,
        2 => Hotkey3,
        3 => Hotkey4,
        4 => Hotkey5,
        5 => Hotkey6,
        6 => Hotkey7,
        7 => Hotkey8,
        8 => Hotkey9,
        9 => Hotkey10,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
}
