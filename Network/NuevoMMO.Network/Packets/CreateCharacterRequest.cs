using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed class CreateCharacterRequest(
    SessionId session,
    string sessionToken,
    string name,
    DefinitionId traditionId) : IPacket
{
    /// <summary>
    /// Compatibilidad para fixtures/harness antiguos. El frontend de producción siempre envía
    /// una Tradición elegida explícitamente; esta ruta usa Veyrkan únicamente para código legacy.
    /// </summary>
    public CreateCharacterRequest(SessionId session, string sessionToken, string name)
        : this(session, sessionToken, name, CanonicalTraditions.Veyrkan.Id) { }

    public SessionId Session { get; } = session;
    public string SessionToken { get; } = sessionToken;
    public string Name { get; } = name;
    public DefinitionId TraditionId { get; } = traditionId;
    public override string ToString() =>
        $"CreateCharacterRequest {{ Session = {Session}, SessionToken = [REDACTED], Name = {Name}, TraditionId = {TraditionId} }}";
}
