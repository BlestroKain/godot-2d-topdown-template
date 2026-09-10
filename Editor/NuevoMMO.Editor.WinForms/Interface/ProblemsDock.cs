using System.ComponentModel;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

[DesignerCategory("Form")]
public sealed partial class ProblemsDock : DockContent
{
    public ProblemsDock()
    {
        InitializeComponent();
    }

    public void SetProblems(IEnumerable<string> problems)
    {
        list.BeginUpdate();
        list.Items.Clear();
        foreach (var problem in problems) list.Items.Add(problem);
        if (list.Items.Count == 0) list.Items.Add("Sin problemas de validación.");
        list.EndUpdate();
    }
}
