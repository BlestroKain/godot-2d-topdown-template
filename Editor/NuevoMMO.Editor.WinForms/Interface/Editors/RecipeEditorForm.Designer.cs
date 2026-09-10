#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class RecipeEditorForm
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
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(960, 640);
        Name = "RecipeEditorForm";
        Text = "Recetas";
        ResumeLayout(false);
    }
}
