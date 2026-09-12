#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class TraditionEditorForm
{
    private IContainer? components;


    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();

        SuspendLayout();
        Name = "TraditionEditorForm";
        Text = "Tradiciones / Clases";
        specificTabPage.Text = "Tradición";
        ResumeLayout(false);
    }
}
