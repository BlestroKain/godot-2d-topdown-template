#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class QuestEditorForm
{
    private IContainer? components;
    private TextBox startTextBox = null!;
    private TextBox inProgressTextBox = null!;
    private TextBox endTextBox = null!;
    private CheckBox repeatableCheck = null!;
    private CheckBox quitableCheck = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        startTextBox = new TextBox { AcceptsReturn = true, Multiline = true, ScrollBars = ScrollBars.Vertical };
        inProgressTextBox = new TextBox { AcceptsReturn = true, Multiline = true, ScrollBars = ScrollBars.Vertical };
        endTextBox = new TextBox { AcceptsReturn = true, Multiline = true, ScrollBars = ScrollBars.Vertical };
        repeatableCheck = new CheckBox { AutoSize = true, Text = "Repetible" };
        quitableCheck = new CheckBox { AutoSize = true, Text = "Se puede abandonar", Checked = true };
        SuspendLayout();
        Name = "QuestEditorForm";
        Text = "Quests";
        specificTabPage.Text = "Quest";
        AddSpecificRow(0, "Inicio", startTextBox, 70);
        AddSpecificRow(1, "En curso", inProgressTextBox, 70);
        AddSpecificRow(2, "Final", endTextBox, 70);
        AddSpecificRow(3, "Repetible", repeatableCheck);
        AddSpecificRow(4, "Abandonable", quitableCheck);
        ResumeLayout(false);
    }
}
