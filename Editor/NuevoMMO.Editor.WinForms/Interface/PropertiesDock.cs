using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

public sealed class PropertiesDock : DockContent
{
    private readonly PropertyGrid propertyGrid = new()
    {
        Dock = DockStyle.Fill,
        HelpVisible = true,
        ToolbarVisible = true
    };

    public PropertiesDock()
    {
        Text = "Propiedades";
        TabText = Text;
        HideOnClose = true;
        Controls.Add(propertyGrid);
    }

    public object? SelectedObject
    {
        get => propertyGrid.SelectedObject;
        set => propertyGrid.SelectedObject = value;
    }

    public void RefreshSelectedObject() => propertyGrid.Refresh();
}
