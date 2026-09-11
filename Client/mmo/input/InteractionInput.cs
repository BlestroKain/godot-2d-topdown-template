using Godot;
using NuevoMMO.Client;
using NuevoMMO.Core;

namespace NuevoMMO.GodotClient;

public sealed class InteractionInput
{
    public const string InteractAction = "mmo_interact";

    public bool JustPressed => Input.IsActionJustPressed(InteractAction);

    public EntityState? NearestInteractable(ClientWorldState world, float maxRange = 64f)
    {
        ArgumentNullException.ThrowIfNull(world);
        EntityState? best = null;
        var bestDistance = maxRange * maxRange;
        foreach (var entity in world.Entities.All.Values)
        {
            if (entity.Kind is not (EntityKind.Npc or EntityKind.Resource or EntityKind.WorldItem or EntityKind.InteractiveObject))
                continue;
            var dx = entity.Position.X - world.Local.Position.X;
            var dy = entity.Position.Y - world.Local.Position.Y;
            var distance = dx * dx + dy * dy;
            if (distance > bestDistance) continue;
            best = entity;
            bestDistance = distance;
        }

        return best;
    }
}
