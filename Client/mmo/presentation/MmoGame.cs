using Godot;

namespace NuevoMMO.GodotClient;

public partial class MmoGame : Node2D
{
    public NetworkBridge Network { get; private set; } = null!;
    private WorldPresentation world = null!;
    private LineEdit host = null!, username = null!, password = null!;
    private Label status = null!, details = null!;
    private Button login = null!, register = null!, disconnect = null!;
    private double accumulator;
    private bool focused = true;
    public Vector2? TestInput { get; set; }

    public override void _Ready()
    {
        RenderingServer.SetDefaultClearColor(new Color("121c24"));
        Network = new(); AddChild(Network);
        world = new(); AddChild(world);
        BuildUi(); RegisterInput();
    }

    private void BuildUi()
    {
        var hud = new CanvasLayer(); AddChild(hud);
        var panel = new PanelContainer { Position = new(10, 10), CustomMinimumSize = new(620, 0) }; hud.AddChild(panel);
        var box = new VBoxContainer(); panel.AddChild(box);

        var serverRow = new HBoxContainer(); box.AddChild(serverRow);
        serverRow.AddChild(new Label { Text = "Servidor" });
        host = new() { Text = "127.0.0.1", CustomMinimumSize = new(150, 32) }; serverRow.AddChild(host);

        var accountRow = new HBoxContainer(); box.AddChild(accountRow);
        username = new() { PlaceholderText = "Usuario", MaxLength = 24, CustomMinimumSize = new(150, 32) }; accountRow.AddChild(username);
        password = new() { PlaceholderText = "Contraseña", Secret = true, MaxLength = 128, CustomMinimumSize = new(150, 32) }; accountRow.AddChild(password);
        login = new() { Text = "Ingresar" }; accountRow.AddChild(login);
        register = new() { Text = "Registrar" }; accountRow.AddChild(register);
        disconnect = new() { Text = "Salir" }; accountRow.AddChild(disconnect);
        var preferences = new Button { Text = "Ajustes" }; accountRow.AddChild(preferences);

        preferences.Pressed += () => GetNode("/root/Globals").Call("open_settings_menu");
        login.Pressed += () =>
        {
            GetViewport().GuiReleaseFocus();
            Network.Login(host.Text, 7777, username.Text, password.Text);
        };
        register.Pressed += () =>
        {
            GetViewport().GuiReleaseFocus();
            Network.Register(host.Text, 7777, username.Text, password.Text);
        };
        disconnect.Pressed += Network.DisconnectFromServer;

        status = new() { Text = "Nuevo MMO · servidor local" }; box.AddChild(status);
        details = new() { Text = "Crea una cuenta o ingresa. Movimiento: WASD / flechas / mando." }; box.AddChild(details);
        details.AddThemeFontSizeOverride("font_size", 12);

        var controls = new HBoxContainer { Position = new(10, 308) }; hud.AddChild(controls);
        foreach (var pair in new[] { ("←", "mmo_left"), ("↑", "mmo_up"), ("↓", "mmo_down"), ("→", "mmo_right") })
        {
            var button = new Button { Text = pair.Item1, CustomMinimumSize = new(44, 42), FocusMode = Control.FocusModeEnum.None };
            controls.AddChild(button); button.ButtonDown += () => Input.ActionPress(pair.Item2); button.ButtonUp += () => Input.ActionRelease(pair.Item2);
        }
    }

    private static void RegisterInput()
    {
        var bindings = new[] { ("mmo_left", Key.A, Key.Left, JoyAxis.LeftX, -1f), ("mmo_right", Key.D, Key.Right, JoyAxis.LeftX, 1f),
            ("mmo_up", Key.W, Key.Up, JoyAxis.LeftY, -1f), ("mmo_down", Key.S, Key.Down, JoyAxis.LeftY, 1f) };
        foreach (var binding in bindings)
        {
            if (InputMap.HasAction(binding.Item1)) continue;
            InputMap.AddAction(binding.Item1, .2f);
            InputMap.ActionAddEvent(binding.Item1, new InputEventKey { PhysicalKeycode = binding.Item2 });
            InputMap.ActionAddEvent(binding.Item1, new InputEventKey { PhysicalKeycode = binding.Item3 });
            InputMap.ActionAddEvent(binding.Item1, new InputEventJoypadMotion { Axis = binding.Item4, AxisValue = binding.Item5 });
        }
    }

    public override void _Process(double delta)
    {
        var typing = GetViewport().GuiGetFocusOwner() is LineEdit;
        var input = TestInput ?? (focused && !typing ? Input.GetVector("mmo_left", "mmo_right", "mmo_up", "mmo_down") : Vector2.Zero);
        if (Network.World.Session.Map is { } map && Network.InWorld)
        {
            var step = map.TickMilliseconds / 1000d;
            accumulator += Math.Min(delta, step * 2);
            if (accumulator >= step) { accumulator %= step; Network.SubmitInput(input.X, input.Y); }
            world.Present(Network.World, input, (float)(accumulator / step));
            details.Text = $"Jugador {Network.World.Session.Self.Value} · visibles {Network.World.Entities.All.Count} · tick {Network.World.LastTick} · pendientes {Network.World.Predictor!.PendingCount}";
        }
        else { accumulator = 0; world.Present(Network.World, Vector2.Zero, 0); }
        status.Text = Network.Status;
        var busy = Network.Status.StartsWith("Conectando", StringComparison.Ordinal)
            || Network.Status.StartsWith("Registrando", StringComparison.Ordinal)
            || Network.Status.StartsWith("Autenticado", StringComparison.Ordinal)
            || Network.World.Session.Map is not null;
        login.Disabled = busy;
        register.Disabled = busy;
        disconnect.Disabled = Network.Status == "Desconectado";
        username.Editable = !busy;
        password.Editable = !busy;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationApplicationFocusOut)
        {
            focused = false;
            foreach (var action in new[] { "mmo_left", "mmo_right", "mmo_up", "mmo_down" }) Input.ActionRelease(action);
        }
        if (what == NotificationApplicationFocusIn) focused = true;
    }
}
