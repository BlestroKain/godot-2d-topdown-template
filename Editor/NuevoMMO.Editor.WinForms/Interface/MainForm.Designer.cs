using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

partial class MainForm
{
    private IContainer? components;
    private MenuStrip mainMenuStrip = null!;
    private StatusStrip mainStatusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;
    private DockPanel dockPanel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            images?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        mainMenuStrip = new MenuStrip();
        mainStatusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel("Listo");
        dockPanel = new DockPanel();

        SuspendLayout();
        mainMenuStrip.SuspendLayout();
        mainStatusStrip.SuspendLayout();

        mainMenuStrip.Dock = DockStyle.Top;
        mainMenuStrip.Name = "mainMenuStrip";
        mainMenuStrip.Size = new Size(1440, 24);

        mainStatusStrip.Dock = DockStyle.Bottom;
        mainStatusStrip.Items.Add(statusLabel);
        mainStatusStrip.Name = "mainStatusStrip";
        mainStatusStrip.Size = new Size(1440, 22);

        dockPanel.Dock = DockStyle.Fill;
        dockPanel.DocumentStyle = DocumentStyle.DockingWindow;
        dockPanel.Name = "dockPanel";

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1440, 900);
        Controls.Add(dockPanel);
        Controls.Add(mainStatusStrip);
        Controls.Add(mainMenuStrip);
        MainMenuStrip = mainMenuStrip;
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "NuevoMMO Editor";
        WindowState = FormWindowState.Maximized;

        mainStatusStrip.ResumeLayout(false);
        mainStatusStrip.PerformLayout();
        mainMenuStrip.ResumeLayout(false);
        mainMenuStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
