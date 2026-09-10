using System.Collections.Concurrent;
using ENet;

namespace NuevoMMO.Network;

public sealed class EnetTransport : INetworkTransport
{
    private abstract record Command;
    private sealed record ConnectCommand(TransportEndpoint Endpoint) : Command;
    private sealed record SendCommand(TransportPeerId Peer, byte Channel, DeliveryMode Delivery, byte[] Payload) : Command;
    private sealed record DisconnectCommand(TransportPeerId Peer, uint Reason) : Command;

    private static readonly object RuntimeGate = new();
    private static int runtimeUsers;
    private readonly ConcurrentQueue<Command> commands = new();
    private readonly ConcurrentQueue<TransportEvent> events = new();
    private readonly CancellationTokenSource lifetime = new();
    private Thread? ioThread;
    private TransportEndpoint? listenEndpoint;
    private int maxPeers;
    private int running;
    private bool server;

    public bool IsRunning => Volatile.Read(ref running) == 1;

    public void StartServer(TransportEndpoint endpoint, int peerLimit)
    {
        if (peerLimit is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(peerLimit));
        listenEndpoint = endpoint; maxPeers = peerLimit; server = true; Start();
    }

    public void StartClient() { maxPeers = 1; server = false; Start(); }

    public void Connect(TransportEndpoint endpoint)
    {
        EnsureRunning();
        if (server) throw new InvalidOperationException("Un transporte servidor no inicia conexiones.");
        commands.Enqueue(new ConnectCommand(endpoint));
    }

    public IReadOnlyList<TransportEvent> Poll()
    {
        var result = new List<TransportEvent>();
        while (events.TryDequeue(out var value)) result.Add(value);
        return result;
    }

    public void Send(TransportPeerId peer, byte channel, DeliveryMode delivery, ReadOnlyMemory<byte> payload)
    {
        EnsureRunning();
        if (channel >= NetworkChannels.Count || payload.Length is < PacketHeader.Size or > PacketCodec.MaxPacketBytes)
            throw new ArgumentException("Envío inválido.");
        commands.Enqueue(new SendCommand(peer, channel, delivery, payload.ToArray()));
    }

    public void Disconnect(TransportPeerId peer, uint reason = 0)
    {
        if (IsRunning) commands.Enqueue(new DisconnectCommand(peer, reason));
    }

    private void Start()
    {
        if (Interlocked.CompareExchange(ref running, 1, 0) != 0) throw new InvalidOperationException("Transporte ya iniciado.");
        ioThread = new Thread(IoLoop) { IsBackground = true, Name = server ? "NuevoMMO ENet Server" : "NuevoMMO ENet Client" };
        ioThread.Start();
    }

    private void IoLoop()
    {
        var peers = new Dictionary<TransportPeerId, Peer>();
        Host? host = null;
        try
        {
            AcquireRuntime();
            host = new Host();
            if (server)
            {
                var address = Address(listenEndpoint!);
                host.Create(address, maxPeers, NetworkChannels.Count);
            }
            else host.Create(maxPeers, NetworkChannels.Count);

            while (!lifetime.IsCancellationRequested)
            {
                while (commands.TryDequeue(out var command)) Execute(host, peers, command);
                if (host.Service(5, out var networkEvent) > 0) Handle(peers, networkEvent);
                while (host.CheckEvents(out networkEvent) > 0) Handle(peers, networkEvent);
            }
            foreach (var peer in peers.Values) if (peer.IsSet) peer.DisconnectNow(0);
            host.Flush();
        }
        catch (Exception exception)
        {
            events.Enqueue(new(TransportEventKind.Error, default, 0, ReadOnlyMemory<byte>.Empty, exception.Message));
        }
        finally
        {
            host?.Dispose(); ReleaseRuntime(); Interlocked.Exchange(ref running, 0);
        }
    }

    private void Execute(Host host, Dictionary<TransportPeerId, Peer> peers, Command command)
    {
        switch (command)
        {
            case ConnectCommand value:
                var peer = host.Connect(Address(value.Endpoint), NetworkChannels.Count);
                peers[Id(peer)] = peer;
                break;
            case SendCommand value when peers.TryGetValue(value.Peer, out var target) && target.IsSet:
                var packet = default(Packet);
                packet.Create(value.Payload, value.Delivery switch
                {
                    DeliveryMode.Reliable => PacketFlags.Reliable,
                    DeliveryMode.Unreliable => PacketFlags.Unsequenced,
                    DeliveryMode.UnreliableSequenced => PacketFlags.None,
                    _ => throw new ArgumentOutOfRangeException()
                });
                if (!target.Send(value.Channel, ref packet)) packet.Dispose();
                break;
            case DisconnectCommand value when peers.TryGetValue(value.Peer, out var target): target.Disconnect(value.Reason); break;
        }
    }

    private void Handle(Dictionary<TransportPeerId, Peer> peers, Event networkEvent)
    {
        var id = Id(networkEvent.Peer);
        switch (networkEvent.Type)
        {
            case EventType.Connect: peers[id] = networkEvent.Peer; events.Enqueue(new(TransportEventKind.Connected, id, 0, ReadOnlyMemory<byte>.Empty)); break;
            case EventType.Receive:
                var data = new byte[networkEvent.Packet.Length]; networkEvent.Packet.CopyTo(data); networkEvent.Packet.Dispose();
                events.Enqueue(new(TransportEventKind.Packet, id, networkEvent.ChannelID, data)); break;
            case EventType.Disconnect:
            case EventType.Timeout: peers.Remove(id); events.Enqueue(new(TransportEventKind.Disconnected, id, 0, ReadOnlyMemory<byte>.Empty)); break;
        }
    }

    private static Address Address(TransportEndpoint endpoint)
    {
        if (endpoint.Port == 0 || string.IsNullOrWhiteSpace(endpoint.Host)) throw new ArgumentException("Endpoint inválido.");
        var address = new Address { Port = endpoint.Port };
        if (!address.SetHost(endpoint.Host)) throw new ArgumentException("Host ENet inválido.");
        return address;
    }
    private static TransportPeerId Id(Peer peer) => new(peer.ID + 1L);
    private void EnsureRunning() { if (!IsRunning) throw new InvalidOperationException("Transporte no iniciado."); }
    private static void AcquireRuntime() { lock (RuntimeGate) { if (runtimeUsers++ == 0 && !Library.Initialize()) { runtimeUsers--; throw new InvalidOperationException("ENet no pudo inicializar."); } } }
    private static void ReleaseRuntime() { lock (RuntimeGate) { if (runtimeUsers > 0 && --runtimeUsers == 0) Library.Deinitialize(); } }

    public void Dispose()
    {
        lifetime.Cancel();
        if (ioThread is not null && ioThread != Thread.CurrentThread && !ioThread.Join(TimeSpan.FromSeconds(5)))
            throw new TimeoutException("El hilo ENet no cerró a tiempo.");
        lifetime.Dispose();
    }
}
