using Godot;
using NuevoMMO.Client;
using NuevoMMO.Core;

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

    public MobState? CycleTarget(ClientWorldState world, EntityId? current, float maxRange = CombatTargeting.DefaultRange)
    {
        ArgumentNullException.ThrowIfNull(world);
        return CombatTargeting.CycleMob(world.Entities.All.Values, world.Local.Position, current, maxRange);
    }
}
