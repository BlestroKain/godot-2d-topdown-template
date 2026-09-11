using Godot;
using NuevoMMO.Client;
using NuevoMMO.Core;
using NuevoMMO.Network;

namespace NuevoMMO.GodotClient;

/// <summary>
/// HUD de juego inspirado en GameInterface de Broken Reborn:
/// entity boxes, chat, hotbar, inventario y personaje. Godot Controls, no Gwen.
/// </summary>
public partial class GameHud : CanvasLayer
{
    private Label selfVitals = null!, targetVitals = null!, chatLog = null!, hotbarLabel = null!, inventoryLabel = null!;
    private Control characterRoot = null!, inventoryRoot = null!, escapeRoot = null!;
    private bool characterOpen, inventoryOpen, escapeOpen;

    public override void _Ready()
    {
        Layer = 10;
        BuildSelfBox();
        BuildTargetBox();
        BuildChat();
        BuildHotbar();
        BuildCharacter();
        BuildInventory();
        BuildEscape();
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

        if (world.Local.Target.HasTarget)
            targetVitals.Text = $"{world.Local.Target.Kind} · {world.Local.Target.DisplayName}";
        else
            targetVitals.Text = "Sin objetivo";

        chatLog.Text = string.Join('\n', world.Chat.Messages.TakeLast(8).Select(static message => $"[{message.Channel}] {message.Text}"));
        hotbarLabel.Text = string.Join("  ", world.Local.Hotbar.Slots.Select(static slot =>
            slot.IsEmpty ? $"{slot.Index + 1}:—" : $"{slot.Index + 1}:{slot.Kind}"));
        inventoryLabel.Text = world.Inventory.Count == 0
            ? "Inventario vacío (el servidor aún no replica slots)."
            : string.Join('\n', world.Inventory.Slots.Select(static slot => $"#{slot.Slot} x{slot.Quantity}"));
    }

    public void ToggleCharacter() => characterRoot.Visible = characterOpen = !characterOpen;
    public void ToggleInventory() => inventoryRoot.Visible = inventoryOpen = !inventoryOpen;
    public void ToggleEscape() => escapeRoot.Visible = escapeOpen = !escapeOpen;

    private void BuildSelfBox()
    {
        var panel = Box(10, 360, 360);
        selfVitals = new Label { Text = "HP --/--" };
        panel.AddChild(selfVitals);
    }

    private void BuildTargetBox()
    {
        var panel = Box(380, 360, 280);
        targetVitals = new Label { Text = "Sin objetivo" };
        panel.AddChild(targetVitals);
    }

    private void BuildChat()
    {
        var panel = Box(10, 420, 520);
        chatLog = new Label();
        chatLog.AddThemeFontSizeOverride("font_size", 12);
        panel.AddChild(chatLog);
    }

    private void BuildHotbar()
    {
        var panel = Box(10, 620, 520);
        hotbarLabel = new Label { Text = "1:—  2:—  3:—  4:—  5:—  6:—" };
        panel.AddChild(hotbarLabel);
    }

    private void BuildCharacter()
    {
        var box = Window(640, 220, 410, "Personaje", out characterRoot);
        characterRoot.Visible = false;
        box.AddChild(new Label { Text = "Stats en el panel de debug V0.1." });
    }

    private void BuildInventory()
    {
        var box = Window(10, 120, 360, "Inventario", out inventoryRoot);
        inventoryRoot.Visible = false;
        inventoryLabel = new Label();
        box.AddChild(inventoryLabel);
    }

    private void BuildEscape()
    {
        var box = Window(400, 240, 280, "Menú", out escapeRoot);
        escapeRoot.Visible = false;
        box.AddChild(new Label { Text = "Esc cierra. Ajustes: botón del login." });
    }

    private VBoxContainer Box(float x, float y, float width)
    {
        var panel = new PanelContainer { Position = new(x, y), CustomMinimumSize = new(width, 0) };
        AddChild(panel);
        var box = new VBoxContainer();
        panel.AddChild(box);
        return box;
    }

    private VBoxContainer Window(float x, float y, float width, string title, out Control root)
    {
        var panel = new PanelContainer { Position = new(x, y), CustomMinimumSize = new(width, 0) };
        AddChild(panel);
        root = panel;
        var box = new VBoxContainer();
        panel.AddChild(box);
        box.AddChild(new Label { Text = title });
        return box;
    }
}
