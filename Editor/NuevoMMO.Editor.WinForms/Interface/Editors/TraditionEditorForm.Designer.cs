#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class TraditionEditorForm
{
    private IContainer? components;
    private TextBox visualKeyTextBox = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        visualKeyTextBox = new TextBox();
        SuspendLayout();
        Name = "TraditionEditorForm";
        Text = "Tradiciones / Clases";
        specificTabPage.Text = "Tradición";
        AddSpecificRow(0, "VisualKey", visualKeyTextBox);
        ResumeLayout(false);
    }
}
