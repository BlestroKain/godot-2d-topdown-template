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
    private Label selfVitals = null!, targetVitals = null!, chatLog = null!, inventorySummary = null!, characterStats = null!, levelLabel = null!;
    private ProgressBar healthBar = null!, manaBar = null!;
    private UiSlotGrid hotbarGrid = null!, inventoryGrid = null!;
    private MmoWindow characterRoot = null!, inventoryRoot = null!, escapeRoot = null!, questRoot = null!, techniquesRoot = null!;
    private readonly Dictionary<string, MmoWindow> windows = new(StringComparer.OrdinalIgnoreCase);
    private string lastChatFingerprint = string.Empty;
    private UiSystemBinder systemBinder = null!;

    public override void _Ready()
    {
        Layer = 10;

        selfVitals = GetNode<Label>("Root/CombatBar/HBox/Vitals/SelfVitals");
        healthBar = GetNode<ProgressBar>("Root/CombatBar/HBox/Vitals/HealthBar");
        manaBar = GetNode<ProgressBar>("Root/CombatBar/HBox/Vitals/ManaBar");
        levelLabel = GetNode<Label>("Root/CombatBar/HBox/PortraitStack/Level");
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
        inventoryGrid = inventoryRoot.GetNode<UiSlotGrid>("Margin/VBox/SlotGrid");

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

        systemBinder = new UiSystemBinder(this);
        Visible = false;
    }

    public void Present(NetworkBridge network)
    {
        ArgumentNullException.ThrowIfNull(network);
        var world = network.World;
        Visible = world.Flow == GameFlowState.InWorld;
        if (!Visible) return;

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
        foreach (var child in inventoryGrid.GetChildren())
            if (child is UiSlot slot)
            {
                slot.Clear();
                slot.Text = string.Empty;
            }

        foreach (var state in world.Inventory.Slots)
        {
            var slot = inventoryGrid.GetSlot(state.Slot);
            if (slot is null) continue;
            slot.SetData(null, state.Quantity, $"Item {state.DefinitionId} · x{state.Quantity}");
            slot.Text = $"#{state.Slot}\nx{state.Quantity}";
        }

        inventorySummary.Text = world.Inventory.Count == 0
            ? "Inventario vacío."
            : $"{world.Inventory.Count} stacks replicados por el servidor.";
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
