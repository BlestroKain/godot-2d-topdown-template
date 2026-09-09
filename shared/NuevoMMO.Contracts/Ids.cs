namespace NuevoMMO.Contracts;

public readonly record struct EntityId(long Value);
public readonly record struct CharacterId(Guid Value);
public readonly record struct AccountId(Guid Value);
public readonly record struct ConnectionId(Guid Value);
public readonly record struct MapId(Guid Value);
public readonly record struct RegionId(Guid Value);
public readonly record struct MapInstanceId(long Value);
public readonly record struct ItemDefinitionId(Guid Value);
public readonly record struct ItemInstanceId(Guid Value);
