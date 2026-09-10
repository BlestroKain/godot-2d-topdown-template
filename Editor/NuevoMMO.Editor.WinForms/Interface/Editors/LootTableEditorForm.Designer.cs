#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class LootTableEditorForm
{
    private IContainer? components;
    private Label entriesHelpLabel = null!;
    private TextBox entriesTextBox = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        entriesHelpLabel = new Label { AutoSize = true, Text = "Una línea: claveItem, chance%, min, max" };
        entriesTextBox = new TextBox { AcceptsReturn = true, Multiline = true, ScrollBars = ScrollBars.Vertical };
        SuspendLayout();
        Name = "LootTableEditorForm";
        Text = "Loot Tables";
        specificTabPage.Text = "Loot";
        AddSpecificRow(0, "Formato", entriesHelpLabel);
        AddSpecificRow(1, "Entradas", entriesTextBox, 180);
        ResumeLayout(false);
    }
}
