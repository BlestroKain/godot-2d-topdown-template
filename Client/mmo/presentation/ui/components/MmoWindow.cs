using Godot;

namespace NuevoMMO.GodotClient.UI;

/// <summary>
/// Shared behavior for all MMO windows. Layout remains in .tscn; this only provides close and drag behavior.
/// </summary>
public partial class MmoWindow : PanelContainer
{
    [Signal] public delegate void WindowClosedEventHandler();

    private Control? header;
    private Button? closeButton;
    private bool dragging;
    private Vector2 dragOffset;

    public override void _Ready()
    {
        header = GetNodeOrNull<Control>("%HeaderDrag");
        closeButton = GetNodeOrNull<Button>("%CloseButton");

        if (header is not null)
            header.GuiInput += OnHeaderGuiInput;
        if (closeButton is not null)
            closeButton.Pressed += Close;
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
