using System.ComponentModel;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

[DesignerCategory("Form")]
public sealed partial class PropertiesDock : DockContent
{
    public PropertiesDock()
    {
        InitializeComponent();
        EditorTheme.ApplyWindow(this);
    }

    public object? SelectedObject
    {
        get => propertyGrid.SelectedObject;
        set => propertyGrid.SelectedObject = value;
    }

    public void RefreshSelectedObject() => propertyGrid.Refresh();
}
