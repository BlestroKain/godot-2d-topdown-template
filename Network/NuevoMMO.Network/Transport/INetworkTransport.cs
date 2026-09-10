namespace NuevoMMO.Network;

public interface INetworkTransport : IDisposable
{
    bool IsRunning { get; }
    void StartServer(TransportEndpoint endpoint, int maxPeers);
    void StartClient();
    void Connect(TransportEndpoint endpoint);
    IReadOnlyList<TransportEvent> Poll();
    void Send(TransportPeerId peer, byte channel, DeliveryMode delivery, ReadOnlyMemory<byte> payload);
    void Disconnect(TransportPeerId peer, uint reason = 0);
}
