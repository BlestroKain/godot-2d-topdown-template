using System.Collections.Concurrent;
using Godot;
using NuevoMMO.Client;
using NuevoMMO.Core;
using NuevoMMO.Network;

namespace NuevoMMO.GodotClient;

public partial class NetworkBridge : Node
{
    [Signal] public delegate void ConnectionChangedEventHandler(string state);
    [Signal] public delegate void LobbyUpdatedEventHandler();
    [Signal] public delegate void WorldUpdatedEventHandler();
    [Signal] public delegate void PlayerStatsUpdatedEventHandler();
    [Signal] public delegate void CombatDebugUpdatedEventHandler();

    private readonly ConcurrentQueue<(GameConnection Connection, IPacket? Message, string? Error)> inbox = new();
    private GameConnection? connection;
    private int queued;
    private int overflow;
    private bool sending;
    private bool lobbyBusy;

    public ClientWorldState World { get; } = new();
    public string Status { get; private set; } = "Desconectado";
    public string LastNotice { get; private set; } = string.Empty;
    public CombatDebugPacket? LastCombat { get; private set; }
    public CharacterSummary[] Characters { get; private set; } = [];
    public bool InWorld => World.Predictor is not null;
    public bool Authenticated => connection?.IsAuthenticated == true;
    public bool LobbyBusy => lobbyBusy;
    public bool Connected => connection?.IsConnected == true;
    public static double Now => Time.GetTicksMsec() / 1000d;

    public void ConnectToServer(string host, int port, string name)
        => ConnectToServer(host, port, name, "development", true);

    /// <summary>Compatibilidad con el slice anterior.</summary>
    public async void ConnectToServer(string host, int port, string username, string password, bool registerAccount = false)
    {
        if (connection is not null) return;
        ResetLobbyState();
        SetStatus(registerAccount ? "Registrando…" : "Conectando…");
        var current = CreateConnection();
        try
        {
            lobbyBusy = true;
            await current.ConnectAsync(host, port, username, password, registerAccount);
        }
        catch (Exception exception) { Enqueue(current, null, exception.Message); }
        finally { if (connection == current) lobbyBusy = false; EmitSignal(SignalName.LobbyUpdated); }
    }

    public async void LoginLobby(string host, int port, string username, string password)
    {
        if (lobbyBusy || InWorld) return;
        try
        {
            lobbyBusy = true;
            var current = await EnsureOpenAsync(host, port);
            SetStatus("Autenticando…");
            await current.LoginAsync(username, password);
            Characters = [];
            LastNotice = string.Empty;
            SetStatus("Autenticado");
            EmitSignal(SignalName.LobbyUpdated);
        }
        catch (Exception exception) { FailLobby(exception.Message); }
        finally { lobbyBusy = false; EmitSignal(SignalName.LobbyUpdated); }
    }

    public async void RegisterLobby(string host, int port, string username, string password)
    {
        if (lobbyBusy || InWorld) return;
        try
        {
            lobbyBusy = true;
            var current = await EnsureOpenAsync(host, port);
            SetStatus("Registrando cuenta…");
            await current.RegisterAsync(username, password);
            SetStatus("Cuenta creada · autenticando…");
            await current.LoginAsync(username, password);
            Characters = [];
            LastNotice = string.Empty;
            SetStatus("Autenticado");
            EmitSignal(SignalName.LobbyUpdated);
        }
        catch (Exception exception) { FailLobby(exception.Message); }
        finally { lobbyBusy = false; EmitSignal(SignalName.LobbyUpdated); }
    }

    public async void RequestCharacters()
    {
        if (lobbyBusy || connection is null || !connection.IsAuthenticated || InWorld) return;
        var current = connection;
        try
        {
            lobbyBusy = true;
            SetStatus("Cargando personajes…");
            var list = await current.ListCharactersAsync();
            Characters = list.Characters;
            LastNotice = string.Empty;
            SetStatus("Personajes listos");
        }
        catch (Exception exception) { FailLobby(exception.Message); }
        finally { lobbyBusy = false; EmitSignal(SignalName.LobbyUpdated); }
    }

