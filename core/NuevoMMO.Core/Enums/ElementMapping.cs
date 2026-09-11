namespace NuevoMMO.Core;

public static class ElementMapping
{
    public static PrimaryAttributeId PrimaryFor(Element element) => element switch
    {
        Element.Earth => PrimaryAttributeId.Strength,
        Element.Fire => PrimaryAttributeId.Intelligence,
        Element.Air => PrimaryAttributeId.Agility,
        Element.Water => PrimaryAttributeId.Spirit,
        Element.Neutral => PrimaryAttributeId.Vitality,
        _ => throw new ArgumentOutOfRangeException(nameof(element))
    };
}
