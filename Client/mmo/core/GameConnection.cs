using System.Net.Sockets;
using NuevoMMO.Core;
using NuevoMMO.Network;

namespace NuevoMMO.Client;

/// <summary>
/// Transporte de cliente dividido en dos fases: lobby y mundo.
/// Durante lobby las operaciones son request/response secuenciales; el receptor
/// continuo solo arranca cuando el personaje entra al mundo.
/// </summary>
public sealed class GameConnection : IDisposable
{
    private readonly TcpClient tcp = new() { NoDelay = true };
    private readonly SemaphoreSlim writer = new(1);
    private readonly CancellationTokenSource lifetime = new();
    private int disposed;
    private int realtimeStarted;
    private SessionId session;
    private string sessionToken = string.Empty;

    public event Action<IPacket>? Message;
    public event Action<string>? Closed;

    public MapLoadPacket? Map { get; private set; }
    public bool IsConnected => tcp.Connected && Volatile.Read(ref disposed) == 0;
    public bool IsAuthenticated => !string.IsNullOrEmpty(sessionToken);
    public SessionId Session => session;

    public Task ConnectAsync(string host, int port, string name)
        => ConnectAsync(host, port, name, "development", true);

    /// <summary>
    /// Compatibilidad con el antiguo vertical slice. El flujo de producción usa
    /// Open/Login/List/Create/Enter por separado desde el frontend.
    /// </summary>
    public async Task ConnectAsync(string host, int port, string username, string password, bool registerAccount = false)
    {
        await OpenAsync(host, port);
        if (registerAccount) await RegisterAsync(username, password);
        await LoginAsync(username, password);
        var list = await ListCharactersAsync();
        var character = list.Characters.Length == 0
            ? (await CreateCharacterAsync(username, CanonicalTraditions.Veyrkan.Id, CanonicalCharacterAppearance.Default)).Character
            : list.Characters[0];
        await EnterWorldAsync(character.Id);
    }

    public async Task<ConnectionAccepted> OpenAsync(string host, int port)
    {
        ThrowIfDisposed();
        if (tcp.Connected) throw new InvalidOperationException("La conexión ya está abierta.");
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("Host requerido.", nameof(host));
        if (port is <= 0 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));

