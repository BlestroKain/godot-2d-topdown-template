#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class ProfessionEditorForm
{
    private IContainer? components;

    private TextBox dimensionsTextBox = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();

        dimensionsTextBox = new TextBox();
        SuspendLayout();
        Name = "ProfessionEditorForm";
        Text = "Profesiones";
        specificTabPage.Text = "Profesión";
        AddSpecificRow(0, "Dimensiones", dimensionsTextBox);
        ResumeLayout(false);
    }
}
