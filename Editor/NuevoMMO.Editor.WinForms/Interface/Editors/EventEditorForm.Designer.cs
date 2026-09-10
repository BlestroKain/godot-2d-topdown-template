#nullable enable
using System.ComponentModel;
using NuevoMMO.Core;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class EventEditorForm
{
    private IContainer? components;
    private ComboBox scopeCombo = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        scopeCombo = new ComboBox();
        SuspendLayout();
        Name = "EventEditorForm";
        Text = "Eventos";
        specificTabPage.Text = "Evento";
        FillEnum<EventScope>(scopeCombo);
        AddSpecificRow(0, "Ámbito", scopeCombo);
        ResumeLayout(false);
    }
}
