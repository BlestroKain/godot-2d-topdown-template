namespace NuevoMMO.Core;

public sealed class MovementState
{
    public bool CanMove { get; set; } = true;
    public float SpeedMultiplier { get; set; } = 1f;
}
