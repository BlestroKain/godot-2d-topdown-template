using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NuevoMMO.Application;
using NuevoMMO.Contracts;
using NuevoMMO.Protocol;

namespace NuevoMMO.Server;

public sealed class ServerHost(WorldRuntime world, int port = 7777)
{
    private readonly TcpListener listener = new(IPAddress.Loopback, port);
    private readonly ConcurrentDictionary<ConnectionId, PeerConnection> peers = [];
    private readonly ConcurrentDictionary<ConnectionId, Task> connections = [];
    private readonly SemaphoreSlim slots = new(32); // Includes incomplete handshakes.
    public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;
    public int PlayerCount => world.PlayerCount;
    public double LastTickMilliseconds { get; private set; }

    public async Task RunAsync(CancellationToken stopping)
    {
        listener.Start();
        Console.WriteLine($"Servidor Development escuchando 127.0.0.1:{Port}; tick={world.TickMilliseconds}ms.");
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
                _ = task.ContinueWith(_ => { connections.TryRemove(id, out var ignored); }, TaskScheduler.Default);
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        finally
        {
            lifetime.Cancel(); listener.Stop();
            foreach (var peer in peers.Values) peer.Dispose();
            await Task.WhenAll(connections.Values);
            try { await simulation; } catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
        }
    }

    private async Task SimulateAsync(CancellationToken stopping)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(world.TickMilliseconds));
        while (await timer.WaitForNextTickAsync(stopping))
        {
            var started = Stopwatch.GetTimestamp();
            foreach (var pair in world.Step()) if (peers.TryGetValue(pair.Key, out var peer)) peer.Send(pair.Value);
            LastTickMilliseconds = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        }
    }

    private async Task ServeAsync(ConnectionId id, TcpClient tcp, CancellationToken stopping)
    {
        using var peer = new PeerConnection(tcp, stopping);
        Task? writer = null;
        var state = SessionState.Connected;
        try
        {
            state = SessionState.Handshaking;
            using var handshakeTimeout = CancellationTokenSource.CreateLinkedTokenSource(peer.Token);
            handshakeTimeout.CancelAfter(TimeSpan.FromSeconds(5));
            if (await Frames.ReadAsync(peer.Stream, handshakeTimeout.Token) is not HandshakeRequest hello)
                throw new InvalidDataException("Se requiere handshake.");
            state = SessionState.AuthenticatedPlaceholder; // Development only; not account authentication.
            var accepted = world.Join(id, hello.Name, activate: false);
            // Queue hello before making this peer visible to the tick publisher.
            peer.Send(accepted);
            peers[id] = peer;
            writer = peer.WriteLoopAsync();
            world.Activate(id);
            state = SessionState.InWorld;
            var rateStart = Stopwatch.GetTimestamp();
            var received = 0;
            while (!peer.Token.IsCancellationRequested)
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(peer.Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(10));
                var message = await Frames.ReadAsync(peer.Stream, timeout.Token);
                if (Stopwatch.GetElapsedTime(rateStart).TotalSeconds >= 1) { received = 0; rateStart = Stopwatch.GetTimestamp(); }
                if (++received > 100) throw new InvalidDataException("Límite de mensajes excedido.");
                if (message is Ping ping) peer.Send(new Pong(ping.Nonce));
                else world.Dispatch(id, message);
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or SocketException or OperationCanceledException or ArgumentException or InvalidOperationException)
        {
            if (!stopping.IsCancellationRequested && exception is not (EndOfStreamException or OperationCanceledException))
                Console.WriteLine($"Conexión cerrada ({state}): {exception.Message}");
            if (writer is null && !peer.Token.IsCancellationRequested)
            {
                try
                {
                    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                    await Frames.WriteAsync(peer.Stream, new HandshakeRejected("Conexión rechazada: protocolo, nombre o capacidad inválidos."), timeout.Token);
                }
                catch (Exception writeError) when (writeError is IOException or SocketException or OperationCanceledException or ObjectDisposedException) { }
            }
        }
        finally
        {
            peers.TryRemove(id, out _); world.Disconnect(id); peer.Dispose();
            if (writer is not null)
            {
                try { await writer; }
                catch (Exception exception) when (exception is IOException or SocketException or OperationCanceledException or ObjectDisposedException) { }
            }
            slots.Release();
        }
    }
}
