namespace NuevoMMO.Server.Security;

public sealed class AuthorizationService
{
    public bool CanEnterWorld(bool authenticated, bool characterSelected) => authenticated && characterSelected;
}