    public async void CreateCharacter(string name, DefinitionId traditionId = default)
    {
        if (lobbyBusy || connection is null || !connection.IsAuthenticated || InWorld) return;
        if (!CanonicalTraditions.IsValidAtCreate(traditionId))
        {
            FailLobby("Tradición inválida. El personaje nace Novicio.");
            EmitSignal(SignalName.LobbyUpdated);
            return;
        }
        var current = connection;
        try
        {
            lobbyBusy = true;
            SetStatus("Creando personaje…");
            var created = await current.CreateCharacterAsync(name, traditionId);
            Characters = Characters
                .Where(character => character.Id != created.Character.Id)
                .Append(created.Character)
                .ToArray();
            LastNotice = string.Empty;
            SetStatus("Personaje creado");
        }
        catch (Exception exception) { FailLobby(exception.Message); }
        finally { lobbyBusy = false; EmitSignal(SignalName.LobbyUpdated); }
    }

    public async void EnterWorld(CharacterId character)
    {
        if (lobbyBusy || connection is null || !connection.IsAuthenticated || InWorld) return;
        var current = connection;
        try
        {
            lobbyBusy = true;
            SetStatus("Entrando al mundo…");
            await current.EnterWorldAsync(character);
        }
        catch (Exception exception) { FailLobby(exception.Message); }
        finally { lobbyBusy = false; EmitSignal(SignalName.LobbyUpdated); }
    }

    private GameConnection CreateConnection()
    {
        var current = new GameConnection();
        connection = current;
        current.Message += message => Enqueue(current, message, null);
        current.Closed += error => Enqueue(current, null, error);
        return current;
    }

    private async Task<GameConnection> EnsureOpenAsync(string host, int port)
    {
        if (connection is { IsConnected: true } current) return current;
        connection?.Dispose();
        ResetLobbyState();
        current = CreateConnection();
        SetStatus("Conectando…");
        await current.OpenAsync(host, port);
        return current;
    }

    private void ResetLobbyState()
    {
        World.Clear();
        Characters = [];
        LastNotice = string.Empty;
        LastCombat = null;
    }

    private void FailLobby(string message)
    {
        LastNotice = message;
        SetStatus(message);
    }

    private void Enqueue(GameConnection current, IPacket? message, string? error)
    {
        if (Interlocked.Increment(ref queued) > 256)
        {
            Interlocked.Decrement(ref queued);
            Interlocked.Exchange(ref overflow, 1);
            return;
        }
        inbox.Enqueue((current, message, error));
    }

