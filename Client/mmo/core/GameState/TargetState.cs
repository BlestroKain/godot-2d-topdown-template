using NuevoMMO.Core;

namespace NuevoMMO.Client;

public sealed class TargetState
{
    public EntityId? Id { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public EntityKind? Kind { get; private set; }
    public bool HasTarget => Id is { } id && id.Value > 0;

    public void Set(EntityState entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Id = entity.Id;
        DisplayName = entity.DisplayName;
        Kind = entity.Kind;
    }

    public void Clear()
    {
        Id = null;
        DisplayName = string.Empty;
        Kind = null;
    }
}
