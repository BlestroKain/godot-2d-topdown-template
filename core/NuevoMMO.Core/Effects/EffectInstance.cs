namespace NuevoMMO.Core;

public sealed class EffectInstance
{
    public EffectInstance(DefinitionId definitionId, TimeSpan remaining)
    {
        if (definitionId.Value == Guid.Empty) throw new ArgumentException("DefinitionId inválido.", nameof(definitionId));
        DefinitionId = definitionId;
        Remaining = remaining;
    }

    public DefinitionId DefinitionId { get; }
    public TimeSpan Remaining { get; set; }
}
