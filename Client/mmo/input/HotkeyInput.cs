using Godot;
using NuevoMMO.Network;

namespace NuevoMMO.GodotClient;

public sealed class HotkeyInput
{
    public DevelopmentAttackKind? ReadCombatHotkey(InputBindings bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        if (Input.IsActionJustPressed(bindings.Hotkey1)) return DevelopmentAttackKind.Basic;
        if (Input.IsActionJustPressed(bindings.Hotkey2)) return DevelopmentAttackKind.Earth;
        if (Input.IsActionJustPressed(bindings.Hotkey3)) return DevelopmentAttackKind.Fire;
        if (Input.IsActionJustPressed(bindings.Hotkey4)) return DevelopmentAttackKind.Air;
        if (Input.IsActionJustPressed(bindings.Hotkey5)) return DevelopmentAttackKind.Water;
        if (Input.IsActionJustPressed(bindings.Hotkey6)) return DevelopmentAttackKind.NeutralStrength;
        return null;
    }
}
