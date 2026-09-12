using Godot;
using NuevoMMO.Client;
using NuevoMMO.Core;
using NuevoMMO.Network;
using NuevoMMO.GodotClient.UI;

namespace NuevoMMO.GodotClient;

/// <summary>
/// Production MMO HUD shell. Visual layout lives in .tscn scenes; this script binds replicated game state.
/// System-specific window behavior is delegated to UiSystemBinder so this composition root stays small.
/// </summary>
public partial class GameHud : CanvasLayer
{
    private Label selfVitals = null!, targetVitals = null!, chatLog = null!, inventorySummary = null!, inventoryCapacity = null!, characterStats = null!, levelLabel = null!;
    private ProgressBar healthBar = null!, manaBar = null!;
    private UiSlotGrid hotbarGrid = null!;
    private MmoWindow characterRoot = null!, inventoryRoot = null!, escapeRoot = null!, questRoot = null!, techniquesRoot = null!;
    private readonly Dictionary<string, MmoWindow> windows = new(StringComparer.OrdinalIgnoreCase);
    private string lastChatFingerprint = string.Empty;
    private string lastInventoryProjectionFingerprint = string.Empty;
    private long lastInventoryRevision = -1;
    private readonly HashSet<ItemInstanceId> pendingInventoryItems = [];
    private Node inventoryProjection = null!;
    private Node inventoryGrid = null!;
    private UiSystemBinder systemBinder = null!;

    public override void _Ready()
    {
        Layer = 10;

        selfVitals = GetNode<Label>("Root/CombatBar/HBox/Vitals/SelfVitals");
        healthBar = GetNode<ProgressBar>("Root/CombatBar/HBox/Vitals/HealthBar");
        manaBar = GetNode<ProgressBar>("Root/CombatBar/HBox/Vitals/ManaBar");
        levelLabel = GetNode<Label>("Root/CombatBar/HBox/PortraitStack/Level");
        inventoryProjection = GetNode<Node>("ServerInventoryProjection");
        hotbarGrid = GetNode<UiSlotGrid>("Root/CombatBar/HBox/Hotbar");
        targetVitals = GetNode<Label>("Root/TargetBox/TargetVitals");
        chatLog = GetNode<Label>("Root/ChatPanel/VBox/ChatLog");

        characterRoot = GetNode<MmoWindow>("Root/CharacterWindow");
        inventoryRoot = GetNode<MmoWindow>("Root/InventoryWindow");
        questRoot = GetNode<MmoWindow>("Root/QuestJournal");
        techniquesRoot = GetNode<MmoWindow>("Root/TechniquesWindow");
        escapeRoot = GetNode<MmoWindow>("Root/EscapeWindow");

        characterStats = characterRoot.GetNode<Label>("Margin/VBox/Body/StatsPanel/StatsVBox/CharacterStats");
        inventorySummary = inventoryRoot.GetNode<Label>("Margin/VBox/InventorySummary");
        inventoryCapacity = inventoryRoot.GetNode<Label>("Margin/VBox/HeaderDrag/Weight");
        inventoryGrid = inventoryRoot.GetNode<Node>("Margin/VBox/GridScroll/SlotGrid");
        inventoryGrid.Set("inventory", inventoryProjection);
        inventoryGrid.Connect("server_item_activated", Callable.From<string>(OnInventoryItemActivated));
        inventoryGrid.Connect("server_item_move_requested", Callable.From<string, int>(OnInventoryItemMoveRequested));

        RegisterWindow("character", characterRoot);
        RegisterWindow("inventory", inventoryRoot);
        RegisterWindow("quests", questRoot);
        RegisterWindow("techniques", techniquesRoot);
        RegisterWindow("shop", GetNode<MmoWindow>("Root/ShopWindow"));
        RegisterWindow("bank", GetNode<MmoWindow>("Root/BankWindow"));
        RegisterWindow("mail", GetNode<MmoWindow>("Root/MailWindow"));
        RegisterWindow("community", GetNode<MmoWindow>("Root/CommunityWindow"));
        RegisterWindow("dialogue", GetNode<MmoWindow>("Root/DialogueWindow"));
        RegisterWindow("profession", GetNode<MmoWindow>("Root/ProfessionWindow"));
        RegisterWindow("escape", escapeRoot);

        GetNode<Button>("Root/CombatBar/HBox/Quick/CharacterButton").Pressed += ToggleCharacter;
        GetNode<Button>("Root/CombatBar/HBox/Quick/InventoryButton").Pressed += ToggleInventory;
        GetNode<Button>("Root/CombatBar/HBox/Quick/QuestButton").Pressed += ToggleQuests;
        GetNode<Button>("Root/CombatBar/HBox/Quick/TechniquesButton").Pressed += ToggleTechniques;
        GetNode<Button>("Root/QuestTracker/VBox/Header/JournalButton").Pressed += ToggleQuests;

        WireEscapeMenu();
        systemBinder = new UiSystemBinder(this);
        Visible = false;
    }

