namespace NuevoMMO.Server.Security;

/// <summary>Validaciones baratas para datos no confiables antes de entregarlos al runtime.</summary>
public sealed class InputValidator
{
    public static bool IsFiniteDirection(float x, float y)
        => float.IsFinite(x) && float.IsFinite(y) && MathF.Abs(x) <= 1 && MathF.Abs(y) <= 1;

    public static bool IsNormalizedOrZero(float x, float y, float tolerance = 0.01f)
    {
        if (!IsFiniteDirection(x, y) || !float.IsFinite(tolerance) || tolerance < 0) return false;
        var lengthSquared = x * x + y * y;
        return lengthSquared <= tolerance * tolerance || lengthSquared <= (1f + tolerance) * (1f + tolerance);
    }

    public static bool IsValidSequence(long sequence, long lastAcceptedSequence, long maxLead = 1_024)
    {
        if (sequence < 0 || lastAcceptedSequence < -1 || maxLead < 1) return false;
        if (sequence <= lastAcceptedSequence) return false;
        return sequence - lastAcceptedSequence <= maxLead;
    }

    public static bool IsBoundedText(string? value, int minLength, int maxLength, bool allowLineBreaks = false)
    {
        if (minLength < 0 || maxLength < minLength) throw new ArgumentOutOfRangeException(nameof(maxLength));
        if (value is null || value.Length < minLength || value.Length > maxLength) return false;
        foreach (var character in value)
        {
            if (char.IsControl(character) && (!allowLineBreaks || character is not ('\r' or '\n' or '\t'))) return false;
        }
        return true;
    }

    public static bool IsSafeName(string? value, int minLength = 3, int maxLength = 24)
    {
        if (!IsBoundedText(value, minLength, maxLength)) return false;
        return value!.All(character => char.IsLetterOrDigit(character) || character is '_' or '-');
    }
}
