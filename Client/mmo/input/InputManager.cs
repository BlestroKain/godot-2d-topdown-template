namespace NuevoMMO.GodotClient;

public sealed class InputManager
{
    public InputBindings Bindings { get; } = new();
    public MovementInput Movement { get; }
    public CombatInput Combat { get; }
    public InteractionInput Interaction { get; } = new();
    public HotkeyInput Hotkeys { get; } = new();
    public GameplayInput Gameplay { get; }

    public InputManager()
    {
        Movement = new(Bindings);
        Combat = new(Bindings);
        Gameplay = new(Bindings);
    }

    public void SetUiFocus(bool typing)
        => Gameplay.Context = typing ? GameplayInputContext.Ui : GameplayInputContext.World;
}
