using NuevoMMO.Core;

namespace NuevoMMO.Network;

/// <summary>Acciones técnicas de combate disponibles solo en el fixture Development/Test.</summary>
public enum DevelopmentAttackKind : byte
{
    Basic = 0,
    Earth = 1,
    Fire = 2,
    Air = 3,
    Water = 4,
    NeutralStrength = 5
}

public sealed record DevelopmentAttackRequest(EntityId Target, DevelopmentAttackKind Attack) : IPacket;
