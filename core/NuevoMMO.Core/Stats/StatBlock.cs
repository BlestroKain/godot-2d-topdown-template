namespace NuevoMMO.Core;

public sealed class StatBlock
{
    public PrimaryStats Primary { get; } = new();
    public SecondaryStats Secondary { get; } = new();
    public ResistanceStats Resistances { get; } = new();
}
