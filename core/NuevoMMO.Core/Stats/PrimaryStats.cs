namespace NuevoMMO.Core;

public sealed class PrimaryStats
{
    public int Strength { get; set; }
    public int Intelligence { get; set; }
    public int Agility { get; set; }
    public int Spirit { get; set; }
    public int Vitality { get; set; }

    public int Get(PrimaryAttributeId id) => id switch
    {
        PrimaryAttributeId.Strength => Strength,
        PrimaryAttributeId.Intelligence => Intelligence,
        PrimaryAttributeId.Agility => Agility,
        PrimaryAttributeId.Spirit => Spirit,
        PrimaryAttributeId.Vitality => Vitality,
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    };
}
