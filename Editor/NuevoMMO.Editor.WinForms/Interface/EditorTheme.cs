using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

/// <summary>
/// Paleta oscura al estilo DarkUI de Broken Reborn. Se aplica a toda la UI WinForms
/// para que no quede ningún panel blanco de sistema.
/// </summary>
public static class EditorTheme
{
    public static readonly Color Background = Color.FromArgb(30, 30, 30);
    public static readonly Color Surface = Color.FromArgb(37, 37, 38);
    public static readonly Color SurfaceAlt = Color.FromArgb(45, 45, 48);
    public static readonly Color Input = Color.FromArgb(27, 27, 28);
    public static readonly Color Border = Color.FromArgb(63, 63, 70);
    public static readonly Color Text = Color.FromArgb(220, 220, 220);
    public static readonly Color Muted = Color.FromArgb(160, 160, 160);
    public static readonly Color Accent = Color.FromArgb(0, 122, 204);

    public static void ApplyWindow(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = Text;
        Apply(form);
    }

    public static void Apply(Control root)
    {
        ArgumentNullException.ThrowIfNull(root);
        Paint(root);
        foreach (Control child in root.Controls)
            Apply(child);
    }

    private static void Paint(Control control)
    {
        switch (control)
        {
            case PropertyGrid grid:
                grid.BackColor = Surface;
                grid.ForeColor = Text;
                grid.ViewBackColor = Input;
                grid.ViewForeColor = Text;
                grid.HelpBackColor = Surface;
                grid.HelpForeColor = Text;
                grid.LineColor = Border;
                grid.CategoryForeColor = Text;
                grid.CommandsBackColor = Surface;
                grid.CommandsForeColor = Text;
                break;
            case TextBox or RichTextBox or NumericUpDown or MaskedTextBox:
                control.BackColor = Input;
                control.ForeColor = Text;
                break;
            case ListBox or TreeView or ListView or ComboBox:
                control.BackColor = Input;
                control.ForeColor = Text;
                break;
            case TabControl tabs:
                tabs.BackColor = Surface;
                tabs.ForeColor = Text;
                break;
            case TabPage page:
                page.BackColor = Surface;
                page.ForeColor = Text;
                page.UseVisualStyleBackColor = false;
                break;
            case SplitContainer splitter:
                splitter.BackColor = Border;
                splitter.Panel1.BackColor = Surface;
                splitter.Panel2.BackColor = Surface;
                break;
            case MenuStrip or ToolStrip or StatusStrip:
                control.BackColor = SurfaceAlt;
                control.ForeColor = Text;
                if (control is ToolStrip strip)
                {
                    strip.Renderer = new DarkToolStripRenderer();
                    foreach (ToolStripItem item in strip.Items)
                        PaintItem(item);
                }
                break;
            case Button button:
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = Border;
                button.BackColor = SurfaceAlt;
                button.ForeColor = Text;
                button.UseVisualStyleBackColor = false;
                break;
            case CheckBox or RadioButton or Label:
                control.BackColor = Color.Transparent;
                control.ForeColor = Text;
                if (control.Parent is not null && control.BackColor == Color.Transparent)
                    control.ForeColor = Text;
                break;
            case TableLayoutPanel or Panel or FlowLayoutPanel or GroupBox:
                control.BackColor = Surface;
                control.ForeColor = Text;
                break;
            default:
                if (control.BackColor.Name is "Control" or "Window" or "White" ||
                    control.BackColor.R > 240 && control.BackColor.G > 240 && control.BackColor.B > 240)
                    control.BackColor = Surface;
                if (control.ForeColor.Name is "ControlText" or "WindowText" or "Black")
                    control.ForeColor = Text;
                break;
        }
    }

    private static void PaintItem(ToolStripItem item)
    {
        item.ForeColor = Text;
        item.BackColor = SurfaceAlt;
        if (item is ToolStripTextBox text)
        {
            text.BackColor = Input;
            text.ForeColor = Text;
            text.BorderStyle = BorderStyle.FixedSingle;
        }

        if (item is ToolStripDropDownItem drop)
        {
            foreach (ToolStripItem child in drop.DropDownItems)
                PaintItem(child);
        }
    }

    private sealed class DarkToolStripRenderer : ToolStripProfessionalRenderer
    {
        public DarkToolStripRenderer() : base(new DarkColorTable()) { }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e) { }
    }

    private sealed class DarkColorTable : ProfessionalColorTable
    {
        public override Color MenuStripGradientBegin => SurfaceAlt;
        public override Color MenuStripGradientEnd => SurfaceAlt;
        public override Color ToolStripGradientBegin => SurfaceAlt;
        public override Color ToolStripGradientMiddle => SurfaceAlt;
        public override Color ToolStripGradientEnd => SurfaceAlt;
        public override Color MenuItemSelected => Accent;
        public override Color MenuItemSelectedGradientBegin => Accent;
        public override Color MenuItemSelectedGradientEnd => Accent;
        public override Color MenuItemPressedGradientBegin => Border;
        public override Color MenuItemPressedGradientEnd => Border;
        public override Color ImageMarginGradientBegin => Surface;
        public override Color ImageMarginGradientMiddle => Surface;
        public override Color ImageMarginGradientEnd => Surface;
        public override Color ToolStripDropDownBackground => Surface;
        public override Color MenuBorder => Border;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
        public override Color StatusStripGradientBegin => SurfaceAlt;
        public override Color StatusStripGradientEnd => SurfaceAlt;
        public override Color ButtonSelectedHighlight => Accent;
        public override Color ButtonCheckedHighlight => Accent;
    }
}
