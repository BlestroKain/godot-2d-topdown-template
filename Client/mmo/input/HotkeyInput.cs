using Godot;

namespace NuevoMMO.GodotClient;

public sealed class HotkeyInput
{
    public int? ReadHotbarIndex(InputBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        for (var index = 0; index < 10; index++)
        {
            if (Input.IsActionJustPressed(bindings.Hotkey(index))) return index;
        }

        return null;
    }
}
