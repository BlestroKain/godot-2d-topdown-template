namespace NuevoMMO.Core;

public sealed class CombatState
{
    public bool InCombat { get; set; }
    public EntityId? Target { get; set; }
}
