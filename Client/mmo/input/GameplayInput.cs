using Godot;
using NuevoMMO.Client;
using NuevoMMO.Core;

namespace NuevoMMO.GodotClient;

public enum GameplayInputContext : byte
{
    World,
    Ui
}

/// <summary>
/// Acciones de gameplay reales. Si el contexto es UI (chat/línea de texto), no dispara ataque.
/// </summary>
public sealed class GameplayInput
{
    public GameplayInput(InputBindings bindings) => Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));

    public InputBindings Bindings { get; }
    public GameplayInputContext Context { get; set; } = GameplayInputContext.World;

    public bool Blocked => Context == GameplayInputContext.Ui;

    public bool Attack => !Blocked && Input.IsActionJustPressed(Bindings.Attack);
    public bool Interact => !Blocked && Input.IsActionJustPressed(Bindings.Interact);
    public bool ClearTarget => !Blocked && Input.IsActionJustPressed(Bindings.ClearTarget);
    public bool CycleTarget => !Blocked && Input.IsActionJustPressed(Bindings.CycleTarget);

    public int? HotbarIndex
    {
        get
        {
            if (Blocked) return null;
            for (var index = 0; index < 10; index++)
            {
                if (Input.IsActionJustPressed(Bindings.Hotkey(index))) return index;
            }

            return null;
        }
    }

    public Vector2 MouseWorld(Viewport viewport, Camera2D? camera)
    {
        ArgumentNullException.ThrowIfNull(viewport);
        var screen = viewport.GetMousePosition();
        return camera is null ? screen : camera.GetScreenTransform().AffineInverse() * screen;
    }

    public Vector2 Aim(Viewport viewport, Camera2D? camera, Vector2 origin)
    {
        var stick = Input.GetVector(Bindings.AimLeft, Bindings.AimRight, Bindings.AimUp, Bindings.AimDown);
        if (stick.LengthSquared() > 0.04f) return stick.Normalized();
        var mouse = MouseWorld(viewport, camera);
        var delta = mouse - origin;
        return delta.LengthSquared() <= 1f ? Vector2.Right : delta.Normalized();
    }
}
