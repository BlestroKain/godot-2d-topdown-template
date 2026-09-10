namespace NuevoMMO.GodotClient;

public sealed class InputManager
{
    public InputBindings Bindings { get; } = new();
    public MovementInput Movement { get; }
    public CombatInput Combat { get; } = new();
    public InteractionInput Interaction { get; } = new();
    public HotkeyInput Hotkeys { get; } = new();
    public InputManager() => Movement = new(Bindings);
}
