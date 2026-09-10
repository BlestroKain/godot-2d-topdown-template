using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

public sealed class ProblemsDock : DockContent
{
    private readonly ListBox list = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true };

    public ProblemsDock()
    {
        Text = "Problemas";
        TabText = Text;
        HideOnClose = true;
        Controls.Add(list);
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
