using System.Runtime.CompilerServices;
using NuevoMMO.Core;
using NuevoMMO.Network;
using NuevoMMO.Server.NetworkHandlers;

internal static class CharacterCreationCanonVerification
{
    [ModuleInitializer]
    internal static void Verify()
    {
        Expect(CanonicalTraditions.IsValidAtCreate(DefinitionId.Empty), "Novicio es válido al crear");
        Expect(!CanonicalTraditions.IsValidAtCreate(CanonicalTraditions.Veyrkan.Id), "una Tradición publicada no se elige al crear");
        Expect(CanonicalTraditions.IsSelectable(CanonicalTraditions.Veyrkan.Id), "la Tradición sigue publicada para aprendizaje runtime");

        var request = new CreateCharacterRequest(
            new SessionId(Guid.NewGuid()),
            "token-de-prueba",
            "Heroe",
            DefinitionId.Empty,
            CanonicalCharacterAppearance.Default);
        var rejected = new ArgumentException("Nombre inválido.");
        Expect(ServerRequestFailurePolicy.CanRecover(request, rejected), "rechazo de create es recuperable");
        var response = ServerRequestFailurePolicy.ToPacket(request);
        Expect(!response.Fatal && response.Code == "character_create", "create inválido responde error no fatal");

        var handshake = new ConnectRequest("test", 1);
        Expect(!ServerRequestFailurePolicy.CanRecover(handshake, rejected), "handshake no hereda política de create");
        Expect(!ServerRequestFailurePolicy.CanRecover(request, new InvalidDataException("framing")), "error de protocolo no es recuperable como dominio");
    }

    private static void Expect(bool value, string name)
    {
        if (!value) throw new InvalidOperationException("CharacterCreationCanonVerification FAIL: " + name);
    }
}
