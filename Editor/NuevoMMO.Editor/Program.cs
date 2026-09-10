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
            ContentPath = "gamedata"
        };

        var application = new EditorApplication(configuration);
        Application.Run(new MainEditorForm(application));
    }
}
