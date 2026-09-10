using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

partial class DefinitionEditorForm
{
    private IContainer? components;
    private ToolStrip editorToolStrip = null!;
    private ToolStripButton newButton = null!;
    private ToolStripButton duplicateButton = null!;
    private ToolStripButton deleteButton = null!;
    private ToolStripButton saveButton = null!;
    private ToolStripButton refreshButton = null!;
    private ToolStripTextBox searchTextBox = null!;
    private SplitContainer editorSplitContainer = null!;
    private ListBox definitionsListBox = null!;
    private TabControl editorTabs = null!;
    private TabPage generalTabPage = null!;
    private TabPage jsonTabPage = null!;
    private TableLayoutPanel generalTable = null!;
    private TextBox idTextBox = null!;
    private TextBox keyTextBox = null!;
    private TextBox nameTextBox = null!;
    private TextBox descriptionTextBox = null!;
    private CheckBox enabledCheckBox = null!;
    private NumericUpDown versionNumeric = null!;
    private TextBox tagsTextBox = null!;
    private TextBox jsonTextBox = null!;
    private Label editorStatusLabel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        editorToolStrip = new ToolStrip();
        newButton = new ToolStripButton("Nuevo");
        duplicateButton = new ToolStripButton("Duplicar");
        deleteButton = new ToolStripButton("Eliminar");
        saveButton = new ToolStripButton("Guardar");
        refreshButton = new ToolStripButton("Actualizar");
        searchTextBox = new ToolStripTextBox { AutoSize = false, Width = 220 };
        editorSplitContainer = new SplitContainer();
        definitionsListBox = new ListBox();
        editorTabs = new TabControl();
        generalTabPage = new TabPage("General");
        jsonTabPage = new TabPage("Datos avanzados (JSON)");
        generalTable = new TableLayoutPanel();
        idTextBox = new TextBox { ReadOnly = true };
        keyTextBox = new TextBox();
        nameTextBox = new TextBox();
        descriptionTextBox = new TextBox { AcceptsReturn = true, Multiline = true, ScrollBars = ScrollBars.Vertical };
        enabledCheckBox = new CheckBox { AutoSize = true, Text = "Disponible" };
        versionNumeric = new NumericUpDown { Minimum = 1, Maximum = 1000000, Value = 1 };
        tagsTextBox = new TextBox();
        jsonTextBox = new TextBox
        {
            AcceptsReturn = true,
            AcceptsTab = true,
            Dock = DockStyle.Fill,
            Font = new Font("Consolas", 9F),
            Multiline = true,
            ScrollBars = ScrollBars.Both,
            WordWrap = false
        };
        editorStatusLabel = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Bottom,
            ForeColor = Color.Gainsboro,
            Height = 24,
            Padding = new Padding(6, 4, 6, 0),
            Text = "Seleccione una definición."
        };

        SuspendLayout();
        editorToolStrip.SuspendLayout();
        ((ISupportInitialize)editorSplitContainer).BeginInit();
        editorSplitContainer.Panel1.SuspendLayout();
        editorSplitContainer.Panel2.SuspendLayout();
        editorSplitContainer.SuspendLayout();
        editorTabs.SuspendLayout();
        generalTabPage.SuspendLayout();
        jsonTabPage.SuspendLayout();
        generalTable.SuspendLayout();
        ((ISupportInitialize)versionNumeric).BeginInit();

        editorToolStrip.GripStyle = ToolStripGripStyle.Hidden;
        editorToolStrip.Items.AddRange([
            newButton, duplicateButton, deleteButton,
            new ToolStripSeparator(), saveButton, refreshButton,
            new ToolStripSeparator(), new ToolStripLabel("Buscar:"), searchTextBox
        ]);
        editorToolStrip.Dock = DockStyle.Top;

        editorSplitContainer.Dock = DockStyle.Fill;
        editorSplitContainer.FixedPanel = FixedPanel.Panel1;
        editorSplitContainer.SplitterDistance = 280;
        editorSplitContainer.Panel1.Controls.Add(definitionsListBox);
        editorSplitContainer.Panel2.Controls.Add(editorTabs);
        editorSplitContainer.Panel2.Controls.Add(editorStatusLabel);

        definitionsListBox.Dock = DockStyle.Fill;
        definitionsListBox.IntegralHeight = false;

        editorTabs.Dock = DockStyle.Fill;
        editorTabs.Controls.Add(generalTabPage);
        editorTabs.Controls.Add(jsonTabPage);

        generalTabPage.Controls.Add(generalTable);
        generalTabPage.Padding = new Padding(8);
        generalTable.Dock = DockStyle.Fill;
        generalTable.AutoScroll = true;
        generalTable.ColumnCount = 2;
        generalTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
        generalTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        generalTable.RowCount = 7;
        generalTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        generalTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        generalTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        generalTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        generalTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        generalTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        generalTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));

        AddGeneralRow(0, "ID", idTextBox);
        AddGeneralRow(1, "Clave", keyTextBox);
        AddGeneralRow(2, "Nombre", nameTextBox);
        AddGeneralRow(3, "Descripción", descriptionTextBox);
        AddGeneralRow(4, "Estado", enabledCheckBox);
        AddGeneralRow(5, "Versión", versionNumeric);
        AddGeneralRow(6, "Etiquetas", tagsTextBox);

        jsonTabPage.Controls.Add(jsonTextBox);
        jsonTabPage.Padding = new Padding(8);

        BackColor = Color.FromArgb(37, 37, 38);
        ClientSize = new Size(960, 640);
        Controls.Add(editorSplitContainer);
        Controls.Add(editorToolStrip);
        HideOnClose = true;
        Name = "DefinitionEditorForm";
        ShowHint = WeifenLuo.WinFormsUI.Docking.DockState.Document;
        Text = "Editor de contenido";

        ((ISupportInitialize)versionNumeric).EndInit();
        generalTable.ResumeLayout(false);
        generalTable.PerformLayout();
        jsonTabPage.ResumeLayout(false);
        jsonTabPage.PerformLayout();
        generalTabPage.ResumeLayout(false);
        editorTabs.ResumeLayout(false);
        editorSplitContainer.Panel2.ResumeLayout(false);
        editorSplitContainer.Panel1.ResumeLayout(false);
        ((ISupportInitialize)editorSplitContainer).EndInit();
        editorSplitContainer.ResumeLayout(false);
        editorToolStrip.ResumeLayout(false);
        editorToolStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private void AddGeneralRow(int row, string label, Control control)
    {
        var caption = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 7, 0, 0),
            Text = label,
            TextAlign = ContentAlignment.TopLeft
        };
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(3, 4, 3, 4);
        generalTable.Controls.Add(caption, 0, row);
        generalTable.Controls.Add(control, 1, row);
    }
}
