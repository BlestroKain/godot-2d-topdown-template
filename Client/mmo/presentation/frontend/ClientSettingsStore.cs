using Godot;

namespace NuevoMMO.GodotClient;

/// <summary>
/// Preferencias locales compartidas entre frontend e ingame. No contiene
/// configuración autoritativa de cuenta/personaje.
/// </summary>
public sealed class ClientSettingsStore
{
    private const string Path = "user://client_settings.cfg";
    private readonly ConfigFile config = new();

    public bool Fullscreen { get; private set; }
    public float MasterVolume { get; private set; } = 1f;
    public float UiScale { get; private set; } = 1f;

    public void Load()
    {
        if (config.Load(Path) == Error.Ok)
        {
            Fullscreen = config.GetValue("video", "fullscreen", false).AsBool();
            MasterVolume = Mathf.Clamp((float)config.GetValue("audio", "master_volume", 1d).AsDouble(), 0f, 1f);
            UiScale = Mathf.Clamp((float)config.GetValue("interface", "ui_scale", 1d).AsDouble(), .8f, 1.5f);
        }
        Apply();
    }

    public void SetFullscreen(bool value)
    {
        Fullscreen = value;
        ApplyVideo();
        Save();
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp(value, 0f, 1f);
        ApplyAudio();
        Save();
    }

    public void SetUiScale(float value)
    {
        UiScale = Mathf.Clamp(value, .8f, 1.5f);
        Save();
    }

    private void Apply()
    {
        ApplyVideo();
        ApplyAudio();
    }

    private void ApplyVideo()
    {
        DisplayServer.WindowSetMode(Fullscreen
            ? DisplayServer.WindowMode.Fullscreen
            : DisplayServer.WindowMode.Windowed);
    }

    private void ApplyAudio()
    {
        var bus = AudioServer.GetBusIndex("Master");
        if (bus < 0) return;
        var db = MasterVolume <= .001f ? -80f : Mathf.LinearToDb(MasterVolume);
        AudioServer.SetBusVolumeDb(bus, db);
    }

    private void Save()
    {
        config.SetValue("video", "fullscreen", Fullscreen);
        config.SetValue("audio", "master_volume", MasterVolume);
        config.SetValue("interface", "ui_scale", UiScale);
        config.Save(Path);
    }
}
