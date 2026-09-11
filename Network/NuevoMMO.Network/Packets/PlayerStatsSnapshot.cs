namespace NuevoMMO.Network;

/// <summary>Un atributo visto por el cliente: valor natural persistido, valor efectivo y coste del siguiente punto.</summary>
public sealed record AttributeSnapshot(int Natural, int Effective, int NextCost);

/// <summary>
/// Estado de progresión/stats que el servidor permite presentar al cliente.
/// No es autoridad: cualquier cambio debe volver al servidor mediante comandos.
/// </summary>
public sealed record PlayerStatsSnapshot(
    int Level,
    long Experience,
    long ExperienceToNextLevel,
    int AvailableAttributePoints,
    AttributeSnapshot Strength,
    AttributeSnapshot Intelligence,
    AttributeSnapshot Agility,
    AttributeSnapshot Spirit,
    AttributeSnapshot Vitality,
    int Health,
    int MaxHealth,
    int Mana,
    int MaxMana,
    float Defense,
    float ManaRegenPerSecond,
    float OutOfCombatManaRegenPerSecond,
    int Luck,
    int ResistEarth,
    int ResistFire,
    int ResistAir,
    int ResistWater,
    int ResistNeutral);
