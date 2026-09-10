namespace NuevoMMO.Server.Security;

public sealed class AuthenticationSettings
{
    /// <summary>Solo para fixtures/dev explícitos. Producción nunca debe depender de creación implícita.</summary>
    public bool DevelopmentAutoCreate { get; init; } = false;
    public int MinimumUsernameLength { get; init; } = 3;
    public int MaximumUsernameLength { get; init; } = 24;
    public int MinimumPasswordLength { get; init; } = 8;
    public int MaximumPasswordLength { get; init; } = 128;
    public int LoginAttemptsPerWindow { get; init; } = 8;
    public int LoginWindowMilliseconds { get; init; } = 60_000;
    public int RegistrationAttemptsPerWindow { get; init; } = 4;
    public int RegistrationWindowMilliseconds { get; init; } = 60_000;

    public void Validate()
    {
        if (MinimumUsernameLength < 1 || MaximumUsernameLength < MinimumUsernameLength)
            throw new InvalidOperationException("Rango de longitud de usuario inválido.");
        if (MinimumPasswordLength < 1 || MaximumPasswordLength < MinimumPasswordLength)
            throw new InvalidOperationException("Rango de longitud de contraseña inválido.");
        if (LoginAttemptsPerWindow < 1 || LoginWindowMilliseconds < 1)
            throw new InvalidOperationException("Rate limit de login inválido.");
        if (RegistrationAttemptsPerWindow < 1 || RegistrationWindowMilliseconds < 1)
            throw new InvalidOperationException("Rate limit de registro inválido.");
    }
}
