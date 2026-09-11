using Godot;
using NuevoMMO.Client;
using NuevoMMO.Core;
using NuevoMMO.Network;

namespace NuevoMMO.GodotClient;

public sealed class CombatInput
{
    public const string AttackAction = "mmo_attack";

    public CombatInput(InputBindings bindings) => Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));

    public InputBindings Bindings { get; }

    public bool JustPressed => Input.IsActionJustPressed(Bindings.Attack);

    public MobState? FindTarget(ClientWorldState world, Func<MobState, bool>? predicate = null, float maxRange = CombatTargeting.DefaultRange)
    {
        ArgumentNullException.ThrowIfNull(world);
        return CombatTargeting.NearestMob(world.Entities.All.Values, world.Local.Position, predicate, maxRange);
    }

    public DevelopmentAttackKind ReadAttackKind()
    {
        if (Input.IsActionJustPressed(Bindings.Hotkey2)) return DevelopmentAttackKind.Earth;
        if (Input.IsActionJustPressed(Bindings.Hotkey3)) return DevelopmentAttackKind.Fire;
        if (Input.IsActionJustPressed(Bindings.Hotkey4)) return DevelopmentAttackKind.Air;
        if (Input.IsActionJustPressed(Bindings.Hotkey5)) return DevelopmentAttackKind.Water;
        if (Input.IsActionJustPressed(Bindings.Hotkey6)) return DevelopmentAttackKind.NeutralStrength;
        return DevelopmentAttackKind.Basic;
    }
}
