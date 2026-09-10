namespace NuevoMMO.Core;

public readonly record struct DefinitionId(Guid Value)
{
    public static DefinitionId New() => new(Guid.NewGuid());
}

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

public readonly record struct EntityId(long Value);
public readonly record struct AccountId(Guid Value);
public readonly record struct CharacterId(Guid Value);
public readonly record struct SessionId(Guid Value);
public readonly record struct ConnectionId(Guid Value);
public readonly record struct MapInstanceId(long Value);
public readonly record struct ItemInstanceId(Guid Value);
public readonly record struct PartyId(Guid Value);
public readonly record struct GuildId(Guid Value);
