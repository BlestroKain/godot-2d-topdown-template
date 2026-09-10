#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class ProfessionEditorForm
{
    private IContainer? components;
    private TextBox visualKeyTextBox = null!;
    private TextBox dimensionsTextBox = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        visualKeyTextBox = new TextBox();
        dimensionsTextBox = new TextBox();
        SuspendLayout();
        Name = "ProfessionEditorForm";
        Text = "Profesiones";
        specificTabPage.Text = "Profesión";
        AddSpecificRow(0, "VisualKey", visualKeyTextBox);
        AddSpecificRow(1, "Dimensiones", dimensionsTextBox);
        ResumeLayout(false);
    }
}
