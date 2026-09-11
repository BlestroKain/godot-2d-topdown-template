using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed class CreateCharacterRequest(
    SessionId session,
    string sessionToken,
    string name,
    DefinitionId traditionId,
    CharacterAppearance appearance) : IPacket
{
    /// <summary>
    /// Compatibilidad para fixtures/harness antiguos. El frontend de producción siempre envía
    /// una Tradición elegida explícitamente; esta ruta usa Veyrkan únicamente para código legacy.
    /// </summary>
    public CreateCharacterRequest(SessionId session, string sessionToken, string name)
        : this(session, sessionToken, name, CanonicalTraditions.Veyrkan.Id, CanonicalCharacterAppearance.Default) { }

    public CreateCharacterRequest(SessionId session, string sessionToken, string name, DefinitionId traditionId)
        : this(session, sessionToken, name, traditionId, CanonicalCharacterAppearance.Default) { }

    public SessionId Session { get; } = session;
    public string SessionToken { get; } = sessionToken;
    public string Name { get; } = name;
    public DefinitionId TraditionId { get; } = traditionId;
    public CharacterAppearance Appearance { get; } = Validate(appearance);
    public override string ToString() =>
        $"CreateCharacterRequest {{ Session = {Session}, SessionToken = [REDACTED], Name = {Name}, TraditionId = {TraditionId}, BaseVisual = {Appearance.BaseVisual} }}";

    private static CharacterAppearance Validate(CharacterAppearance value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!CanonicalCharacterAppearance.IsSupported(value))
            throw new ArgumentException("La apariencia todavía no está publicada en el catálogo del cliente/servidor.", nameof(value));
        return value;
    }
}
