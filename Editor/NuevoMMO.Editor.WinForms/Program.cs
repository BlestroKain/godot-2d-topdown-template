using System.Windows.Forms;
using NuevoMMO.Core;

namespace NuevoMMO.Editor;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var gameDataPath = GameDatabase.LocateSharedPath();
        var configuration = new EditorConfiguration
        {
            Mode = EditorMode.Offline,
            ContentPath = gameDataPath,
            ResourcesRoot = AssetCatalog.SharedRootFromRepo
        };

        var application = new EditorApplication(configuration);
        application.Content.BindPath(gameDataPath);
        try
        {
            if (File.Exists(gameDataPath))
                application.Content.Load(gameDataPath);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"No se pudo abrir {gameDataPath}:{Environment.NewLine}{exception.Message}",
                "NuevoMMO Editor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        Application.Run(new MainForm(application));
    }
}
