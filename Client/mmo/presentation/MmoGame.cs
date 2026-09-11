using Godot;
using NuevoMMO.Core;
using NuevoMMO.Network;

namespace NuevoMMO.GodotClient;

public partial class MmoGame : Node2D
{
    public NetworkBridge Network { get; private set; } = null!;
    private WorldPresentation world = null!;
    private LineEdit host = null!, username = null!, password = null!;
    private Label status = null!, details = null!, statsText = null!, statsNotice = null!, combatText = null!;
    private Button login = null!, register = null!, disconnect = null!;
    private readonly Dictionary<PrimaryAttributeId, Button> attributeButtons = [];
    private readonly List<Button> combatButtons = [];
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

        BuildStatsPanel(hud);

        var controls = new HBoxContainer { Position = new(10, 308) }; hud.AddChild(controls);
        foreach (var pair in new[] { ("←", "mmo_left"), ("↑", "mmo_up"), ("↓", "mmo_down"), ("→", "mmo_right") })
        {
            var button = new Button { Text = pair.Item1, CustomMinimumSize = new(44, 42), FocusMode = Control.FocusModeEnum.None };
            controls.AddChild(button); button.ButtonDown += () => Input.ActionPress(pair.Item2); button.ButtonUp += () => Input.ActionRelease(pair.Item2);
        }
    }

    private void BuildStatsPanel(CanvasLayer hud)
    {
        var panel = new PanelContainer { Position = new(640, 10), CustomMinimumSize = new(410, 0) };
        hud.AddChild(panel);
        var box = new VBoxContainer(); panel.AddChild(box);
        box.AddChild(new Label { Text = "PERSONAJE · DEBUG V0.1" });
        statsText = new Label { Text = "Stats esperando servidor…" }; box.AddChild(statsText);
        statsText.AddThemeFontSizeOverride("font_size", 12);

        var buttons = new HBoxContainer(); box.AddChild(buttons);
        AddAttributeButton(buttons, PrimaryAttributeId.Strength, "+STR");
        AddAttributeButton(buttons, PrimaryAttributeId.Intelligence, "+INT");
        AddAttributeButton(buttons, PrimaryAttributeId.Agility, "+AGI");
        AddAttributeButton(buttons, PrimaryAttributeId.Spirit, "+SPI");
        AddAttributeButton(buttons, PrimaryAttributeId.Vitality, "+VIT");

        statsNotice = new Label(); box.AddChild(statsNotice);
        statsNotice.AddThemeFontSizeOverride("font_size", 11);

        box.AddChild(new HSeparator());
        box.AddChild(new Label { Text = "COMBATE · FIXTURE" });
        var combatRow1 = new HBoxContainer(); box.AddChild(combatRow1);
        AddCombatButton(combatRow1, "Básico", DevelopmentAttackKind.Basic, targetDummy: true);
        AddCombatButton(combatRow1, "Tierra", DevelopmentAttackKind.Earth, targetDummy: true);
        AddCombatButton(combatRow1, "Fuego", DevelopmentAttackKind.Fire, targetDummy: true);
        var combatRow2 = new HBoxContainer(); box.AddChild(combatRow2);
        AddCombatButton(combatRow2, "Aire", DevelopmentAttackKind.Air, targetDummy: true);
        AddCombatButton(combatRow2, "Agua", DevelopmentAttackKind.Water, targetDummy: true);
        AddCombatButton(combatRow2, "Neutral/STR", DevelopmentAttackKind.NeutralStrength, targetDummy: true);
        var xpButton = new Button { Text = "Golpear Explorador XP", FocusMode = Control.FocusModeEnum.None };
        xpButton.Pressed += () => AttackVisibleMob(DevelopmentAttackKind.Basic, targetDummy: false);
        box.AddChild(xpButton); combatButtons.Add(xpButton);

        combatText = new Label { Text = "Telemetría esperando primer golpe…" };
        combatText.AddThemeFontSizeOverride("font_size", 11);
        box.AddChild(combatText);
    }

    private void AddAttributeButton(HBoxContainer parent, PrimaryAttributeId attribute, string text)
    {
        var button = new Button { Text = text, FocusMode = Control.FocusModeEnum.None };
        button.Pressed += () => Network.AllocateAttribute(attribute);
        parent.AddChild(button);
        attributeButtons[attribute] = button;
    }

    private void AddCombatButton(HBoxContainer parent, string text, DevelopmentAttackKind attack, bool targetDummy)
    {
        var button = new Button { Text = text, FocusMode = Control.FocusModeEnum.None };
        button.Pressed += () => AttackVisibleMob(attack, targetDummy);
        parent.AddChild(button);
        combatButtons.Add(button);
    }

    private void AttackVisibleMob(DevelopmentAttackKind attack, bool targetDummy)
    {
        if (!Network.InWorld) return;
        var local = Network.World.Local.Position;
        var candidate = Network.World.Entities.All.Values
            .OfType<MobState>()
            .Where(mob => targetDummy
                ? mob.DisplayName.Contains("Muñeco de entrenamiento", StringComparison.OrdinalIgnoreCase)
                : mob.DisplayName.Contains("Explorador XP", StringComparison.OrdinalIgnoreCase))
            .OrderBy(mob => DistanceSquared(local, mob.Position))
            .FirstOrDefault();
        if (candidate is null)
        {
            statsNotice.Text = targetDummy ? "El dummy no está visible." : "El mob XP no está visible o ya salió del AOI.";
            return;
        }
        Network.DevelopmentAttack(candidate.Id, attack);
    }

    private static float DistanceSquared(Vector2Data left, Vector2Data right)
    {
        var x = left.X - right.X;
        var y = left.Y - right.Y;
        return x * x + y * y;
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
        RefreshStats();
        RefreshCombat();
        var busy = Network.Status.StartsWith("Conectando", StringComparison.Ordinal)
            || Network.Status.StartsWith("Registrando", StringComparison.Ordinal)
            || Network.Status.StartsWith("Autenticado", StringComparison.Ordinal)
            || Network.World.Session.Map is not null;
        login.Disabled = busy;
        register.Disabled = busy;
        disconnect.Disabled = Network.Status == "Desconectado";
        username.Editable = !busy;
        password.Editable = !busy;
        foreach (var button in combatButtons) button.Disabled = !Network.InWorld;
    }

    private void RefreshStats()
    {
        var stats = Network.World.Local.Stats;
        if (stats is null)
        {
            statsText.Text = "Stats esperando servidor…";
            statsNotice.Text = Network.LastNotice;
            foreach (var button in attributeButtons.Values) button.Disabled = true;
            return;
        }

        var xpTarget = stats.ExperienceToNextLevel == 0 ? "MAX" : stats.ExperienceToNextLevel.ToString();
        statsText.Text =
            $"Lv {stats.Level} · XP {stats.Experience}/{xpTarget} · puntos {stats.AvailableAttributePoints}\n" +
            $"STR {Format(stats.Strength)}  INT {Format(stats.Intelligence)}  AGI {Format(stats.Agility)}\n" +
            $"SPI {Format(stats.Spirit)}  VIT {Format(stats.Vitality)}\n" +
            $"HP {stats.Health}/{stats.MaxHealth} · PM {stats.Mana}/{stats.MaxMana}\n" +
            $"DEF {stats.Defense:0.##} · PM/s {stats.ManaRegenPerSecond:0.##} · fuera combate {stats.OutOfCombatManaRegenPerSecond:0.##}\n" +
            $"RES Tierra {stats.ResistEarth}% · Fuego {stats.ResistFire}% · Aire {stats.ResistAir}% · Agua {stats.ResistWater}% · Neutral {stats.ResistNeutral}%\n" +
            $"Luck {stats.Luck}";
        statsNotice.Text = Network.LastNotice;

        UpdateButton(PrimaryAttributeId.Strength, stats.Strength, stats.AvailableAttributePoints);
        UpdateButton(PrimaryAttributeId.Intelligence, stats.Intelligence, stats.AvailableAttributePoints);
        UpdateButton(PrimaryAttributeId.Agility, stats.Agility, stats.AvailableAttributePoints);
        UpdateButton(PrimaryAttributeId.Spirit, stats.Spirit, stats.AvailableAttributePoints);
        UpdateButton(PrimaryAttributeId.Vitality, stats.Vitality, stats.AvailableAttributePoints);
    }

    private void RefreshCombat()
    {
        var combat = Network.LastCombat;
        if (combat is null)
        {
            combatText.Text = string.IsNullOrWhiteSpace(Network.LastNotice)
                ? "Telemetría esperando primer golpe…"
                : Network.LastNotice;
            return;
        }
        combatText.Text =
            $"{combat.Attack} · {combat.DamageType}/{combat.ScalingAttribute}\n" +
            $"RAW {combat.RawDamage:0.##} → RES {combat.ResistancePercent:0.##}% → {combat.AppliedDamage} daño" +
            (combat.Critical ? " · CRIT" : "") + "\n" +
            $"Target HP {combat.TargetHealth}/{combat.TargetMaxHealth}\n" +
            $"DPS5 {combat.Dps5Seconds:0.##} · DPS10 {combat.Dps10Seconds:0.##} · Total {combat.TotalDamage} · Hits {combat.Hits} · Crits {combat.CriticalHits}";
    }

    private void UpdateButton(PrimaryAttributeId attribute, AttributeSnapshot snapshot, int points)
    {
        var button = attributeButtons[attribute];
        button.Disabled = !Network.InWorld || snapshot.NextCost <= 0 || points < snapshot.NextCost;
        button.TooltipText = snapshot.NextCost <= 0 ? "Cap natural alcanzado" : $"Coste: {snapshot.NextCost} punto(s)";
    }

    private static string Format(AttributeSnapshot value)
        => value.Natural == value.Effective ? value.Natural.ToString() : $"{value.Natural}→{value.Effective}";

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
