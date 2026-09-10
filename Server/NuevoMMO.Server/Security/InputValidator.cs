namespace NuevoMMO.Server.Security;

public sealed class InputValidator
{
    public static bool IsFiniteDirection(float x, float y) => float.IsFinite(x) && float.IsFinite(y) && MathF.Abs(x) <= 1 && MathF.Abs(y) <= 1;
}
