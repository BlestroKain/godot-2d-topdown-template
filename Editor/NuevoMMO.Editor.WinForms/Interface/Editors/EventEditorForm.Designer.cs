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
    private ListBox pagesList = null!;
    private Button addPageButton = null!;
    private Button removePageButton = null!;
    private ComboBox triggerCombo = null!;
    private NumericUpDown priorityNumeric = null!;
    private NumericUpDown radiusNumeric = null!;
    private ListBox commandsList = null!;
    private ComboBox commandKindCombo = null!;
    private Button addCommandButton = null!;
    private Button removeCommandButton = null!;
    private TextBox commandTextBox = null!;
    private TextBox commandKeyBox = null!;
    private NumericUpDown commandValueNumeric = null!;
    private NumericUpDown commandXNumeric = null!;
    private NumericUpDown commandYNumeric = null!;
    private ComboBox commandItemCombo = null!;
    private ComboBox commandTechniqueCombo = null!;
    private Button applyCommandButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        scopeCombo = new ComboBox();
        pagesList = new ListBox();
        addPageButton = new Button { Text = "Página +" };
        removePageButton = new Button { Text = "Página −" };
        triggerCombo = new ComboBox();
        priorityNumeric = new NumericUpDown { Minimum = 0, Maximum = 1000 };
        radiusNumeric = new NumericUpDown { Minimum = 0, Maximum = 512, DecimalPlaces = 0 };
        commandsList = new ListBox();
        commandKindCombo = new ComboBox();
        addCommandButton = new Button { Text = "Comando +" };
        removeCommandButton = new Button { Text = "Comando −" };
        commandTextBox = new TextBox { Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
        commandKeyBox = new TextBox();
        commandValueNumeric = new NumericUpDown { Minimum = -1000000, Maximum = 1000000, DecimalPlaces = 2 };
        commandXNumeric = new NumericUpDown { Minimum = -100000, Maximum = 100000, DecimalPlaces = 1 };
        commandYNumeric = new NumericUpDown { Minimum = -100000, Maximum = 100000, DecimalPlaces = 1 };
        commandItemCombo = new ComboBox();
        commandTechniqueCombo = new ComboBox();
        applyCommandButton = new Button { Text = "Aplicar comando" };

        SuspendLayout();
        Name = "EventEditorForm";
        Text = "Eventos";
        specificTabPage.Text = "Evento";
        FillEnum<EventScope>(scopeCombo);
        FillEnum<EventTrigger>(triggerCombo);
        FillEnum<EventCommandKind>(commandKindCombo);

        var pageButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        pageButtons.Controls.Add(addPageButton);
        pageButtons.Controls.Add(removePageButton);
        var commandButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        commandButtons.Controls.Add(commandKindCombo);
        commandButtons.Controls.Add(addCommandButton);
        commandButtons.Controls.Add(removeCommandButton);

        AddSpecificRow(0, "Ámbito", scopeCombo);
        AddSpecificRow(1, "Páginas", pagesList, 90);
        AddSpecificRow(2, "", pageButtons);
        AddSpecificRow(3, "Trigger", triggerCombo);
        AddSpecificRow(4, "Prioridad", priorityNumeric);
        AddSpecificRow(5, "Radio", radiusNumeric);
        AddSpecificRow(6, "Comandos", commandsList, 110);
        AddSpecificRow(7, "", commandButtons, 40);
        AddSpecificRow(8, "Texto / diálogo", commandTextBox, 70);
        AddSpecificRow(9, "Clave switch/var", commandKeyBox);
        AddSpecificRow(10, "Valor", commandValueNumeric);
        AddSpecificRow(11, "X", commandXNumeric);
        AddSpecificRow(12, "Y", commandYNumeric);
        AddSpecificRow(13, "Item", commandItemCombo);
        AddSpecificRow(14, "Técnica", commandTechniqueCombo);
        AddSpecificRow(15, "", applyCommandButton);

        pagesList.SelectedIndexChanged += (_, _) => LoadSelectedPage();
        commandsList.SelectedIndexChanged += (_, _) => LoadSelectedCommand();
        addPageButton.Click += (_, _) => AddPage();
        removePageButton.Click += (_, _) => RemovePage();
        addCommandButton.Click += (_, _) => AddCommand();
        removeCommandButton.Click += (_, _) => RemoveCommand();
        applyCommandButton.Click += (_, _) => ApplyCommand();
        triggerCombo.SelectedIndexChanged += (_, _) => RememberPageHeader();
        ResumeLayout(false);
    }
}
