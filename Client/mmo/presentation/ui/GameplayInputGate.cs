using Godot;

namespace NuevoMMO.GodotClient.UI;

/// <summary>
/// Godot-first gameplay input gate backed by the vendored State Charts addon.
/// The server remains authoritative; this node only decides whether local gameplay input
/// should be produced while modal MMO UI or text entry owns the player's attention.
/// </summary>
public partial class GameplayInputGate : Node
{
    private static readonly StringName UiCaptureEvent = new("ui_capture");
    private static readonly StringName UiReleaseEvent = new("ui_release");

    private static readonly NodePath[] WindowPaths =
    [
        new("Root/CharacterWindow"),
        new("Root/InventoryWindow"),
        new("Root/QuestJournal"),
        new("Root/TechniquesWindow"),
        new("Root/ShopWindow"),
        new("Root/BankWindow"),
        new("Root/MailWindow"),
        new("Root/CommunityWindow"),
        new("Root/DialogueWindow"),
        new("Root/ProfessionWindow"),
        new("Root/EscapeWindow")
    ];

    private Node stateChart = null!;
    private readonly List<Control> modalWindows = [];
    private bool requestedCapture;
    private bool chartEntered;

    public bool GameplayEnabled { get; private set; } = true;
    public bool UiCaptured => !GameplayEnabled;

    public override void _Ready()
    {
        stateChart = GetNode<Node>("StateChart");

        var gameplayState = GetNode<Node>("StateChart/Root/Gameplay");
        var uiCapturedState = GetNode<Node>("StateChart/Root/UiCaptured");
        gameplayState.Connect("state_entered", Callable.From(() => OnStateEntered(captured: false)));
        uiCapturedState.Connect("state_entered", Callable.From(() => OnStateEntered(captured: true)));

        var hud = GetNodeOrNull<Node>("../GameHud");
        if (hud is null)
        {
            GD.PushWarning("[GameplayInputGate] GameHud sibling not found; only text focus will capture gameplay input.");
            return;
        }

        foreach (var path in WindowPaths)
        {
            var window = hud.GetNodeOrNull<Control>(path);
            if (window is not null)
                modalWindows.Add(window);
            else
                GD.PushWarning($"[GameplayInputGate] MMO window not found at GameHud/{path}.");
        }
    }

    public void Refresh(Control? focusOwner)
    {
        var captured = focusOwner is LineEdit || focusOwner is TextEdit;
        if (!captured)
        {
            foreach (var window in modalWindows)
            {
                if (!window.Visible)
                    continue;

                captured = true;
                break;
            }
        }

        SetUiCaptured(captured);
    }

    private void SetUiCaptured(bool captured)
    {
        requestedCapture = captured;

        if (chartEntered && captured == UiCaptured)
            return;

        stateChart.Call("send_event", captured ? UiCaptureEvent : UiReleaseEvent);
    }

    private void OnStateEntered(bool captured)
    {
        chartEntered = true;
        GameplayEnabled = !captured;

        // StateChart enters its initial state deferred. If UI requested capture before that
        // first entry, replay the desired state now; the addon queues the event transactionally.
        if (requestedCapture != captured)
            stateChart.Call("send_event", requestedCapture ? UiCaptureEvent : UiReleaseEvent);
    }
}
