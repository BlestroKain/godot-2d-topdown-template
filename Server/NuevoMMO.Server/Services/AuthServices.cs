using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using NuevoMMO.Core;
using NuevoMMO.Network;
using NuevoMMO.Server.Database;
using NuevoMMO.Server.World;

namespace NuevoMMO.Server.Services;

public sealed class AuthService(IAccountRepository accounts, ISessionRepository sessions, PasswordHasher<string> hasher)
{
    public async Task<AccountRecord> RegisterAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        username = ValidateUsername(username);
        ValidatePassword(password);
        if (await accounts.FindByUsernameAsync(username, cancellationToken) is not null)
            throw new InvalidOperationException("El usuario ya existe.");
        var hash = hasher.HashPassword(username, password);
        return await accounts.CreateAsync(username, hash, cancellationToken);
    }

    public async Task<(AccountRecord Account, SessionRecord Session)> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        username = ValidateUsername(username);
        if (string.IsNullOrEmpty(password)) throw new ArgumentException("Contraseña inválida.");
        var account = await accounts.FindByUsernameAsync(username, cancellationToken)
            ?? throw new InvalidOperationException("Credenciales inválidas.");
        var verification = hasher.VerifyHashedPassword(account.Username, account.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Credenciales inválidas.");
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var session = await sessions.CreateAsync(account.Id, token, cancellationToken);
        return (account, session);
    }

    public static void EnsureSession(PlayerSession session, SessionId id, string token)
    {
        if (session.State is not (PlayerSessionState.Authenticated or PlayerSessionState.CharacterSelected
            or PlayerSessionState.WaitingForMap or PlayerSessionState.InWorld))
            throw new InvalidOperationException("Sesión no autenticada.");
        if (session.Session != id || !FixedTimeEquals(session.SessionToken, token))
            throw new InvalidOperationException("Token de sesión inválido.");
    }

    private static string ValidateUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length is < 3 or > 24 || username.Any(char.IsControl))
            throw new ArgumentException("El usuario debe tener entre 3 y 24 caracteres válidos.");
        username = username.Trim();
        if (username.Any(character => !(char.IsLetterOrDigit(character) || character is '_' or '-')))
            throw new ArgumentException("El usuario solo puede contener letras, números, '_' y '-'.");
        return username;
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length is < 8 or > 128)
            throw new ArgumentException("La contraseña debe tener entre 8 y 128 caracteres.");
        if (password.Any(char.IsControl)) throw new ArgumentException("La contraseña contiene caracteres inválidos.");
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var a = System.Text.Encoding.UTF8.GetBytes(left ?? string.Empty);
        var b = System.Text.Encoding.UTF8.GetBytes(right ?? string.Empty);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }
}

public sealed class CharacterService(ICharacterRepository characters, MapDefinition map)
{
    public Task<IReadOnlyList<CharacterRecord>> ListAsync(AccountId account, CancellationToken cancellationToken = default)
        => characters.ListByAccountAsync(account, cancellationToken);

    public async Task<CharacterRecord> CreateAsync(
        AccountId account,
        string name,
        DefinitionId traditionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 24 || name.Any(char.IsControl))
            throw new ArgumentException("Nombre de personaje inválido.");
        if (!CanonicalTraditions.IsSelectable(traditionId))
            throw new ArgumentException("Tradición inválida o no seleccionable.", nameof(traditionId));
        return await characters.CreateAsync(account, name.Trim(), map.Id, map.Spawn, traditionId, cancellationToken);
    }

    public static CharacterSummary ToSummary(CharacterRecord record)
        => new(record.Id, record.Name, record.MapDefinition, record.Position, record.TraditionId);
}
