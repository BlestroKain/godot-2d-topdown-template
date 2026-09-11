namespace NuevoMMO.Core;

/// <summary>
/// Valores naturales distribuidos por el jugador. No incluyen equipo, Tradición,
/// buffs ni efectos temporales.
/// </summary>
public sealed record NaturalPrimaryStats(
    int Strength,
    int Intelligence,
    int Agility,
    int Spirit,
    int Vitality)
{
    public int Get(PrimaryAttributeId id) => id switch
    {
        PrimaryAttributeId.Strength => Strength,
        PrimaryAttributeId.Intelligence => Intelligence,
        PrimaryAttributeId.Agility => Agility,
        PrimaryAttributeId.Spirit => Spirit,
        PrimaryAttributeId.Vitality => Vitality,
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
    };

    public NaturalPrimaryStats With(PrimaryAttributeId id, int value) => id switch
    {
        PrimaryAttributeId.Strength => this with { Strength = value },
        PrimaryAttributeId.Intelligence => this with { Intelligence = value },
        PrimaryAttributeId.Agility => this with { Agility = value },
        PrimaryAttributeId.Spirit => this with { Spirit = value },
        PrimaryAttributeId.Vitality => this with { Vitality = value },
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
    };

    public PrimaryStats ToPrimaryStats() => new()
    {
        Strength = Strength,
        Intelligence = Intelligence,
        Agility = Agility,
        Spirit = Spirit,
        Vitality = Vitality
    };
}

/// <summary>
/// Estado persistente mínimo de progresión de personaje. Los stats efectivos y
/// máximos de vitales se derivan; no deben guardarse como verdad paralela.
/// </summary>
public sealed record PlayerProgressionState(
    int Level,
    long Experience,
    int AvailableAttributePoints,
    NaturalPrimaryStats NaturalAttributes);
