namespace NuevoMMO.GodotClient;

public partial class NetworkBridge
{
    // Compatibilidad temporal con MmoGame.BuildUi. El frontend nuevo llama
    // directamente LoginLobby/RegisterLobby y esta consola debug permanece oculta.
    public void Login(string host, int port, string username, string password)
        => LoginLobby(host, port, username, password);

    public void Register(string host, int port, string username, string password)
        => RegisterLobby(host, port, username, password);
}
