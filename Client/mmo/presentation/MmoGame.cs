using Godot;
using NuevoMMO.Client;
using NuevoMMO.Core;
using NuevoMMO.Network;

namespace NuevoMMO.GodotClient;

public partial class MmoGame : Node2D
{
    public NetworkBridge Network { get; private set; } = null!;
    private WorldPresentation world = null!;
    private GameHud gameHud = null!;
    private LineEdit host = null!, username = null!, password = null!;
    private Label status = null!, details = null!, statsText = null!, statsNotice = null!, combatText = null!;
    private Button login = null!, register = null!, disconnect = null!;
    private readonly Dictionary<PrimaryAttributeId, Button> attributeButtons = [];
    private readonly List<Button> combatButtons = [];
    private readonly InputManager inputs = new();
    private double accumulator;
    private bool focused = true;
    public Vector2? TestInput { get; set; }

    public override void _Ready()
    {
        RenderingServer.SetDefaultClearColor(new Color("121c24"));
        Network = new(); AddChild(Network);
        world = GetNodeOrNull<WorldPresentation>("World");
        if (world is null)
        {
            world = new WorldPresentation();
            AddChild(world);
        }
        gameHud = GetNodeOrNull<GameHud>("GameHud");
        if (gameHud is null)
        {
            gameHud = GD.Load<PackedScene>("res://mmo/presentation/hud/game_hud.tscn").Instantiate<GameHud>();
            AddChild(gameHud);
        }
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
        box.AddChild(new Label { Text = "COMBATE · DEBUG (Development)" });
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
        MobState? candidate = null;
        if (targetDummy)
            candidate = inputs.Combat.FindTarget(Network.World, mob =>
                mob.DisplayName.Contains("Muñeco de entrenamiento", StringComparison.OrdinalIgnoreCase));
        else
            candidate = inputs.Combat.FindTarget(Network.World, mob =>
                mob.DisplayName.Contains("Explorador XP", StringComparison.OrdinalIgnoreCase));
        candidate ??= inputs.Combat.FindTarget(Network.World);
        if (candidate is null)
        {
            statsNotice.Text = "No hay un mob visible en alcance.";
            return;
        }
        SelectTarget(candidate);
        Network.DevelopmentAttack(candidate.Id, attack);
    }

    private void BasicAttackVisible()
    {
        if (!Network.InWorld) return;
        var candidate = CurrentMobTarget() ?? inputs.Combat.FindTarget(Network.World);
        if (candidate is null)
        {
            statsNotice.Text = "No hay un objetivo visible.";
            return;
        }

        SelectTarget(candidate);
        Network.BasicAttack(candidate.Id);
    }

    private void UseHotbar(int index)
    {
        if (!Network.InWorld) return;
        if (index == 0)
        {
            BasicAttackVisible();
            return;
        }

        var candidate = CurrentMobTarget() ?? inputs.Combat.FindTarget(Network.World);
        var targetId = candidate?.Id ?? default;
        if (candidate is not null) SelectTarget(candidate);
        var origin = Network.World.Local.Position;
        var aim = inputs.Gameplay.Aim(GetViewport(), world.Camera, new Vector2(origin.X, origin.Y));
        Network.UseTechnique(CanonicalCombatContent.BasicAttackId, targetId, new Vector2Data(origin.X + aim.X * 64f, origin.Y + aim.Y * 64f));
    }

    private MobState? CurrentMobTarget()
    {
        if (Network.World.Local.Target.Id is not { } id) return null;
        return Network.World.Entities.All.TryGetValue(id, out var entity) ? entity as MobState : null;
    }

    private void SelectTarget(MobState candidate)
    {
        Network.World.Local.Target.Set(candidate);
        Network.World.Local.Entity?.TryTarget(candidate.Id);
        Network.SetTarget(candidate.Id);
    }

    private void CycleVisibleTarget()
    {
        if (!Network.InWorld) return;
        var next = inputs.Combat.CycleTarget(Network.World, CurrentMobTarget()?.Id);
        if (next is null)
        {
            statsNotice.Text = "No hay objetivos para ciclar.";
            return;
        }

        SelectTarget(next);
    }

