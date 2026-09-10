using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NuevoMMO.Core;
using NuevoMMO.Network;
using NuevoMMO.Server.Configuration;
using NuevoMMO.Server.Database;
using NuevoMMO.Server.NetworkHandlers;
using NuevoMMO.Server.Telemetry;
using NuevoMMO.Server.World;

namespace NuevoMMO.Server;

internal sealed class PeerConnection(TcpClient tcp, CancellationToken stopping, int writeTimeoutSeconds) : IDisposable
{
    private readonly System.Threading.Channels.Channel<IPacket> outbound = System.Threading.Channels.Channel.CreateBounded<IPacket>(
        new System.Threading.Channels.BoundedChannelOptions(32) { SingleReader = true, FullMode = System.Threading.Channels.BoundedChannelFullMode.Wait });
    private readonly CancellationTokenSource lifetime = CancellationTokenSource.CreateLinkedTokenSource(stopping);
    public CancellationToken Token => lifetime.Token;
    public NetworkStream Stream => tcp.GetStream();
    public void Send(IPacket packet)
    {
        if (!outbound.Writer.TryWrite(packet)) lifetime.Cancel();
    }

    public async Task WriteLoopAsync()
    {
        try
        {
            await foreach (var packet in outbound.Reader.ReadAllAsync(Token))
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(writeTimeoutSeconds));
                await TcpPacketFraming.WriteAsync(Stream, packet, timeout.Token);
            }
        }
        finally { lifetime.Cancel(); }
    }

    public void Dispose()
    {
        lifetime.Cancel();
        outbound.Writer.TryComplete();
        tcp.Dispose();
    }
}

public sealed class ServerHost
{
    private readonly WorldRuntime world;
    private readonly PersistenceService persistence;
    private readonly PacketDispatcher<ServerPacketContext> dispatcher;
    private readonly TcpListener listener;
    private readonly ServerConfiguration configuration;
    private readonly ConcurrentDictionary<ConnectionId, PeerConnection> peers = [];
    private readonly ConcurrentDictionary<ConnectionId, Task> connections = [];
    private readonly SemaphoreSlim slots;
    private readonly ServerMetrics metrics = new();
    public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
    public int PlayerCount => world.PlayerCount;
    public ServerMetrics Metrics => metrics;
    public double LastTickMilliseconds { get; private set; }

    public ServerHost(WorldRuntime world, PersistenceService persistence, PacketDispatcher<ServerPacketContext> dispatcher, int port = 7777)
        : this(world, persistence, dispatcher, ServerConfiguration.Development(port))
    {
    }

    public ServerHost(
        WorldRuntime world,
        PersistenceService persistence,
        PacketDispatcher<ServerPacketContext> dispatcher,
        ServerConfiguration configuration)
    {
        this.world = world;
        this.persistence = persistence;
        this.dispatcher = dispatcher;
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        configuration.Validate();

        if (!IPAddress.TryParse(configuration.Host, out var address))
            throw new ArgumentException($"Host debe ser una dirección IP válida: {configuration.Host}.", nameof(configuration));

        listener = new(address, configuration.Port);
        slots = new(configuration.MaxConnections);
    }

    public async Task RunAsync(CancellationToken stopping)
    {
        listener.Start();
        Console.WriteLine($"Servidor {configuration.Environment} escuchando {configuration.Host}:{Port}; tick={world.TickMilliseconds}ms; maxPlayers={configuration.MaxPlayers}; maxConnections={configuration.MaxConnections}.");
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(stopping);
        var simulation = SimulateAsync(lifetime.Token);
        try
        {
            while (!lifetime.IsCancellationRequested)
            {
                var tcp = await listener.AcceptTcpClientAsync(lifetime.Token);
                if (!slots.Wait(0)) { tcp.Dispose(); continue; }
                tcp.NoDelay = true;
                var id = new ConnectionId(Guid.NewGuid());
                var task = ServeAsync(id, tcp, lifetime.Token);
                connections[id] = task;
                _ = task.ContinueWith(_ => connections.TryRemove(id, out var ignored), TaskScheduler.Default);
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        finally
        {
            lifetime.Cancel();
            listener.Stop();
            foreach (var peer in peers.Values) peer.Dispose();
            await Task.WhenAll(connections.Values);
            try { await simulation; } catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
            await persistence.SaveDirtyAsync(world.DirtyPlayers(), CancellationToken.None);
        }
    }

    private async Task SimulateAsync(CancellationToken stopping)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(world.TickMilliseconds));
        var autosave = 0;
        while (await timer.WaitForNextTickAsync(stopping))
        {
            var started = Stopwatch.GetTimestamp();
            foreach (var pair in world.Step())
                if (peers.TryGetValue(pair.Key, out var peer)) peer.Send(pair.Value);
            LastTickMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            metrics.ObserveTick(LastTickMilliseconds, world.PlayerCount, world.EntityCount, 1);
            if (++autosave % configuration.AutosaveIntervalTicks == 0)
                await persistence.SaveDirtyAsync(world.DirtyPlayers(), stopping);
        }
    }

    private async Task ServeAsync(ConnectionId id, TcpClient tcp, CancellationToken stopping)
    {
        using var peer = new PeerConnection(tcp, stopping, configuration.WriteTimeoutSeconds);
        Task? writer = null;
        PlayerSession? session = null;
        try
        {
            session = world.AddConnection(id);
            using var handshakeTimeout = CancellationTokenSource.CreateLinkedTokenSource(peer.Token);
            handshakeTimeout.CancelAfter(TimeSpan.FromSeconds(configuration.HandshakeTimeoutSeconds));
            var first = await TcpPacketFraming.ReadAsync(peer.Stream, handshakeTimeout.Token);
            if (first is not ConnectRequest) throw new InvalidDataException("Se requiere ConnectRequest.");
            var context = new ServerPacketContext { Connection = id, Session = session, Send = peer.Send };
            await dispatcher.DispatchAsync(context, first, handshakeTimeout.Token);
            peers[id] = peer;
            writer = peer.WriteLoopAsync();
            var rate = new RateLimiter(configuration.MaxMessagesPerWindow);
            while (!peer.Token.IsCancellationRequested)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(peer.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(configuration.ReadTimeoutSeconds));
                var packet = await TcpPacketFraming.ReadAsync(peer.Stream, timeout.Token);
                metrics.PacketIn();
                if (!rate.TryAdmit()) throw new InvalidDataException("Límite de mensajes excedido.");
                await dispatcher.DispatchAsync(new ServerPacketContext { Connection = id, Session = session, Send = peer.Send }, packet, timeout.Token);
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or SocketException or OperationCanceledException or ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            if (!stopping.IsCancellationRequested && exception is not (EndOfStreamException or OperationCanceledException))
                Console.WriteLine($"Conexión cerrada: {exception.Message}");
            if (writer is null && !peer.Token.IsCancellationRequested)
            {
                try
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                    await TcpPacketFraming.WriteAsync(peer.Stream, new ErrorPacket("handshake", "Conexión rechazada: protocolo, nombre o capacidad inválidos.", true), timeout.Token);
                }
                catch (Exception writeError) when (writeError is IOException or SocketException or OperationCanceledException or ObjectDisposedException) { }
            }
        }
        finally
        {
            peers.TryRemove(id, out _);
            var player = world.Disconnect(id);
            if (player is not null) await persistence.SaveCharacterAsync(player, CancellationToken.None);
            peer.Dispose();
            if (writer is not null)
            {
                try { await writer; }
                catch (Exception exception) when (exception is IOException or SocketException or OperationCanceledException or ObjectDisposedException) { }
            }
            slots.Release();
        }
    }
}
