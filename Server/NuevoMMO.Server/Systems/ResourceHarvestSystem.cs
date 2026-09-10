using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Systems;

public enum HarvestFailure : byte
{
    None = 0,
    ResourceUnavailable = 1,
    MissingProfession = 2,
    ProfessionLevelTooLow = 3,
    MissingTool = 4
}

public readonly record struct HarvestResult(
    bool Success,
    HarvestFailure Failure,
    float WorkApplied,
    bool Depleted,
    IReadOnlyList<LootRoll> Loot,
    string Message)
{
    public static HarvestResult Fail(HarvestFailure failure, string message)
        => new(false, failure, 0, false, [], message);
}

/// <summary>
/// Reglas autoritativas de trabajo sobre nodos recolectables. Valida requisitos y degrada la salud
/// del nodo; cuando se agota genera el loot una sola vez. Interacción/rango y entrega al inventario
/// permanecen fuera de este sistema para no acoplar mundo, profesión e inventario.
/// </summary>
public sealed class ResourceHarvestSystem
{
    private readonly DefinitionRegistry definitions;
    private readonly LootSystem loot;

    public ResourceHarvestSystem(DefinitionRegistry definitions, LootSystem loot)
    {
        this.definitions = definitions ?? throw new ArgumentNullException(nameof(definitions));
        this.loot = loot ?? throw new ArgumentNullException(nameof(loot));
    }

    public HarvestResult TryHarvest(Player player, ResourceEntity resource, float workPower, long nowMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(resource);
        if (!float.IsFinite(workPower) || workPower <= 0) throw new ArgumentOutOfRangeException(nameof(workPower));
        if (nowMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(nowMilliseconds));

        if (!resource.IsAvailable)
            return HarvestResult.Fail(HarvestFailure.ResourceUnavailable, "El recurso está agotado.");

        var harvest = resource.Harvest;
        if (harvest.RequiredProfessionId is { } professionId)
        {
            if (!player.Professions.TryGet(professionId, out var progress) || progress is null)
                return HarvestResult.Fail(HarvestFailure.MissingProfession, "No conoces la profesión requerida.");
            if (progress.Level < harvest.RequiredProfessionLevel)
                return HarvestResult.Fail(HarvestFailure.ProfessionLevelTooLow, "Tu nivel de profesión es insuficiente.");
        }

        if (harvest.RequiredToolKey is { } toolKey && !HasUsableTool(player, toolKey))
            return HarvestResult.Fail(HarvestFailure.MissingTool, "No tienes la herramienta requerida.");

        var applied = resource.ApplyHarvestDamage(workPower, nowMilliseconds);
        var depleted = !resource.IsAvailable;
        var drops = depleted && harvest.LootTableId is not null
            ? loot.Roll(definitions.Get<ResourceDefinition>(resource.DefinitionId))
            : [];

        return new(true, HarvestFailure.None, applied, depleted, drops, string.Empty);
    }

    public bool HasUsableTool(Player player, ContentKey toolKey)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (toolKey.IsEmpty) throw new ArgumentException("ToolKey vacío.", nameof(toolKey));

        foreach (var item in player.Inventory.Items)
        {
            if (!definitions.TryGet<ItemDefinition>(item.DefinitionId, out var definition) || definition is null || !definition.Enabled)
                continue;
            if (definition.ToolKey == toolKey && item.Quantity > 0 && (definition.Equipment?.MaxDurability is null || item.Durability > 0))
                return true;
        }
        return false;
    }
}
