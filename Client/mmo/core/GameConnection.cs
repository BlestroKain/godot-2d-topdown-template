using System.Net.Sockets;
using NuevoMMO.Network;

namespace NuevoMMO.Client;

public sealed class GameConnection : IDisposable
{
    private readonly TcpClient tcp = new() { NoDelay = true };
    private readonly SemaphoreSlim writer = new(1);
    private readonly CancellationTokenSource lifetime = new();
    private int disposed;
    public event Action<IPacket>? Message;
    public event Action<string>? Closed;
    public MapLoadPacket? Map { get; private set; }

    public async Task ConnectAsync(string host, int port, string username, string password, bool registerAccount = false)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));
        await tcp.ConnectAsync(host, port, timeout.Token);
        var stream = tcp.GetStream();
        await TcpPacketFraming.WriteAsync(stream, new ConnectRequest("client-dev", ProtocolVersion.Current), timeout.Token);
        if (await TcpPacketFraming.ReadAsync(stream, timeout.Token) is not ConnectionAccepted accepted)
            throw new IOException("Handshake inválido.");
        Message?.Invoke(accepted);

        if (registerAccount)
        {
            await TcpPacketFraming.WriteAsync(stream, new RegisterRequest(username, password), timeout.Token);
            if (await TcpPacketFraming.ReadAsync(stream, timeout.Token) is not RegisterResult registration)
                throw new IOException("Respuesta de registro inválida.");
            Message?.Invoke(registration);
            if (!registration.Succeeded) throw new IOException(registration.Error);
        }

        await TcpPacketFraming.WriteAsync(stream, new LoginRequest(username, password), timeout.Token);
        if (await TcpPacketFraming.ReadAsync(stream, timeout.Token) is not LoginResult login)
            throw new IOException("Login inválido.");
        if (!login.Succeeded) throw new IOException(login.Error);
        Message?.Invoke(login);
        await TcpPacketFraming.WriteAsync(stream, new CharacterListRequest(login.Session, login.SessionToken), timeout.Token);
        if (await TcpPacketFraming.ReadAsync(stream, timeout.Token) is not CharacterListResult list)
            throw new IOException("Lista de personajes inválida.");
        Message?.Invoke(list);
        CharacterSummary character;
        if (list.Characters.Length == 0)
        {
            await TcpPacketFraming.WriteAsync(stream, new CreateCharacterRequest(login.Session, login.SessionToken, username), timeout.Token);
            if (await TcpPacketFraming.ReadAsync(stream, timeout.Token) is not CharacterCreated created)
                throw new IOException("Creación de personaje inválida.");
            Message?.Invoke(created);
            character = created.Character;
        }
        else character = list.Characters[0];
        await TcpPacketFraming.WriteAsync(stream, new CharacterSelectRequest(login.Session, login.SessionToken, character.Id), timeout.Token);
        IPacket selected = await TcpPacketFraming.ReadAsync(stream, timeout.Token);
        if (selected is not CharacterSelected) throw new IOException("Selección de personaje inválida.");
        Message?.Invoke(selected);
        if (await TcpPacketFraming.ReadAsync(stream, timeout.Token) is not MapLoadPacket map)
            throw new IOException("Carga de mapa inválida.");
        Map = map;
        Message?.Invoke(map);
        await TcpPacketFraming.WriteAsync(stream, new MapReadyRequest(map.Map.Instance), timeout.Token);
        _ = ReceiveAsync();
    }

    public async Task SendAsync(IPacket packet)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        await writer.WaitAsync(timeout.Token);
        try { await TcpPacketFraming.WriteAsync(tcp.GetStream(), packet, timeout.Token); }
        finally { writer.Release(); }
    }

    private async Task ReceiveAsync()
    {
        try
        {
            while (!lifetime.IsCancellationRequested)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(10));
                var packet = await TcpPacketFraming.ReadAsync(tcp.GetStream(), timeout.Token);
                if (packet is not (EntityStatePacket or PongPacket or ServerTimePacket or SpawnEntityPacket or DespawnEntityPacket or EntityMovedPacket or ErrorPacket))
                    throw new InvalidDataException("Mensaje servidor inesperado.");
                Message?.Invoke(packet);
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or SocketException or OperationCanceledException or ObjectDisposedException)
        {
            if (!lifetime.IsCancellationRequested) Closed?.Invoke(exception.Message);
        }
        finally { Dispose(); }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        lifetime.Cancel();
        tcp.Dispose();
    }
}
