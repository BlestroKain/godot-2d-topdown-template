namespace NuevoMMO.Core;

public readonly record struct ContentKey
{
    public string Value { get; }

    public ContentKey(string value)
    {
        value = value?.Trim() ?? throw new ArgumentNullException(nameof(value));
        if (value.Length is < 1 or > 128 || value.Any(character => char.IsControl(character) || char.IsWhiteSpace(character)))
            throw new ArgumentException("ContentKey inválido.", nameof(value));
        Value = value;
    }

    public override string ToString() => Value;
}
