using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.World;

/// <summary>
/// Materializa placements y spawn-zones de una MapDefinition sobre el WorldRuntime.
/// No inventa tablas ni fórmulas: solo instancia Definitions ya validadas.
/// </summary>
public static class ContentWorldPopulator
{
    public static int Populate(WorldRuntime world, DefinitionRegistry definitions, MapDefinition? map = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(definitions);
        var source = map ?? world.Map;
        var spawned = 0;

        foreach (var placement in source.Content.Placements)
            spawned += SpawnPlacement(world, definitions, placement) is null ? 0 : 1;

        foreach (var zone in source.Content.SpawnZones)
            spawned += PopulateZone(world, definitions, zone);

        return spawned;
    }

    public static Entity? SpawnPlacement(
        WorldRuntime world,
        DefinitionRegistry definitions,
        MapContentPlacementDefinition placement)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(placement);
        return Spawn(world, definitions, placement.Kind, placement.DefinitionId, placement.Position);
    }

    public static int PopulateZone(WorldRuntime world, DefinitionRegistry definitions, MapSpawnZoneDefinition zone)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(zone);

        var table = definitions.Get<SpawnTableDefinition>(zone.SpawnTableId);
        if (table.Entries.Length == 0) return 0;

        var cap = zone.MaximumAliveOverride > 0
            ? zone.MaximumAliveOverride
            : table.MaximumTotalAlive > 0 ? table.MaximumTotalAlive : 1;
        cap = Math.Max(1, cap);

        var spawned = 0;
        for (var index = 0; index < cap; index++)
        {
            var entry = SelectEntry(table, index);
            var position = PointInZone(zone.Area, index, cap);
            if (Spawn(world, definitions, entry.Kind, entry.DefinitionId, position) is not null)
                spawned++;
        }

        return spawned;
    }

    private static Entity? Spawn(
        WorldRuntime world,
        DefinitionRegistry definitions,
        SpawnEntityKind kind,
        DefinitionId definitionId,
        Vector2Data position)
    {
        return kind switch
        {
            SpawnEntityKind.Mob when definitions.TryGet<MobDefinition>(definitionId, out var mob) && mob is not null
                => world.SpawnMob(mob, position),
            SpawnEntityKind.Npc when definitions.TryGet<NpcDefinition>(definitionId, out var npc) && npc is not null
                => world.SpawnNpc(npc, position),
            SpawnEntityKind.Resource when definitions.TryGet<ResourceDefinition>(definitionId, out var resource) && resource is not null
                => world.SpawnResource(resource, position),
            _ => throw new InvalidOperationException($"No se puede spawnear {kind} {definitionId}: Definition ausente o tipo incorrecto.")
        };
    }

    private static SpawnEntryDefinition SelectEntry(SpawnTableDefinition table, int index)
    {
        var entries = table.Entries;
        if (entries.Length == 1) return entries[0];

        if (table.SelectionMode is SpawnSelectionMode.Sequence or SpawnSelectionMode.All)
            return entries[index % entries.Length];

        var totalWeight = entries.Sum(static entry => entry.Weight);
        if (totalWeight <= 0) return entries[index % entries.Length];

        var cursor = ((index + 1) * 0.6180339887d % 1d) * totalWeight;
        var accumulated = 0d;
        foreach (var entry in entries)
        {
            accumulated += entry.Weight;
            if (cursor <= accumulated) return entry;
        }

        return entries[^1];
    }

    private static Vector2Data PointInZone(MapShapeDefinition area, int index, int total)
    {
        if (total <= 1) return area.Center;
        var t = (index + 0.5f) / total - 0.5f;
        return area.Kind switch
        {
            MapShapeKind.Rectangle => new(
                area.Center.X + t * area.Size.X * 0.5f,
                area.Center.Y),
            MapShapeKind.Circle => new(
                area.Center.X + t * area.Radius,
                area.Center.Y),
            _ => area.Center
        };
    }
}
