using Godot;

namespace NuevoMMO.GodotClient.UI;

/// <summary>
/// Shared behavior for all MMO windows. Layout remains in .tscn; this only provides generic window interaction.
/// </summary>
public partial class MmoWindow : PanelContainer
{
    [Signal] public delegate void WindowClosedEventHandler();

    private Control? header;
    private Button? closeButton;
    private bool dragging;
    private Vector2 dragOffset;
    private readonly List<ButtonGroup> tabGroups = new();

    public override void _Ready()
    {
        ThemeTypeVariation = "UiWindowPanel";
        header = GetNodeOrNull<Control>("%HeaderDrag");
        closeButton = GetNodeOrNull<Button>("%CloseButton");

        if (header is not null)
            header.GuiInput += OnHeaderGuiInput;
        if (closeButton is not null)
            closeButton.Pressed += Close;

        SetupExclusiveTabContainers(this);
    }

    public void Open()
    {
        Visible = true;
        MoveToFront();
    }

    public void Close()
    {
        Visible = false;
        dragging = false;
        EmitSignal(SignalName.WindowClosed);
    }

    public void Toggle()
    {
        if (Visible) Close();
        else Open();
    }

    private void SetupExclusiveTabContainers(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is UiTabBar)
                continue;

            if (child is HBoxContainer container && (container.Name == "Tabs" || container.Name == "ChestTabs"))
                ConfigureExclusiveButtons(container);

            SetupExclusiveTabContainers(child);
        }
    }

    private void ConfigureExclusiveButtons(HBoxContainer container)
    {
        var buttons = container.GetChildren().OfType<Button>().ToArray();
        if (buttons.Length == 0)
            return;

        var group = new ButtonGroup();
        tabGroups.Add(group);
        var anySelected = false;

        foreach (var button in buttons)
        {
            button.ToggleMode = true;
            button.ButtonGroup = group;
            button.ThemeTypeVariation = "UiTabButton";
            anySelected |= button.ButtonPressed;
        }

        if (!anySelected)
            buttons[0].ButtonPressed = true;
    }

    private void OnHeaderGuiInput(InputEvent inputEvent)
    {
        switch (inputEvent)
        {
            case InputEventMouseButton mouseButton when mouseButton.ButtonIndex == MouseButton.Left:
                dragging = mouseButton.Pressed;
                if (dragging)
                {
                    dragOffset = GetViewport().GetMousePosition() - GlobalPosition;
                    MoveToFront();
                }
                header?.AcceptEvent();
                break;
            case InputEventMouseMotion when dragging:
                GlobalPosition = GetViewport().GetMousePosition() - dragOffset;
                header?.AcceptEvent();
                break;
        }
    }
}
