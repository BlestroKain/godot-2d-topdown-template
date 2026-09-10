using System.Windows.Forms;

namespace NuevoMMO.Editor;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var configuration = new EditorConfiguration
        {
            Mode = EditorMode.Offline,
            ContentPath = Path.Combine("Data", GameDatabase.DefaultFileName),
            ClientAssetRoot = "Client",
            TilesetAssetFolder = "tilesets"
        };

        var application = new EditorApplication(configuration);
        var defaultDatabase = Path.GetFullPath(configuration.ContentPath);
        try
        {
            if (File.Exists(defaultDatabase))
                application.Content.Load(defaultDatabase);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"No se pudo abrir {defaultDatabase}:{Environment.NewLine}{exception.Message}",
                "NuevoMMO Editor",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        Application.Run(new MainForm(application));
    }
}
