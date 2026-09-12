using Godot;
using NuevoMMO.Core;
using NuevoMMO.Network;

namespace NuevoMMO.GodotClient;

public enum FrontendStage
{
    Login,
    Register,
    ServerSelect,
    LoadingCharacters,
    CharacterSelect,
    CharacterCreate,
    Settings,
    LoadingWorld
}

/// <summary>
/// Shell pre-game inspirado en el flujo de un MMO clásico: cuenta -> servidor ->
/// personaje -> mundo. Solo coordina presentación; autenticación y personajes
/// siguen siendo autoritativos del servidor.
/// </summary>
public partial class FrontendFlowController : CanvasLayer
{
    private readonly ClientSettingsStore settings = new();
    private readonly Dictionary<DefinitionId, Button> traditionButtons = [];
    private NetworkBridge? network;
    private FrontendStage stage = FrontendStage.Login;
    private FrontendStage returnFromSettings = FrontendStage.Login;
    private CharacterSummary? selectedCharacter;
    private TraditionDefinition? selectedTradition;
    private bool waitingForCreatedCharacter;
    private bool settingsOverWorld;

    private Control root = null!;
    private Control loginScreen = null!;
    private Control registerScreen = null!;
    private Control serverScreen = null!;
    private Control characterScreen = null!;
    private Control createScreen = null!;
    private Control settingsScreen = null!;
    private Control loadingScreen = null!;
    private Label globalStatus = null!;
    private Label loadingLabel = null!;
    private VBoxContainer characterList = null!;
    private Label selectedName = null!;
    private Label selectedLocation = null!;
    private Button enterWorldButton = null!;
    private Button createCharacterButton = null!;
    private GridContainer traditionGrid = null!;
    private Label selectedTraditionName = null!;
    private Label selectedTraditionDescription = null!;
    private CharacterPreview createPreview = null!;
    private CharacterPreview selectPreview = null!;
    private CheckButton fullscreen = null!;
    private HSlider masterVolume = null!;
    private HSlider uiScale = null!;

    public override void _Ready()
    {
        root = GetNode<Control>("Root");
        loginScreen = GetNode<Control>("Root/ScreenStack/LoginScreen");
        registerScreen = GetNode<Control>("Root/ScreenStack/RegisterScreen");
        serverScreen = GetNode<Control>("Root/ScreenStack/ServerSelectScreen");
        characterScreen = GetNode<Control>("Root/ScreenStack/CharacterSelectScreen");
        createScreen = GetNode<Control>("Root/ScreenStack/CharacterCreateScreen");
        settingsScreen = GetNode<Control>("Root/ScreenStack/SettingsScreen");
        loadingScreen = GetNode<Control>("Root/ScreenStack/LoadingScreen");
        globalStatus = GetNode<Label>("Root/GlobalStatus");
        loadingLabel = GetNode<Label>("Root/ScreenStack/LoadingScreen/Panel/Margin/Content/LoadingLabel");
        characterList = GetNode<VBoxContainer>("Root/ScreenStack/CharacterSelectScreen/Panel/Margin/Content/Body/CharactersScroll/CharacterList");
        selectedName = GetNode<Label>("Root/ScreenStack/CharacterSelectScreen/Panel/Margin/Content/Body/Stage/SelectedName");
        selectedLocation = GetNode<Label>("Root/ScreenStack/CharacterSelectScreen/Panel/Margin/Content/Body/Stage/SelectedLocation");
        enterWorldButton = GetNode<Button>("Root/ScreenStack/CharacterSelectScreen/Panel/Margin/Content/Actions/EnterWorldButton");
        createCharacterButton = GetNode<Button>("Root/ScreenStack/CharacterCreateScreen/Panel/Margin/Content/Actions/CreateButton");
        fullscreen = GetNode<CheckButton>("Root/ScreenStack/SettingsScreen/Panel/Margin/Content/Fullscreen");
        masterVolume = GetNode<HSlider>("Root/ScreenStack/SettingsScreen/Panel/Margin/Content/MasterVolume");
        uiScale = GetNode<HSlider>("Root/ScreenStack/SettingsScreen/Panel/Margin/Content/UiScale");

        BuildCharacterPreviews();
        BuildTraditionPicker();
        WireButtons();
        settings.Load();
        fullscreen.ButtonPressed = settings.Fullscreen;
        masterVolume.Value = settings.MasterVolume * 100d;
        uiScale.Value = settings.UiScale * 100d;
        ShowStage(FrontendStage.Login);
    }