    private void WireEscapeMenu()
    {
        escapeRoot.GetNode<Button>("Margin/VBox/Resume").Pressed += escapeRoot.Close;
        escapeRoot.GetNode<Button>("Margin/VBox/Settings").Pressed += OpenSharedSettings;
        escapeRoot.GetNode<Button>("Margin/VBox/Controls").Pressed += OpenSharedSettings;
        escapeRoot.GetNode<Button>("Margin/VBox/Character").Pressed += () =>
        {
            escapeRoot.Close();
            characterRoot.Open();
        };
        escapeRoot.GetNode<Button>("Margin/VBox/Logout").Pressed += () =>
        {
            escapeRoot.Close();
            if (GetParent() is MmoGame game)
                game.Network.DisconnectFromServer();
        };
    }

    private void OpenSharedSettings()
    {
        escapeRoot.Close();
        GetParent()?.GetNodeOrNull<FrontendFlowController>("FrontendRoot")?.OpenInGameSettings();
    }

    public void Present(NetworkBridge network)
    {
        ArgumentNullException.ThrowIfNull(network);
        var world = network.World;
        Visible = world.Flow == GameFlowState.InWorld;
        if (!Visible)
        {
            if (lastInventoryProjectionFingerprint.Length > 0)
            {
                inventoryProjection.Call("reset_server_projection");
                lastInventoryProjectionFingerprint = string.Empty;
                lastInventoryRevision = -1;
                pendingInventoryItems.Clear();
            }
            return;
        }

        var stats = world.Local.Stats;
        if (stats is null)
        {
            selfVitals.Text = "Jugador · HP --/-- · PM --/--";
            healthBar.MaxValue = 1;
            healthBar.Value = 0;
            manaBar.MaxValue = 1;
            manaBar.Value = 0;
            levelLabel.Text = "Lv --";
        }
        else
        {
            selfVitals.Text = $"{world.Local.Entity?.DisplayName ?? "Jugador"} · HP {stats.Health}/{stats.MaxHealth} · PM {stats.Mana}/{stats.MaxMana}";
            healthBar.MaxValue = Math.Max(1, stats.MaxHealth);
            healthBar.Value = Math.Clamp(stats.Health, 0, stats.MaxHealth);
            manaBar.MaxValue = Math.Max(1, stats.MaxMana);
            manaBar.Value = Math.Clamp(stats.Mana, 0, stats.MaxMana);
            levelLabel.Text = $"Lv {stats.Level}";
        }

        targetVitals.Text = world.Local.Target.HasTarget
            ? $"{world.Local.Target.Kind} · {world.Local.Target.DisplayName}"
            : "Sin objetivo";

        var chat = string.Join('\n', world.Chat.Messages.TakeLast(7).Select(static message => $"[{message.Channel}] {message.Text}"));
        if (chat != lastChatFingerprint)
        {
            chatLog.Text = string.IsNullOrEmpty(chat) ? "…" : chat;
            lastChatFingerprint = chat;
        }

        RefreshHotbar(world);
        RefreshInventory(world);

        characterStats.Text = stats is null
            ? "Stats esperando servidor…"
            : $"Lv {stats.Level}  ·  XP {stats.Experience}/{stats.ExperienceToNextLevel}\n\n" +
              $"STR   {stats.Strength.Effective}\nINT    {stats.Intelligence.Effective}\nAGI    {stats.Agility.Effective}\n" +
              $"SPI    {stats.Spirit.Effective}\nVIT    {stats.Vitality.Effective}\n\n" +
              $"Puntos disponibles   {stats.AvailableAttributePoints}\nHP   {stats.Health}/{stats.MaxHealth}\nPM   {stats.Mana}/{stats.MaxMana}";

        systemBinder.Present(network);
    }

    public void ToggleCharacter() => characterRoot.Toggle();
    public void ToggleInventory() => inventoryRoot.Toggle();
    public void ToggleEscape() => escapeRoot.Toggle();
    public void ToggleQuests() => questRoot.Toggle();
    public void ToggleTechniques() => techniquesRoot.Toggle();

