#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

partial class MapToolsDock
{
    private IContainer? components;
    private TabControl modeTabs = null!;
    private TabPage tilesPage = null!;
    private TabPage attributesPage = null!;
    private TabPage lightsPage = null!;
    private TabPage eventsPage = null!;
    private TabPage entitiesPage = null!;
    private SplitContainer tilesSplit = null!;
    private ListBox tileTools = null!;
    private ListBox attributeTools = null!;
    private ListBox lightTools = null!;
    private ListBox eventTools = null!;
    private ListBox entityTools = null!;
    private ListBox layers = null!;
    private Label tileToolsLabel = null!;
    private Label layersLabel = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new Container();
        modeTabs = new TabControl();
        tilesPage = new TabPage();
        attributesPage = new TabPage();
        lightsPage = new TabPage();
        eventsPage = new TabPage();
        entitiesPage = new TabPage();
        tilesSplit = new SplitContainer();
        tileTools = new ListBox();
        attributeTools = new ListBox();
        lightTools = new ListBox();
        eventTools = new ListBox();
        entityTools = new ListBox();
        layers = new ListBox();
        tileToolsLabel = new Label();
        layersLabel = new Label();

        ((ISupportInitialize)tilesSplit).BeginInit();
        tilesSplit.Panel1.SuspendLayout();
        tilesSplit.Panel2.SuspendLayout();
        tilesSplit.SuspendLayout();
        modeTabs.SuspendLayout();
        tilesPage.SuspendLayout();
        attributesPage.SuspendLayout();
        lightsPage.SuspendLayout();
        eventsPage.SuspendLayout();
        entitiesPage.SuspendLayout();
        SuspendLayout();

        modeTabs.Controls.Add(tilesPage);
        modeTabs.Controls.Add(attributesPage);
        modeTabs.Controls.Add(lightsPage);
        modeTabs.Controls.Add(eventsPage);
        modeTabs.Controls.Add(entitiesPage);
        modeTabs.Dock = DockStyle.Fill;
        modeTabs.Name = "modeTabs";
        modeTabs.Padding = new Point(8, 3);
        modeTabs.SelectedIndex = 0;

        tilesPage.Controls.Add(tilesSplit);
        tilesPage.Name = "tilesPage";
        tilesPage.Padding = new Padding(3);
        tilesPage.Text = "Tiles";
        tilesPage.UseVisualStyleBackColor = false;

        attributesPage.Controls.Add(attributeTools);
        attributesPage.Name = "attributesPage";
        attributesPage.Padding = new Padding(3);
        attributesPage.Text = "Atributos";
        attributesPage.UseVisualStyleBackColor = false;

        lightsPage.Controls.Add(lightTools);
        lightsPage.Name = "lightsPage";
        lightsPage.Padding = new Padding(3);
        lightsPage.Text = "Luces";
        lightsPage.UseVisualStyleBackColor = false;

        eventsPage.Controls.Add(eventTools);
        eventsPage.Name = "eventsPage";
        eventsPage.Padding = new Padding(3);
        eventsPage.Text = "Eventos";
        eventsPage.UseVisualStyleBackColor = false;

        entitiesPage.Controls.Add(entityTools);
        entitiesPage.Name = "entitiesPage";
        entitiesPage.Padding = new Padding(3);
        entitiesPage.Text = "Entidades";
        entitiesPage.UseVisualStyleBackColor = false;

        tileToolsLabel.Dock = DockStyle.Top;
        tileToolsLabel.Height = 20;
        tileToolsLabel.Padding = new Padding(4, 3, 0, 0);
        tileToolsLabel.Text = "Herramienta";

        tileTools.Dock = DockStyle.Fill;
        tileTools.IntegralHeight = false;
        tileTools.Name = "tileTools";
        tileTools.BorderStyle = BorderStyle.None;

        layersLabel.Dock = DockStyle.Top;
        layersLabel.Height = 20;
        layersLabel.Padding = new Padding(4, 3, 0, 0);
        layersLabel.Text = "Capas";

        layers.Dock = DockStyle.Fill;
        layers.DisplayMember = "Label";
        layers.IntegralHeight = false;
        layers.Name = "layers";
        layers.BorderStyle = BorderStyle.None;

        tilesSplit.Dock = DockStyle.Fill;
        tilesSplit.Name = "tilesSplit";
        tilesSplit.Orientation = Orientation.Horizontal;
        tilesSplit.SplitterDistance = 120;
        tilesSplit.Panel1.Controls.Add(tileTools);
        tilesSplit.Panel1.Controls.Add(tileToolsLabel);
        tilesSplit.Panel2.Controls.Add(layers);
        tilesSplit.Panel2.Controls.Add(layersLabel);

        attributeTools.Dock = DockStyle.Fill;
        attributeTools.IntegralHeight = false;
        attributeTools.Name = "attributeTools";
        attributeTools.BorderStyle = BorderStyle.None;

        lightTools.Dock = DockStyle.Fill;
        lightTools.IntegralHeight = false;
        lightTools.Name = "lightTools";
        lightTools.BorderStyle = BorderStyle.None;

        eventTools.Dock = DockStyle.Fill;
        eventTools.IntegralHeight = false;
        eventTools.Name = "eventTools";
        eventTools.BorderStyle = BorderStyle.None;

        entityTools.Dock = DockStyle.Fill;
        entityTools.IntegralHeight = false;
        entityTools.Name = "entityTools";
        entityTools.BorderStyle = BorderStyle.None;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(330, 300);
        Controls.Add(modeTabs);
        HideOnClose = true;
        Name = "MapToolsDock";
        ShowHint = DockState.DockLeft;
        TabText = "Herramientas";
        Text = "Herramientas del mapa";

        tilesSplit.Panel1.ResumeLayout(false);
        tilesSplit.Panel2.ResumeLayout(false);
        ((ISupportInitialize)tilesSplit).EndInit();
        tilesSplit.ResumeLayout(false);
        modeTabs.ResumeLayout(false);
        tilesPage.ResumeLayout(false);
        attributesPage.ResumeLayout(false);
        lightsPage.ResumeLayout(false);
        eventsPage.ResumeLayout(false);
        entitiesPage.ResumeLayout(false);
        ResumeLayout(false);
    }
}
