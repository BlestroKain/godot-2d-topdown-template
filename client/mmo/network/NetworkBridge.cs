using System.Collections.Concurrent;
using Godot;
using NuevoMMO.Client;
using NuevoMMO.Contracts;

namespace NuevoMMO.GodotClient;

public partial class NetworkBridge : Node
{
    [Signal] public delegate void ConnectionChangedEventHandler(string state);
    [Signal] public delegate void WorldUpdatedEventHandler();
    private readonly ConcurrentQueue<(GameConnection Connection, IMessage? Message, string? Error)> inbox = new();
    private GameConnection? connection;
    private int queued;
    private int overflow;
    private bool sending;
    public ClientWorldState World { get; } = new();
    public string Status { get; private set; } = "Desconectado";
    public bool InWorld => World.Predictor is not null;
    public static double Now => Time.GetTicksMsec() / 1000d;

    public async void ConnectToServer(string host, int port, string name)
    {
        if (connection is not null) return;
        World.Clear(); SetStatus("Conectando…");
        var current = new GameConnection(); connection = current;
        current.Message += message => Enqueue(current, message, null);
        current.Closed += error => Enqueue(current, null, error);
        try { await current.ConnectAsync(host, port, name); }
        catch (Exception exception) { Enqueue(current, null, exception.Message); }
    }

    private void Enqueue(GameConnection current, IMessage? message, string? error)
    {
        if (Interlocked.Increment(ref queued) > 256)
        {
            Interlocked.Decrement(ref queued); Interlocked.Exchange(ref overflow, 1); return;
        }
        inbox.Enqueue((current, message, error));
    }

    public override void _Process(double delta)
    {
        if (Interlocked.Exchange(ref overflow, 0) != 0) { DisconnectFromServer(); SetStatus("Cliente atrasado: vuelve a conectar."); }
        while (inbox.TryDequeue(out var item))
        {
            Interlocked.Decrement(ref queued);
            if (item.Connection != connection) continue;
            try
            {
                if (item.Error is not null) { DisconnectFromServer(); SetStatus("Conexión cerrada: " + item.Error); continue; }
                switch (item.Message)
                {
                    case HandshakeAccepted accepted: World.Start(accepted); SetStatus("Conectado · entrando al mundo"); break;
                    case WorldSnapshot snapshot:
                        World.Apply(snapshot, Now); SetStatus("En el mundo"); EmitSignal(SignalName.WorldUpdated); break;
                }
            }
            catch (Exception exception) { DisconnectFromServer(); SetStatus("Estado rechazado: " + exception.Message); }
        }
    }

    public async void SubmitInput(float x, float y)
    {
        if (!InWorld || connection is null || sending) return;
        var current = connection;
        sending = true;
        try { await current.SendAsync(World.Predictor!.Predict(x, y)); }
        catch (Exception exception) { Enqueue(current, null, exception.Message); }
        finally { if (connection == current) sending = false; }
    }

    public void DisconnectFromServer()
    {
        connection?.Dispose(); connection = null; sending = false; World.Clear(); SetStatus("Desconectado");
    }

    private void SetStatus(string value)
    {
        if (Status == value) return;
        Status = value; EmitSignal(SignalName.ConnectionChanged, value);
    }
    public override void _ExitTree() => connection?.Dispose();
}