    /// <summary>Entry point for interaction systems (NPC shop, bank, mail, profession stations, dialogue, etc.).</summary>
    public bool OpenWindow(string id)
    {
        if (!windows.TryGetValue(id, out var window)) return false;
        window.Open();
        return true;
    }

    public bool CloseWindow(string id)
    {
        if (!windows.TryGetValue(id, out var window)) return false;
        window.Close();
        return true;
    }

    private void RegisterWindow(string id, MmoWindow window) => windows[id] = window;

    private void RefreshHotbar(ClientWorldState world)
    {
        foreach (var child in hotbarGrid.GetChildren())
            if (child is UiSlot slot)
            {
                slot.Clear();
                slot.Text = string.Empty;
            }

        foreach (var state in world.Local.Hotbar.Slots)
        {
            var slot = hotbarGrid.GetSlot(state.Index);
            if (slot is null) continue;
            var label = state.IsEmpty ? DefaultHotbarName(state.Index) : state.Kind.ToString();
            slot.Text = $"{state.Index + 1}\n{label}";
            slot.TooltipText = label;
        }
    }

    private void RefreshInventory(ClientWorldState world)
    {
        if (lastInventoryRevision != world.Inventory.Revision)
        {
            lastInventoryRevision = world.Inventory.Revision;
            pendingInventoryItems.Clear();
        }
        ProjectInventoryThroughGodot(world);
        inventoryCapacity.Text = $"Stacks: {world.Inventory.Count}";
        inventorySummary.Text = world.Inventory.Count == 0
            ? "Inventario vacío."
            : pendingInventoryItems.Count > 0
                ? $"Esperando confirmación del servidor · {pendingInventoryItems.Count} operación(es)…"
                : $"{world.Inventory.Count} stacks autoritativos · arrastra para reordenar · doble clic para equipar.";
    }

    private void ProjectInventoryThroughGodot(ClientWorldState world)
    {
        var fingerprint = string.Join('|', world.Inventory.Slots
            .OrderBy(static value => value.Slot)
            .Select(static value =>
                $"{value.Slot}:{value.ItemId.Value:N}:{value.DefinitionId.Value:N}:{value.Quantity}:{value.Durability}:" +
                world.Local.Equipment.Contains(value.ItemId)));
        if (fingerprint == lastInventoryProjectionFingerprint)
            return;

        var slots = new Godot.Collections.Array<Godot.Collections.Dictionary>();
        foreach (var state in world.Inventory.Slots)
        {
            slots.Add(new Godot.Collections.Dictionary
            {
                ["slot"] = state.Slot,
                ["item_id"] = state.ItemId.Value.ToString(),
                ["definition_id"] = state.DefinitionId.Value.ToString(),
                ["quantity"] = state.Quantity,
                ["durability"] = state.Durability,
                ["equipped"] = world.Local.Equipment.Contains(state.ItemId)
            });
        }

        inventoryProjection.Call("apply_server_snapshot", slots);
        lastInventoryProjectionFingerprint = fingerprint;
    }

    private void OnInventoryItemActivated(string rawItemId)
    {
        if (GetParent() is not MmoGame game || game.Network is null || !Guid.TryParse(rawItemId, out var parsed)) return;
        var itemId = new ItemInstanceId(parsed);
        if (pendingInventoryItems.Count > 0 || !pendingInventoryItems.Add(itemId)) return;
        var slot = game.Network.World.Inventory.Slots.FirstOrDefault(value => value.ItemId == itemId);
        if (slot is null)
        {
            pendingInventoryItems.Remove(itemId);
            return;
        }
        if (game.Network.World.Local.Equipment.Contains(slot.ItemId))
            game.Network.UnequipItem(slot.ItemId);
        else
            game.Network.EquipItem(slot.ItemId);
    }

    private void OnInventoryItemMoveRequested(string rawItemId, int targetIndex)
    {
        if (GetParent() is not MmoGame game || game.Network is null ||
            !Guid.TryParse(rawItemId, out var parsed) || targetIndex < 0 || targetIndex >= game.Network.World.Inventory.Count)
            return;
        var itemId = new ItemInstanceId(parsed);
        if (pendingInventoryItems.Count > 0 || !pendingInventoryItems.Add(itemId)) return;
        game.Network.MoveInventoryItem(itemId, targetIndex);
    }

    private static string DefaultHotbarName(int index) => index switch
    {
        0 => "Básico",
        1 => "Tierra",
        2 => "Fuego",
        3 => "Aire",
        4 => "Agua",
        5 => "Neutral",
        _ => "—"
    };
}
