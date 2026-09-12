using NuevoMMO.Network;

namespace NuevoMMO.Server.NetworkHandlers;

/// <summary>
/// Clasifica errores de dominio esperables que deben responder al cliente sin destruir la sesión.
/// Errores de framing/protocolo, dirección de paquete o seguridad no pasan por esta política.
/// </summary>
public static class ServerRequestFailurePolicy
{
    public static bool CanRecover(IPacket packet, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(packet);
        ArgumentNullException.ThrowIfNull(exception);
        return packet switch
        {
            CreateCharacterRequest => exception is ArgumentException or InvalidOperationException,
            CharacterSelectRequest => exception is ArgumentException or InvalidOperationException or KeyNotFoundException,
            _ => false
        };
    }

    public static ErrorPacket ToPacket(IPacket packet)
        => packet switch
        {
            CreateCharacterRequest => new ErrorPacket(
                "character_create",
                "No se pudo crear el personaje. Revisa el nombre y los datos de creación.",
                false),
            CharacterSelectRequest => new ErrorPacket(
                "character_select",
                "No se pudo seleccionar ese personaje.",
                false),
            _ => throw new InvalidOperationException("El paquete no tiene política de recuperación.")
        };
}