    public override void _Process(double delta)
    {
        if (network is null)
        {
            TryBindNetwork();
            return;
        }

        globalStatus.Text = network.Status;
        if (network.InWorld && !settingsOverWorld)
        {
            root.Hide();
            return;
        }

        root.Show();
    }

    /// <summary>Abre exactamente el mismo panel de preferencias encima del mundo.</summary>
    public void OpenInGameSettings()
    {
        if (network is null || !network.InWorld) return;
        settingsOverWorld = true;
        ShowStage(FrontendStage.Settings);
        root.Show();
    }

    private void TryBindNetwork()
    {
        if (GetParent() is not MmoGame game || game.Network is null) return;
        network = game.Network;
        network.ConnectionChanged += OnConnectionChanged;
        network.LobbyUpdated += OnLobbyUpdated;

        var legacy = game.GetNodeOrNull<CanvasLayer>("CanvasLayer");
        legacy?.Hide();
        OnLobbyUpdated();
    }

    private void BuildCharacterPreviews()
    {
        var selectStage = GetNode<VBoxContainer>("Root/ScreenStack/CharacterSelectScreen/Panel/Margin/Content/Body/Stage/StageContent");
        var oldSelectPreview = selectStage.GetNodeOrNull<Label>("Preview");
        if (oldSelectPreview is not null)
        {
            selectStage.RemoveChild(oldSelectPreview);
            oldSelectPreview.QueueFree();
        }
        selectPreview = new CharacterPreview
        {
            Name = "CharacterPreview",
            CustomMinimumSize = new Vector2(0, 250),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        selectStage.AddChild(selectPreview);
        selectStage.MoveChild(selectPreview, 1);
        selectPreview.Present(CanonicalCharacterAppearance.Default);

        var createContent = GetNode<VBoxContainer>("Root/ScreenStack/CharacterCreateScreen/Panel/Margin/Content");
        createPreview = new CharacterPreview
        {
            Name = "CharacterPreview",
            CustomMinimumSize = new Vector2(0, 170),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        createContent.AddChild(createPreview);
        createContent.MoveChild(createPreview, 3);
        createPreview.Present(CanonicalCharacterAppearance.Default);
    }

    private void BuildTraditionPicker()
    {
        var preview = GetNode<PanelContainer>("Root/ScreenStack/CharacterCreateScreen/Panel/Margin/Content/Preview");
        var oldPreview = preview.GetNodeOrNull<Label>("PreviewText");
        if (oldPreview is not null)
        {
            preview.RemoveChild(oldPreview);
            oldPreview.QueueFree();
        }

        GetNode<Label>("Root/ScreenStack/CharacterCreateScreen/Panel/Margin/Content/Rule").Text =
            "El personaje nace Novicio. La Tradición es opcional y no otorga kit al crear. No hay selector de raza; el arma no define la clase.";
        GetNode<Label>("Root/ScreenStack/CharacterCreateScreen/Panel/Margin/Content/AppearanceNotice").Text =
            "El preview usa el renderer real. Cuerpo, rostro, cabello, ojos, orejas, cuernos, pigmentos y marcas ya tienen contrato persistente; se habilitan al publicar sus sprites.";

        var picker = GD.Load<PackedScene>("res://mmo/presentation/frontend/tradition_picker.tscn").Instantiate<Control>();
        preview.AddChild(picker);
        traditionGrid = picker.GetNode<GridContainer>("VBox/TraditionsScroll/TraditionGrid");
        selectedTraditionName = picker.GetNode<Label>("VBox/Selection/SelectionMargin/SelectionVBox/SelectedTradition");
        selectedTraditionDescription = picker.GetNode<Label>("VBox/Selection/SelectionMargin/SelectionVBox/TraditionDescription");

        foreach (var tradition in CanonicalTraditions.All)
        {
            var current = tradition;
            var button = new Button
            {
                Text = current.Name,
                ToggleMode = true,
                CustomMinimumSize = new Vector2(0, 42),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                TooltipText = current.Description ?? current.Name
            };
            button.Pressed += () => SelectTradition(current);
            traditionGrid.AddChild(button);
            traditionButtons[current.Id] = button;
        }

        ResetTraditionSelection();
    }

    private void WireButtons()
    {
        Button("Root/ScreenStack/LoginScreen/Panel/Margin/Content/LoginButton").Pressed += SubmitLogin;
        Button("Root/ScreenStack/LoginScreen/Panel/Margin/Content/RegisterButton").Pressed += () => ShowStage(FrontendStage.Register);
        Button("Root/ScreenStack/LoginScreen/Panel/Margin/Content/SettingsButton").Pressed += () => OpenSettings(FrontendStage.Login);
        Button("Root/ScreenStack/LoginScreen/Panel/Margin/Content/QuitButton").Pressed += () => GetTree().Quit();

        Button("Root/ScreenStack/RegisterScreen/Panel/Margin/Content/CreateAccountButton").Pressed += SubmitRegistration;
        Button("Root/ScreenStack/RegisterScreen/Panel/Margin/Content/BackButton").Pressed += () => ShowStage(FrontendStage.Login);

        Button("Root/ScreenStack/ServerSelectScreen/Panel/Margin/Content/ServerCard").Pressed += SelectServer;
        Button("Root/ScreenStack/ServerSelectScreen/Panel/Margin/Content/SettingsButton").Pressed += () => OpenSettings(FrontendStage.ServerSelect);
        Button("Root/ScreenStack/ServerSelectScreen/Panel/Margin/Content/LogoutButton").Pressed += Logout;

        Button("Root/ScreenStack/CharacterSelectScreen/Panel/Margin/Content/Actions/CreateCharacterButton").Pressed += () => ShowStage(FrontendStage.CharacterCreate);
        enterWorldButton.Pressed += EnterWorld;
        Button("Root/ScreenStack/CharacterSelectScreen/Panel/Margin/Content/Actions/SettingsButton").Pressed += () => OpenSettings(FrontendStage.CharacterSelect);
        Button("Root/ScreenStack/CharacterSelectScreen/Panel/Margin/Content/Actions/LogoutButton").Pressed += Logout;

        createCharacterButton.Pressed += CreateCharacter;
        Button("Root/ScreenStack/CharacterCreateScreen/Panel/Margin/Content/Actions/BackButton").Pressed += () => ShowStage(FrontendStage.CharacterSelect);

        Button("Root/ScreenStack/SettingsScreen/Panel/Margin/Content/BackButton").Pressed += CloseSettings;
        fullscreen.Toggled += settings.SetFullscreen;
        masterVolume.ValueChanged += value => settings.SetMasterVolume((float)value / 100f);
        uiScale.ValueChanged += value => settings.SetUiScale((float)value / 100f);
    }

    private Button Button(string path) => GetNode<Button>(path);

    private void SubmitLogin()
    {
        if (network is null || network.LobbyBusy) return;
        var host = GetNode<LineEdit>("Root/ScreenStack/LoginScreen/Panel/Margin/Content/Host").Text.Trim();
        var username = GetNode<LineEdit>("Root/ScreenStack/LoginScreen/Panel/Margin/Content/Username").Text.Trim();
        var password = GetNode<LineEdit>("Root/ScreenStack/LoginScreen/Panel/Margin/Content/Password").Text;
        if (username.Length < 3 || password.Length < 8)
        {
            globalStatus.Text = "Usuario o contraseña no cumplen los requisitos.";
            return;
        }
        GetViewport().GuiReleaseFocus();
        network.LoginLobby(host, 7777, username, password);
    }

    private void SubmitRegistration()
    {
        if (network is null || network.LobbyBusy) return;
        var host = GetNode<LineEdit>("Root/ScreenStack/RegisterScreen/Panel/Margin/Content/Host").Text.Trim();
        var username = GetNode<LineEdit>("Root/ScreenStack/RegisterScreen/Panel/Margin/Content/Username").Text.Trim();
        var password = GetNode<LineEdit>("Root/ScreenStack/RegisterScreen/Panel/Margin/Content/Password").Text;
        var confirm = GetNode<LineEdit>("Root/ScreenStack/RegisterScreen/Panel/Margin/Content/ConfirmPassword").Text;
        if (password != confirm)
        {
            globalStatus.Text = "Las contraseñas no coinciden.";
            return;
        }
        if (username.Length < 3 || password.Length < 8)
        {
            globalStatus.Text = "Usuario: 3–24 caracteres. Contraseña: mínimo 8.";
            return;
        }
        GetViewport().GuiReleaseFocus();
        network.RegisterLobby(host, 7777, username, password);
    }

    private void SelectServer()
    {
        if (network is null || !network.Authenticated || network.LobbyBusy) return;
        ShowLoading("Cargando personajes…", FrontendStage.LoadingCharacters);
        network.RequestCharacters();
    }

    private void CreateCharacter()
    {
        if (network is null || network.LobbyBusy) return;
        var name = GetNode<LineEdit>("Root/ScreenStack/CharacterCreateScreen/Panel/Margin/Content/Name").Text.Trim();
        if (name.Length is < 2 or > 24)
        {
            globalStatus.Text = "El nombre debe tener entre 2 y 24 caracteres.";
            return;
        }
        waitingForCreatedCharacter = true;
        var traditionId = selectedTradition?.Id ?? default;
        network.CreateCharacter(name, traditionId);
        globalStatus.Text = selectedTradition is null
            ? $"Creando {name} · Novicio…"
            : $"Creando {name} · {selectedTradition.Name}…";
    }

    private void SelectTradition(TraditionDefinition tradition)
    {
        selectedTradition = tradition;
        foreach (var pair in traditionButtons)
            pair.Value.ButtonPressed = pair.Key == tradition.Id;
        selectedTraditionName.Text = tradition.Name;
        selectedTraditionDescription.Text = tradition.Description ?? string.Empty;
        createCharacterButton.Disabled = false;
    }

    private void ResetTraditionSelection()
    {
        selectedTradition = null;
        foreach (var button in traditionButtons.Values)
            button.ButtonPressed = false;
        if (selectedTraditionName is not null)
            selectedTraditionName.Text = "Ninguna Tradición seleccionada";
        if (selectedTraditionDescription is not null)
            selectedTraditionDescription.Text = "Opcional. Si no eliges, el personaje entra al mundo como Novicio.";
        if (createCharacterButton is not null)
            createCharacterButton.Disabled = false;
    }

    private void EnterWorld()
    {
        if (network is null || selectedCharacter is null || network.LobbyBusy) return;
        ShowLoading($"Preparando a {selectedCharacter.Name}…", FrontendStage.LoadingWorld);
        network.EnterWorld(selectedCharacter.Id);
    }

    private void Logout()
    {
        settingsOverWorld = false;
        network?.DisconnectFromServer();
        selectedCharacter = null;
        ClearCharacterList();
        ShowStage(FrontendStage.Login);
    }

    private void OpenSettings(FrontendStage returnStage)
    {
        settingsOverWorld = false;
        returnFromSettings = returnStage;
        ShowStage(FrontendStage.Settings);
    }

    private void CloseSettings()
    {
        if (settingsOverWorld)
        {
            settingsOverWorld = false;
            root.Hide();
            return;
        }
        ShowStage(returnFromSettings);
    }

    private void OnConnectionChanged(string state) => globalStatus.Text = state;

    private void OnLobbyUpdated()
    {
        if (network is null) return;
        globalStatus.Text = network.Status;

        if (!network.Connected && !network.Authenticated)
        {
            settingsOverWorld = false;
            selectedCharacter = null;
            ShowStage(FrontendStage.Login);
            root.Show();
            return;
        }

        if (network.InWorld)
        {
            if (!settingsOverWorld) root.Hide();
            return;
        }

        if (network.Authenticated && stage is FrontendStage.Login or FrontendStage.Register)
        {
            ShowStage(FrontendStage.ServerSelect);
            return;
        }

        if (stage == FrontendStage.LoadingCharacters && !network.LobbyBusy)
        {
            RebuildCharacters();
            ShowStage(FrontendStage.CharacterSelect);
            return;
        }

        if (waitingForCreatedCharacter && !network.LobbyBusy)
        {
            waitingForCreatedCharacter = false;
            RebuildCharacters();
            if (network.Characters.Length > 0) SelectCharacter(network.Characters[^1]);
            ShowStage(FrontendStage.CharacterSelect);
        }
    }

    private void RebuildCharacters()
    {
        ClearCharacterList();
        if (network is null) return;

        if (network.Characters.Length == 0)
        {
            var empty = new Label
            {
                Text = "Aún no hay personajes. Crea el primero para comenzar.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            };
            characterList.AddChild(empty);
            selectedCharacter = null;
            selectedName.Text = "Sin personaje seleccionado";
            selectedLocation.Text = "";
            selectPreview.Present(CanonicalCharacterAppearance.Default);
            enterWorldButton.Disabled = true;
            return;
        }

        foreach (var character in network.Characters)
        {
            var traditionName = CanonicalTraditions.DisplayName(character.TraditionId);
            var button = new Button
            {
                Text = $"{character.Name}\n{traditionName}",
                CustomMinimumSize = new Vector2(0, 64),
                FocusMode = Control.FocusModeEnum.All,
                TooltipText = character.TraditionId.IsEmpty ? "Novicio" : traditionName
            };
            button.Pressed += () => SelectCharacter(character);
            characterList.AddChild(button);
        }

        SelectCharacter(network.Characters[0]);
    }

    private void SelectCharacter(CharacterSummary character)
    {
        selectedCharacter = character;
        selectedName.Text = character.Name;
        selectedLocation.Text =
            $"Tradición: {CanonicalTraditions.DisplayName(character.TraditionId)}\nUbicación: {character.MapDefinition.Value}";
        selectPreview.Present(character.Appearance);
        enterWorldButton.Disabled = false;
    }

    private void ClearCharacterList()
    {
        foreach (var child in characterList.GetChildren())
        {
            characterList.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void ShowLoading(string text, FrontendStage loadingStage)
    {
        loadingLabel.Text = text;
        ShowStage(loadingStage);
    }

    private void ShowStage(FrontendStage next)
    {
        if (next == FrontendStage.CharacterCreate && stage != FrontendStage.CharacterCreate)
        {
            ResetTraditionSelection();
            createPreview.Present(CanonicalCharacterAppearance.Default);
            GetNode<LineEdit>("Root/ScreenStack/CharacterCreateScreen/Panel/Margin/Content/Name").Text = string.Empty;
        }

        stage = next;
        loginScreen.Visible = next == FrontendStage.Login;
        registerScreen.Visible = next == FrontendStage.Register;
        serverScreen.Visible = next == FrontendStage.ServerSelect;
        characterScreen.Visible = next == FrontendStage.CharacterSelect;
        createScreen.Visible = next == FrontendStage.CharacterCreate;
        settingsScreen.Visible = next == FrontendStage.Settings;
        loadingScreen.Visible = next is FrontendStage.LoadingCharacters or FrontendStage.LoadingWorld;
    }
}
