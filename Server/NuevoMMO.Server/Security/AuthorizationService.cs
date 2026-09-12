using NuevoMMO.Server.World;

namespace NuevoMMO.Server.Security;

public enum ServerAction : byte
{
    AcceptProtocol = 0,
    Register = 1,
    Login = 2,
    ListCharacters = 3,
    CreateCharacter = 4,
    SelectCharacter = 5,
    EnterWorld = 6,
    Move = 7,
    Interact = 8,
    UseTechnique = 9,
    Chat = 10,
    AllocateAttribute = 11,
    Equip = 12,
    ManageInventory = 13
}

public readonly record struct AuthorizationDecision(bool Allowed, string Reason)
{
    public static AuthorizationDecision Allow() => new(true, string.Empty);
    public static AuthorizationDecision Deny(string reason) => new(false, reason);
}

/// <summary>
/// Política explícita de orden de sesión. El cliente puede pedir cualquier packet; el servidor decide
/// si esa acción es válida para el estado autoritativo actual.
/// </summary>
public sealed class AuthorizationService
{
    public bool CanEnterWorld(bool authenticated, bool characterSelected) => authenticated && characterSelected;

    public AuthorizationDecision Authorize(PlayerSession session, ServerAction action)
    {
        ArgumentNullException.ThrowIfNull(session);
        var state = session.State;

        var allowed = action switch
        {
            ServerAction.AcceptProtocol => state == PlayerSessionState.Connected,
            ServerAction.Register or ServerAction.Login => state == PlayerSessionState.ProtocolAccepted,
            ServerAction.ListCharacters or ServerAction.CreateCharacter or ServerAction.SelectCharacter
                => state == PlayerSessionState.Authenticated,
            ServerAction.EnterWorld => state == PlayerSessionState.WaitingForMap,
            ServerAction.Move or ServerAction.Interact or ServerAction.UseTechnique or ServerAction.Chat
                or ServerAction.AllocateAttribute or ServerAction.Equip or ServerAction.ManageInventory
                => state == PlayerSessionState.InWorld && session.Player is not null,
            _ => false
        };

        return allowed
            ? AuthorizationDecision.Allow()
            : AuthorizationDecision.Deny($"La acción {action} no está permitida durante el estado {state}.");
    }

    public void Demand(PlayerSession session, ServerAction action)
    {
        var decision = Authorize(session, action);
        if (!decision.Allowed) throw new UnauthorizedAccessException(decision.Reason);
    }
}
