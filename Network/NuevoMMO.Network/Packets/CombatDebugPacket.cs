using NuevoMMO.Core;

namespace NuevoMMO.Network;

/// <summary>Telemetría de combate para Development/Test; no forma parte de la UI final del juego.</summary>
public sealed record CombatDebugPacket(
    EntityId Target,
    DevelopmentAttackKind Attack,
    Element DamageType,
    PrimaryAttributeId ScalingAttribute,
    float RawDamage,
    float ResistancePercent,
    int AppliedDamage,
    bool Critical,
    int TargetHealth,
    int TargetMaxHealth,
    float Dps5Seconds,
    float Dps10Seconds,
    long TotalDamage,
    int Hits,
    int CriticalHits) : IPacket;
