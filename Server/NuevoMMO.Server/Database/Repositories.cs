using System.Security.Cryptography;
using NuevoMMO.Core;

namespace NuevoMMO.Server.Database;

public interface IAccountRepository
{
    Task<AccountRecord?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<AccountRecord> CreateAsync(string username, string passwordHash, CancellationToken cancellationToken = default);
}

public interface ISessionRepository
{
    Task<SessionRecord> CreateAsync(AccountId account, string token, CancellationToken cancellationToken = default);
    Task<SessionRecord?> GetActiveAsync(SessionId id, string token, CancellationToken cancellationToken = default);
    Task RevokeAsync(SessionId id, CancellationToken cancellationToken = default);
}

public interface ICharacterRepository
{
    Task<IReadOnlyList<CharacterRecord>> ListByAccountAsync(AccountId account, CancellationToken cancellationToken = default);
    Task<CharacterRecord?> GetAsync(CharacterId id, CancellationToken cancellationToken = default);
    Task<CharacterRecord> CreateAsync(AccountId account, string name, DefinitionId map, Vector2Data position, DefinitionId traditionId, CharacterAppearance appearance, CancellationToken cancellationToken = default);
    Task SavePositionAsync(CharacterId id, DefinitionId map, Vector2Data position, CancellationToken cancellationToken = default);
    Task SaveCheckpointAsync(
        CharacterId id,
        DefinitionId map,
        Vector2Data position,
        PlayerProgressionState progression,
        int currentHealth,
        int currentMana,
        CancellationToken cancellationToken = default);
}

public interface IInventoryRepository { }
public interface IEquipmentRepository { }
public interface IProfessionRepository { }
public interface IQuestRepository { }
public interface IMarketRepository { }
public interface IMailRepository { }
public interface IGuildRepository { }

public sealed class InMemoryAccountRepository : IAccountRepository
{
    private readonly Dictionary<string, AccountRecord> accounts = new(StringComparer.OrdinalIgnoreCase);
    public Task<AccountRecord?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => Task.FromResult(accounts.TryGetValue(username, out var account) ? account : null);

    public Task<AccountRecord> CreateAsync(string username, string passwordHash, CancellationToken cancellationToken = default)
    {
        var account = new AccountRecord { Id = new(Guid.NewGuid()), Username = username, PasswordHash = passwordHash, CreatedAt = DateTimeOffset.UtcNow };
        if (!accounts.TryAdd(username, account)) throw new InvalidOperationException("Usuario duplicado.");
        return Task.FromResult(account);
    }
}

public sealed class InMemorySessionRepository : ISessionRepository
{
    private readonly Dictionary<SessionId, SessionRecord> sessions = [];

    public Task<SessionRecord> CreateAsync(AccountId account, string token, CancellationToken cancellationToken = default)
    {
        var session = new SessionRecord
        {
            Id = new(Guid.NewGuid()), AccountId = account, Token = token,
            CreatedAt = DateTimeOffset.UtcNow
        };
        sessions.Add(session.Id, session);
        return Task.FromResult(session);
    }

    public Task<SessionRecord?> GetActiveAsync(SessionId id, string token, CancellationToken cancellationToken = default)
    {
        if (!sessions.TryGetValue(id, out var session) || !session.IsActive) return Task.FromResult<SessionRecord?>(null);
        var left = System.Text.Encoding.UTF8.GetBytes(session.Token);
        var right = System.Text.Encoding.UTF8.GetBytes(token);
        return Task.FromResult<SessionRecord?>(left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right) ? session : null);
    }

    public Task RevokeAsync(SessionId id, CancellationToken cancellationToken = default)
    {
        if (sessions.TryGetValue(id, out var session)) session.RevokedAt = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryCharacterRepository : ICharacterRepository
{
    private readonly Dictionary<CharacterId, CharacterRecord> characters = [];
    public Task<IReadOnlyList<CharacterRecord>> ListByAccountAsync(AccountId account, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CharacterRecord>>(characters.Values.Where(character => character.AccountId == account).ToArray());

    public Task<CharacterRecord?> GetAsync(CharacterId id, CancellationToken cancellationToken = default)
        => Task.FromResult(characters.TryGetValue(id, out var character) ? character : null);

    /// <summary>Compatibilidad para fixtures anteriores a Tradición/apariencia-en-creación.</summary>
    public Task<CharacterRecord> CreateAsync(
        AccountId account,
        string name,
        DefinitionId map,
        Vector2Data position,
        CancellationToken cancellationToken = default)
        => CreateAsync(account, name, map, position, CanonicalTraditions.Veyrkan.Id, CanonicalCharacterAppearance.Default, cancellationToken);

    public Task<CharacterRecord> CreateAsync(
        AccountId account,
        string name,
        DefinitionId map,
        Vector2Data position,
        DefinitionId traditionId,
        CancellationToken cancellationToken = default)
        => CreateAsync(account, name, map, position, traditionId, CanonicalCharacterAppearance.Default, cancellationToken);

    public Task<CharacterRecord> CreateAsync(AccountId account, string name, DefinitionId map, Vector2Data position, DefinitionId traditionId, CharacterAppearance appearance, CancellationToken cancellationToken = default)
    {
        if (characters.Values.Any(character => character.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Nombre de personaje duplicado.");
        var record = new CharacterRecord
        {
            Id = new(Guid.NewGuid()), AccountId = account, Name = name, MapDefinition = map,
            Position = position, TraditionId = traditionId,
            Appearance = appearance ?? throw new ArgumentNullException(nameof(appearance))
        };
        record.ApplyProgression(ProgressionRules.CreateInitial());
        characters.Add(record.Id, record);
        return Task.FromResult(record);
    }

    public Task SavePositionAsync(CharacterId id, DefinitionId map, Vector2Data position, CancellationToken cancellationToken = default)
    {
        if (!characters.TryGetValue(id, out var character)) throw new KeyNotFoundException("Personaje inexistente.");
        character.MapDefinition = map;
        character.Position = position;
        return Task.CompletedTask;
    }

    public Task SaveCheckpointAsync(
        CharacterId id,
        DefinitionId map,
        Vector2Data position,
        PlayerProgressionState progression,
        int currentHealth,
        int currentMana,
        CancellationToken cancellationToken = default)
    {
        if (!characters.TryGetValue(id, out var character)) throw new KeyNotFoundException("Personaje inexistente.");
        ProgressionRules.Validate(progression);
        if (currentHealth < 0 || currentMana < 0) throw new ArgumentOutOfRangeException(nameof(currentHealth));
        character.MapDefinition = map;
        character.Position = position;
        character.ApplyProgression(progression);
        character.CurrentHealth = currentHealth;
        character.CurrentMana = currentMana;
        return Task.CompletedTask;
    }
}
