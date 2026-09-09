using System.Net.Sockets;
using NuevoMMO.Contracts;
using NuevoMMO.Protocol;

namespace NuevoMMO.Client;

// Adaptation of GodotMMO's own GameConnection, without gameplay dependencies or local persistence.
public sealed class GameConnection : IDisposable
{
    private readonly TcpClient tcp = new() { NoDelay = true };
    private readonly SemaphoreSlim writer = new(1);
    private readonly CancellationTokenSource lifetime = new();
    private int disposed;
    public event Action<IMessage>? Message;
    public event Action<string>? Closed;

    public async Task ConnectAsync(string host, int port, string name)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        await tcp.ConnectAsync(host, port, timeout.Token);
        await Frames.WriteAsync(tcp.GetStream(), new HandshakeRequest(name), timeout.Token);
        var message = await Frames.ReadAsync(tcp.GetStream(), timeout.Token);
        if (message is not HandshakeAccepted) throw new IOException((message as HandshakeRejected)?.Reason ?? "Handshake inválido.");
        Message?.Invoke(message);
        _ = ReceiveAsync();
    }

    public async Task SendAsync(IMessage message)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        timeout.CancelAfter(TimeSpan.FromSeconds(3));
        await writer.WaitAsync(timeout.Token);
        try { await Frames.WriteAsync(tcp.GetStream(), message, timeout.Token); }
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
                var message = await Frames.ReadAsync(tcp.GetStream(), timeout.Token);
                if (message is not (WorldSnapshot or Pong)) throw new InvalidDataException("Mensaje servidor inesperado.");
                Message?.Invoke(message);
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
        lifetime.Cancel(); tcp.Dispose();
        // Pending operations own their wait handles until they finish.
    }
}
