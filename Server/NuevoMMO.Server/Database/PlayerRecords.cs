using NuevoMMO.Core;

namespace NuevoMMO.Server.Database;

public sealed class AccountRecord
{
    public AccountId Id { get; init; }
    public string Username { get; init; } = "";
    public string PasswordHash { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CharacterRecord
{
    public CharacterId Id { get; init; }
    public AccountId AccountId { get; init; }
    public string Name { get; init; } = "";
    public DefinitionId MapDefinition { get; set; }
    public Vector2Data Position { get; set; }
    public int Level { get; set; } = 1;
    public long Experience { get; set; }
}

public sealed class SessionRecord
{
    public SessionId Id { get; init; }
    public AccountId AccountId { get; init; }
    public string Token { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? RevokedAt { get; set; }
    public bool IsActive => RevokedAt is null;
}