    public override void _Process(double delta)
    {
        if (Interlocked.Exchange(ref overflow, 0) != 0)
        {
            DisconnectFromServer();
            SetStatus("Cliente atrasado: vuelve a conectar.");
        }

        while (inbox.TryDequeue(out var item))
        {
            Interlocked.Decrement(ref queued);
            if (item.Connection != connection) continue;
            try
            {
                if (item.Error is not null)
                {
                    DisconnectFromServer();
                    SetStatus("Conexión cerrada: " + item.Error);
                    EmitSignal(SignalName.LobbyUpdated);
                    continue;
                }

                switch (item.Message)
                {
                    case RegisterResult registration when registration.Succeeded:
                        SetStatus("Cuenta creada");
                        break;
                    case LoginResult login when login.Succeeded:
                        SetStatus("Autenticado");
                        break;
                    case CharacterListResult list:
                        Characters = list.Characters;
                        SetStatus("Personajes listos");
                        EmitSignal(SignalName.LobbyUpdated);
                        break;
                    case CharacterCreated created:
                        Characters = Characters
                            .Where(character => character.Id != created.Character.Id)
                            .Append(created.Character)
                            .ToArray();
                        SetStatus("Personaje creado");
                        EmitSignal(SignalName.LobbyUpdated);
                        break;
                    case CharacterSelected:
                        SetStatus("Preparando mundo…");
                        break;
                    case MapLoadPacket map:
                        World.Start(map);
                        SetStatus("Conectado · entrando al mundo");
                        EmitSignal(SignalName.LobbyUpdated);
                        break;
                    case PlayerStatsPacket stats:
                        World.Apply(stats);
                        LastNotice = string.Empty;
                        EmitSignal(SignalName.PlayerStatsUpdated);
                        break;
                    case InventorySnapshotPacket inventory:
                        World.Apply(inventory);
                        LastNotice = string.Empty;
                        EmitSignal(SignalName.WorldUpdated);
                        break;
                    case CombatDebugPacket combat:
                        LastCombat = combat;
                        LastNotice = string.Empty;
                        World.Chat.Append(ChatChannel.Combat,
                            $"{combat.Attack} {combat.AppliedDamage} dmg · HP {combat.TargetHealth}/{combat.TargetMaxHealth}");
                        EmitSignal(SignalName.CombatDebugUpdated);
                        break;
                    case EntityStatePacket snapshot:
                        World.Apply(snapshot, Now);
                        SetStatus("En el mundo");
                        EmitSignal(SignalName.WorldUpdated);
                        EmitSignal(SignalName.LobbyUpdated);
                        break;
                    case ErrorPacket error when error.Fatal:
                        DisconnectFromServer();
                        SetStatus(error.Message);
                        EmitSignal(SignalName.LobbyUpdated);
                        break;
                    case ErrorPacket error:
                        LastNotice = error.Message;
                        World.Chat.Append(ChatChannel.System, error.Message);
                        EmitSignal(SignalName.PlayerStatsUpdated);
                        EmitSignal(SignalName.CombatDebugUpdated);
                        EmitSignal(SignalName.LobbyUpdated);
                        break;
                }
            }
            catch (Exception exception)
            {
                DisconnectFromServer();
                SetStatus("Estado rechazado: " + exception.Message);
                EmitSignal(SignalName.LobbyUpdated);
            }
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

    public async void AllocateAttribute(PrimaryAttributeId attribute, int increments = 1)
    {
        if (!InWorld || connection is null) return;
        var current = connection;
        try { await current.SendAsync(new AllocateAttributeRequest(attribute, increments)); }
        catch (Exception exception) { Enqueue(current, null, exception.Message); }
    }

    public async void DevelopmentAttack(EntityId target, DevelopmentAttackKind attack)
    {
        if (!InWorld || connection is null || target.Value <= 0) return;
        var current = connection;
        try { await current.SendAsync(new DevelopmentAttackRequest(target, attack)); }
        catch (Exception exception) { Enqueue(current, null, exception.Message); }
    }

    public async void BasicAttack(EntityId target)
    {
        if (!InWorld || connection is null || target.Value <= 0) return;
        var current = connection;
        try { await current.SendAsync(new BasicAttackRequest(target)); }
        catch (Exception exception) { Enqueue(current, null, exception.Message); }
    }

    public async void UseTechnique(DefinitionId techniqueId, EntityId target, Vector2Data point = default)
    {
        if (!InWorld || connection is null || techniqueId.IsEmpty) return;
        var current = connection;
        try { await current.SendAsync(new UseTechniqueRequest(techniqueId, target, point)); }
        catch (Exception exception) { Enqueue(current, null, exception.Message); }
    }

    public async void Interact(EntityId target)
    {
        if (!InWorld || connection is null) return;
        var current = connection;
        try { await current.SendAsync(new InteractRequest(target)); }
        catch (Exception exception) { Enqueue(current, null, exception.Message); }
    }

    public async void SetTarget(EntityId target)
    {
        if (!InWorld || connection is null) return;
        var current = connection;
        try { await current.SendAsync(new SetTargetRequest(target)); }
        catch (Exception exception) { Enqueue(current, null, exception.Message); }
    }

    public async void EquipItem(ItemInstanceId itemId)
    {
        if (!InWorld || connection is null || itemId.Value == Guid.Empty) return;
        var current = connection;
        try { await current.SendAsync(new EquipItemRequest(itemId)); }
        catch (Exception exception) { Enqueue(current, null, exception.Message); }
    }

    public async void UnequipItem(ItemInstanceId itemId)
    {
        if (!InWorld || connection is null || itemId.Value == Guid.Empty) return;
        var current = connection;
        try { await current.SendAsync(new UnequipItemRequest(itemId)); }
        catch (Exception exception) { Enqueue(current, null, exception.Message); }
    }

    public async void MoveInventoryItem(ItemInstanceId itemId, int targetIndex)
    {
        if (!InWorld || connection is null || itemId.Value == Guid.Empty || targetIndex < 0) return;
        var current = connection;
        try { await current.SendAsync(new MoveInventoryItemRequest(itemId, targetIndex)); }
        catch (Exception exception) { Enqueue(current, null, exception.Message); }
    }

    public void DisconnectFromServer()
    {
        connection?.Dispose();
        connection = null;
        sending = false;
        lobbyBusy = false;
        ResetLobbyState();
        SetStatus("Desconectado");
        EmitSignal(SignalName.LobbyUpdated);
    }

    private void SetStatus(string value)
    {
        if (Status == value) return;
        Status = value;
        EmitSignal(SignalName.ConnectionChanged, value);
    }

    public override void _ExitTree() => connection?.Dispose();
}