    private void SelectAtMouse()
    {
        if (!Network.InWorld) return;
        var mouse = inputs.Gameplay.MouseWorld(GetViewport(), world.Camera);
        var origin = new Vector2Data(mouse.X, mouse.Y);
        var candidate = CombatTargeting.NearestMob(Network.World.Entities.All.Values, origin, maxRange: 48f);
        if (candidate is null) return;
        SelectTarget(candidate);
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

        RegisterAction(CombatInput.AttackAction, Key.J, Key.Space);
        RegisterAction(InteractionInput.InteractAction, Key.E, Key.F);
        RegisterAction("mmo_clear_target", Key.X);
        RegisterAction("mmo_cycle_target", Key.Tab);
        RegisterAction("mmo_hotkey_1", Key.Key1);
        RegisterAction("mmo_hotkey_2", Key.Key2);
        RegisterAction("mmo_hotkey_3", Key.Key3);
        RegisterAction("mmo_hotkey_4", Key.Key4);
        RegisterAction("mmo_hotkey_5", Key.Key5);
        RegisterAction("mmo_hotkey_6", Key.Key6);
        RegisterAction("mmo_hotkey_7", Key.Key7);
        RegisterAction("mmo_hotkey_8", Key.Key8);
        RegisterAction("mmo_hotkey_9", Key.Key9);
        RegisterAction("mmo_hotkey_10", Key.Key0);
        RegisterAim("mmo_aim_left", JoyAxis.RightX, -1f);
        RegisterAim("mmo_aim_right", JoyAxis.RightX, 1f);
        RegisterAim("mmo_aim_up", JoyAxis.RightY, -1f);
        RegisterAim("mmo_aim_down", JoyAxis.RightY, 1f);
        RegisterAction("mmo_inventory", Key.I);
        RegisterAction("mmo_character", Key.C);
        RegisterAction("mmo_escape", Key.Escape);
    }

    private static void RegisterAim(string action, JoyAxis axis, float value)
    {
        if (InputMap.HasAction(action)) return;
        InputMap.AddAction(action, .2f);
        InputMap.ActionAddEvent(action, new InputEventJoypadMotion { Axis = axis, AxisValue = value });
    }

    private static void RegisterAction(string action, params Key[] keys)
    {
        if (InputMap.HasAction(action)) return;
        InputMap.AddAction(action, .2f);
        foreach (var key in keys)
            InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = key });
    }

    public override void _Process(double delta)
    {
        var typing = GetViewport().GuiGetFocusOwner() is LineEdit;
        inputs.SetUiFocus(typing);
        var input = TestInput ?? (focused && !typing ? Input.GetVector("mmo_left", "mmo_right", "mmo_up", "mmo_down") : Vector2.Zero);
        if (focused && Network.InWorld)
        {
            if (inputs.Gameplay.Attack || inputs.Combat.JustPressed)
                BasicAttackVisible();
            else if (inputs.Gameplay.HotbarIndex is { } hotbar)
                UseHotbar(hotbar);
            if (inputs.Gameplay.Interact)
                Network.Interact(inputs.Interaction.NearestInteractable(Network.World)?.Id ?? default);
            if (inputs.Gameplay.CycleTarget)
                CycleVisibleTarget();
            if (inputs.Gameplay.ClearTarget)
            {
                Network.World.Local.Target.Clear();
                Network.World.Local.Entity?.ClearTarget();
                Network.SetTarget(default);
            }
            if (Input.IsActionJustPressed("mmo_inventory") || Input.IsActionJustPressed("inventory"))
                gameHud.ToggleInventory();
            if (Input.IsActionJustPressed("mmo_character")) gameHud.ToggleCharacter();
            if (Input.IsActionJustPressed("mmo_escape")) gameHud.ToggleEscape();
        }
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
        gameHud.Present(Network);
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

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!focused || !Network.InWorld || inputs.Gameplay.Blocked) return;
        if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
        {
            SelectAtMouse();
            GetViewport().SetInputAsHandled();
        }
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
