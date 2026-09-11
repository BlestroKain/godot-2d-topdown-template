using Godot;
using NuevoMMO.Client;
using NuevoMMO.Core;
using NuevoMMO.Network;

namespace NuevoMMO.GodotClient;

/// <summary>
/// HUD de juego. La escena `game_hud.tscn` es la fuente visual; este script la alimenta.
/// </summary>
public partial class GameHud : CanvasLayer
{
    private Label selfVitals = null!, targetVitals = null!, chatLog = null!, hotbarLabel = null!, inventoryLabel = null!, characterStats = null!;
    private Control characterRoot = null!, inventoryRoot = null!, escapeRoot = null!;
    private bool characterOpen, inventoryOpen, escapeOpen;
    private string lastChatFingerprint = string.Empty;

    public override void _Ready()
    {
        Layer = 10;
        selfVitals = GetNode<Label>("SelfBox/VBox/Vitals");
        targetVitals = GetNode<Label>("TargetBox/VBox/Vitals");
        chatLog = GetNode<Label>("ChatBox/VBox/Log");
        hotbarLabel = GetNode<Label>("Hotbar/VBox/Slots");
        inventoryLabel = GetNode<Label>("InventoryWindow/VBox/Slots");
        characterStats = GetNode<Label>("CharacterWindow/VBox/Stats");
        characterRoot = GetNode<Control>("CharacterWindow");
        inventoryRoot = GetNode<Control>("InventoryWindow");
        escapeRoot = GetNode<Control>("EscapeWindow");
        Visible = false;
    }

    public void Present(NetworkBridge network)
    {
        ArgumentNullException.ThrowIfNull(network);
        var world = network.World;
        Visible = world.Flow == GameFlowState.InWorld;
        if (!Visible) return;

        var stats = world.Local.Stats;
        selfVitals.Text = stats is null
            ? "HP --/--  PM --/--"
            : $"{world.Local.Entity?.DisplayName ?? "Jugador"}  HP {stats.Health}/{stats.MaxHealth}  PM {stats.Mana}/{stats.MaxMana}  Lv {stats.Level}";

        targetVitals.Text = world.Local.Target.HasTarget
            ? $"{world.Local.Target.Kind} · {world.Local.Target.DisplayName}"
            : "Sin objetivo · J / 1–6 para golpear";

        var chat = string.Join('\n', world.Chat.Messages.TakeLast(8).Select(static message => $"[{message.Channel}] {message.Text}"));
        if (chat != lastChatFingerprint)
        {
            chatLog.Text = string.IsNullOrEmpty(chat) ? "…" : chat;
            lastChatFingerprint = chat;
        }

        hotbarLabel.Text = string.Join("   ", world.Local.Hotbar.Slots.Select(slot =>
            slot.IsEmpty ? $"{slot.Index + 1}:{DefaultHotbarName(slot.Index)}" : $"{slot.Index + 1}:{slot.Kind}"));

        inventoryLabel.Text = world.Inventory.Count == 0
            ? "Inventario vacío (el servidor aún no replica slots)."
            : string.Join('\n', world.Inventory.Slots.Select(static slot => $"#{slot.Slot}  x{slot.Quantity}"));

        characterStats.Text = stats is null
            ? "Stats esperando servidor…"
            : $"Lv {stats.Level}  XP {stats.Experience}/{stats.ExperienceToNextLevel}\n" +
              $"STR {stats.Strength.Effective}  INT {stats.Intelligence.Effective}  AGI {stats.Agility.Effective}\n" +
              $"SPI {stats.Spirit.Effective}  VIT {stats.Vitality.Effective}  puntos {stats.AvailableAttributePoints}\n" +
              $"HP {stats.Health}/{stats.MaxHealth}  PM {stats.Mana}/{stats.MaxMana}";
    }

    public void ToggleCharacter() => characterRoot.Visible = characterOpen = !characterOpen;
    public void ToggleInventory() => inventoryRoot.Visible = inventoryOpen = !inventoryOpen;
    public void ToggleEscape() => escapeRoot.Visible = escapeOpen = !escapeOpen;

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
