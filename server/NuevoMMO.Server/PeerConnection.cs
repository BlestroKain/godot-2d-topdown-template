using System.Net.Sockets;
using System.Threading.Channels;
using NuevoMMO.Contracts;
using NuevoMMO.Protocol;

namespace NuevoMMO.Server;

internal sealed class PeerConnection(TcpClient tcp, CancellationToken stopping) : IDisposable
{
    private readonly Channel<IMessage> outbound = Channel.CreateBounded<IMessage>(new BoundedChannelOptions(32)
    { SingleReader = true, FullMode = BoundedChannelFullMode.Wait });
    private readonly CancellationTokenSource lifetime = CancellationTokenSource.CreateLinkedTokenSource(stopping);
    public CancellationToken Token => lifetime.Token;
    public NetworkStream Stream => tcp.GetStream();
    public void Send(IMessage message)
    {
        // A missing delta breaks the baseline. Disconnect slow readers instead of dropping packets.
        if (!outbound.Writer.TryWrite(message)) lifetime.Cancel();
    }

    public async Task WriteLoopAsync()
    {
        try
        {
            await foreach (var message in outbound.Reader.ReadAllAsync(Token))
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Token);
                timeout.CancelAfter(TimeSpan.FromSeconds(3));
                await Frames.WriteAsync(Stream, message, timeout.Token);
            }
        }
        finally { lifetime.Cancel(); }
    }

    public void Dispose()
    {
        lifetime.Cancel(); outbound.Writer.TryComplete(); tcp.Dispose();
    }
}
