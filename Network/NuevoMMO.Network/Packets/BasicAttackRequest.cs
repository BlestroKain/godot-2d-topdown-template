using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed record BasicAttackRequest(EntityId Target) : IPacket;

public sealed record UseTechniqueRequest(DefinitionId TechniqueId, EntityId Target, Vector2Data Point) : IPacket;

public sealed record InteractRequest(EntityId Target) : IPacket;

public sealed record SetTargetRequest(EntityId Target) : IPacket;