        using var timeout = LobbyTimeout();
        await tcp.ConnectAsync(host.Trim(), port, timeout.Token);
        await WriteAsync(new ConnectRequest("client-lobby", ProtocolVersion.Current), timeout.Token);
        var accepted = await ReadExpectedAsync<ConnectionAccepted>(timeout.Token, "Handshake inválido.");
        Message?.Invoke(accepted);
        return accepted;
    }

    public async Task<RegisterResult> RegisterAsync(string username, string password)
    {
        DemandLobby();
        using var timeout = LobbyTimeout();
        await WriteAsync(new RegisterRequest(username, password), timeout.Token);
        var result = await ReadExpectedAsync<RegisterResult>(timeout.Token, "Respuesta de registro inválida.");
        Message?.Invoke(result);
        if (!result.Succeeded) throw new IOException(result.Error);
        return result;
    }

    public async Task<LoginResult> LoginAsync(string username, string password)
    {
        DemandLobby();
        using var timeout = LobbyTimeout();
        await WriteAsync(new LoginRequest(username, password), timeout.Token);
        var result = await ReadExpectedAsync<LoginResult>(timeout.Token, "Respuesta de login inválida.");
        Message?.Invoke(result);
        if (!result.Succeeded) throw new IOException(result.Error);
        session = result.Session;
        sessionToken = result.SessionToken;
        return result;
    }

    public async Task<CharacterListResult> ListCharactersAsync()
    {
        DemandAuthenticatedLobby();
        using var timeout = LobbyTimeout();
        await WriteAsync(new CharacterListRequest(session, sessionToken), timeout.Token);
        var result = await ReadExpectedAsync<CharacterListResult>(timeout.Token, "Lista de personajes inválida.");
        Message?.Invoke(result);
        return result;
    }

    public Task<CharacterCreated> CreateCharacterAsync(string name)
        => CreateCharacterAsync(name, CanonicalTraditions.Veyrkan.Id, CanonicalCharacterAppearance.Default);

    public Task<CharacterCreated> CreateCharacterAsync(string name, DefinitionId traditionId)
        => CreateCharacterAsync(name, traditionId, CanonicalCharacterAppearance.Default);

    public async Task<CharacterCreated> CreateCharacterAsync(string name, DefinitionId traditionId, CharacterAppearance appearance)
    {
        DemandAuthenticatedLobby();
        if (!CanonicalTraditions.IsSelectable(traditionId))
            throw new ArgumentException("Tradición inválida o no seleccionable.", nameof(traditionId));
        if (!CanonicalCharacterAppearance.IsSupported(appearance))
            throw new ArgumentException("Apariencia no publicada.", nameof(appearance));
        using var timeout = LobbyTimeout();
        await WriteAsync(new CreateCharacterRequest(session, sessionToken, name, traditionId, appearance), timeout.Token);
        var result = await ReadExpectedAsync<CharacterCreated>(timeout.Token, "Creación de personaje inválida.");
        Message?.Invoke(result);
        return result;
    }

    public async Task<MapLoadPacket> EnterWorldAsync(CharacterId character)
    {
        DemandAuthenticatedLobby();
        using var timeout = LobbyTimeout();
        await WriteAsync(new CharacterSelectRequest(session, sessionToken, character), timeout.Token);
        var selected = await ReadExpectedAsync<CharacterSelected>(timeout.Token, "Selección de personaje inválida.");
        Message?.Invoke(selected);
        var map = await ReadExpectedAsync<MapLoadPacket>(timeout.Token, "Carga de mapa inválida.");
        Map = map;
        Message?.Invoke(map);
        await WriteAsync(new MapReadyRequest(map.Map.Instance), timeout.Token);
        StartRealtime();
        return map;
    }

    public async Task SendAsync(IPacket packet)
    {
        ThrowIfDisposed();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        await WriteAsync(packet, timeout.Token);
    }

    private void StartRealtime()
    {
        if (Interlocked.Exchange(ref realtimeStarted, 1) != 0) return;
        _ = ReceiveAsync();
        _ = KeepAliveAsync();
    }

    private async Task WriteAsync(IPacket packet, CancellationToken cancellationToken)
    {
        await writer.WaitAsync(cancellationToken);
        try { await TcpPacketFraming.WriteAsync(tcp.GetStream(), packet, cancellationToken); }
        finally { writer.Release(); }
    }

    private async Task<T> ReadExpectedAsync<T>(CancellationToken cancellationToken, string invalidMessage) where T : class, IPacket
    {
        var packet = await TcpPacketFraming.ReadAsync(tcp.GetStream(), cancellationToken)
            ?? throw new EndOfStreamException("Conexión cerrada por el servidor.");
        if (packet is ErrorPacket error) throw new IOException(error.Message);
        return packet as T ?? throw new IOException(invalidMessage);
    }

    private async Task ReceiveAsync()
    {
        try
        {
            while (!lifetime.IsCancellationRequested)
            {
                var packet = await TcpPacketFraming.ReadAsync(tcp.GetStream(), lifetime.Token);
                if (packet is null)
                {
                    if (!lifetime.IsCancellationRequested) Closed?.Invoke("disconnected");
                    break;
                }

                if (packet is MapLoadPacket map)
                {
                    Map = map;
                    Message?.Invoke(map);
                    await SendAsync(new MapReadyRequest(map.Map.Instance));
                    continue;
                }

                Message?.Invoke(packet);
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or SocketException or OperationCanceledException or ObjectDisposedException)
        {
            if (!lifetime.IsCancellationRequested)
                Closed?.Invoke(exception is EndOfStreamException or OperationCanceledException ? "disconnected" : exception.Message);
        }
        finally { Dispose(); }
    }

    private async Task KeepAliveAsync()
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            while (await timer.WaitForNextTickAsync(lifetime.Token))
                await SendAsync(new PingPacket(Environment.TickCount64, NetworkClock.Timestamp));
        }
        catch (Exception exception) when (exception is OperationCanceledException or IOException or ObjectDisposedException) { }
    }

    private CancellationTokenSource LobbyTimeout()
    {
        var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));
        return timeout;
    }

    private void DemandLobby()
    {
        ThrowIfDisposed();
        if (!tcp.Connected) throw new InvalidOperationException("No hay conexión con el servidor.");
        if (Volatile.Read(ref realtimeStarted) != 0) throw new InvalidOperationException("La sesión ya entró al mundo.");
    }

    private void DemandAuthenticatedLobby()
    {
        DemandLobby();
        if (!IsAuthenticated) throw new InvalidOperationException("La sesión no está autenticada.");
    }

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref disposed) != 0) throw new ObjectDisposedException(nameof(GameConnection));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        lifetime.Cancel();
        try
        {
            if (tcp.Connected) tcp.Client.Shutdown(SocketShutdown.Both);
        }
        catch (Exception exception) when (exception is SocketException or ObjectDisposedException) { }
        tcp.Dispose();
        writer.Dispose();
        lifetime.Dispose();
    }
}
