using Godot;

namespace NuevoMMO.GodotClient;

public sealed class MovementInput(InputBindings bindings)
{
    public Vector2 Read() => Input.GetVector(bindings.Left, bindings.Right, bindings.Up, bindings.Down);
}
