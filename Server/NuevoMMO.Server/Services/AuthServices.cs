using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using NuevoMMO.Core;
using NuevoMMO.Network;
using NuevoMMO.Server.Database;
using NuevoMMO.Server.World;

namespace NuevoMMO.Server.Services;

public sealed class AuthService(IAccountRepository accounts, PasswordHasher<string> hasher)
{
    public async Task<(AccountRecord Account, SessionRecord Session)> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length > 24 || username.Any(char.IsControl))
            throw new ArgumentException("Usuario inválido.");
        username = username.Trim();
        var account = await accounts.FindByUsernameAsync(username, cancellationToken);
        if (account is null)
        {
            var hash = hasher.HashPassword(username, password ?? "");
            account = await accounts.CreateAsync(username, hash, cancellationToken);
        }
        else if (hasher.VerifyHashedPassword(username, account.PasswordHash, password ?? "") == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Credenciales inválidas.");
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        return (account, new SessionRecord { Id = new(Guid.NewGuid()), AccountId = account.Id, Token = token, CreatedAt = DateTimeOffset.UtcNow });
    }

    public static void EnsureSession(PlayerSession session, SessionId id, string token)
    {
        if (session.State is not (PlayerSessionState.Authenticated or PlayerSessionState.CharacterSelected
            or PlayerSessionState.WaitingForMap or PlayerSessionState.InWorld))
            throw new InvalidOperationException("Sesión no autenticada.");
        if (session.Session != id || session.SessionToken != token) throw new InvalidOperationException("Token de sesión inválido.");
    }
}

public sealed class CharacterService(ICharacterRepository characters, MapDefinition map)
{
    public Task<IReadOnlyList<CharacterRecord>> ListAsync(AccountId account, CancellationToken cancellationToken = default)
        => characters.ListByAccountAsync(account, cancellationToken);

    public async Task<CharacterRecord> CreateAsync(AccountId account, string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 24 || name.Any(char.IsControl))
            throw new ArgumentException("Nombre de personaje inválido.");
        return await characters.CreateAsync(account, name.Trim(), map.Id, map.Spawn, cancellationToken);
    }

    public static CharacterSummary ToSummary(CharacterRecord record)
        => new(record.Id, record.Name, record.MapDefinition, record.Position);
}
