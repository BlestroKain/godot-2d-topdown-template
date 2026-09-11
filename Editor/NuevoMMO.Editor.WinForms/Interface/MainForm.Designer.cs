#nullable enable
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

partial class MainForm
{
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private IContainer? components;

    private MenuStrip mainMenuStrip = null!;
    private ToolStrip mainToolStrip = null!;
    private StatusStrip mainStatusStrip = null!;
    private ToolStripStatusLabel statusLabel = null!;
    private DockPanel dockPanel = null!;

    private ToolStripMenuItem fileMenuItem = null!;
    private ToolStripMenuItem fileNewMenuItem = null!;
    private ToolStripMenuItem fileOpenMenuItem = null!;
    private ToolStripMenuItem fileSaveMenuItem = null!;
    private ToolStripMenuItem fileSaveAsMenuItem = null!;
    private ToolStripMenuItem fileExitMenuItem = null!;

    private ToolStripMenuItem editMenuItem = null!;
    private ToolStripMenuItem editUndoMenuItem = null!;
    private ToolStripMenuItem editRedoMenuItem = null!;
    private ToolStripMenuItem editCopyMenuItem = null!;
    private ToolStripMenuItem editPasteMenuItem = null!;

    private ToolStripMenuItem mapMenuItem = null!;
    private ToolStripMenuItem mapNewMenuItem = null!;
    private ToolStripMenuItem mapSaveMenuItem = null!;
    private ToolStripMenuItem mapSelectMenuItem = null!;
    private ToolStripMenuItem mapPaintMenuItem = null!;
    private ToolStripMenuItem mapEraseMenuItem = null!;
    private ToolStripMenuItem mapFillMenuItem = null!;
    private ToolStripMenuItem mapRectangleMenuItem = null!;
    private ToolStripMenuItem mapCollisionMenuItem = null!;
    private ToolStripMenuItem mapImportTilesetsMenuItem = null!;

    private ToolStripMenuItem contentMenuItem = null!;
    private ToolStripMenuItem contentItemsMenuItem = null!;
    private ToolStripMenuItem contentMobsMenuItem = null!;
    private ToolStripMenuItem contentNpcsMenuItem = null!;
    private ToolStripMenuItem contentResourcesMenuItem = null!;
    private ToolStripMenuItem contentTechniquesMenuItem = null!;
    private ToolStripMenuItem contentEffectsMenuItem = null!;
    private ToolStripMenuItem contentTraditionsMenuItem = null!;
    private ToolStripMenuItem contentProfessionsMenuItem = null!;
    private ToolStripMenuItem contentRecipesMenuItem = null!;
    private ToolStripMenuItem contentLootTablesMenuItem = null!;
    private ToolStripMenuItem contentSpawnTablesMenuItem = null!;
    private ToolStripMenuItem contentQuestsMenuItem = null!;
    private ToolStripMenuItem contentEventsMenuItem = null!;
    private ToolStripMenuItem contentDungeonsMenuItem = null!;
    private ToolStripMenuItem contentItemPropertiesMenuItem = null!;
    private ToolStripMenuItem contentTilesetsMenuItem = null!;

    private ToolStripMenuItem viewMenuItem = null!;
    private ToolStripMenuItem viewContentMenuItem = null!;
    private ToolStripMenuItem viewMapToolsMenuItem = null!;
    private ToolStripMenuItem viewTilesetsMenuItem = null!;
    private ToolStripMenuItem viewPropertiesMenuItem = null!;
    private ToolStripMenuItem viewProblemsMenuItem = null!;

    private ToolStripMenuItem toolsMenuItem = null!;
    private ToolStripMenuItem toolsValidateMenuItem = null!;

    private ToolStripButton toolNewButton = null!;
    private ToolStripButton toolOpenButton = null!;
    private ToolStripButton toolSaveButton = null!;
    private ToolStripButton toolUndoButton = null!;
    private ToolStripButton toolRedoButton = null!;
    private ToolStripButton toolNewMapButton = null!;
    private ToolStripButton toolPaintButton = null!;
    private ToolStripButton toolEraseButton = null!;
    private ToolStripButton toolCollisionButton = null!;
    private ToolStripButton toolValidateButton = null!;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            images?.Dispose();
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new Container();
        mainMenuStrip = new MenuStrip();
        fileMenuItem = new ToolStripMenuItem();
        fileNewMenuItem = new ToolStripMenuItem();
        fileOpenMenuItem = new ToolStripMenuItem();
        fileSaveMenuItem = new ToolStripMenuItem();
        fileSaveAsMenuItem = new ToolStripMenuItem();
        fileExitMenuItem = new ToolStripMenuItem();
        editMenuItem = new ToolStripMenuItem();
        editUndoMenuItem = new ToolStripMenuItem();
        editRedoMenuItem = new ToolStripMenuItem();
        editCopyMenuItem = new ToolStripMenuItem();
        editPasteMenuItem = new ToolStripMenuItem();
        mapMenuItem = new ToolStripMenuItem();
        mapNewMenuItem = new ToolStripMenuItem();
        mapSaveMenuItem = new ToolStripMenuItem();
        mapSelectMenuItem = new ToolStripMenuItem();
        mapPaintMenuItem = new ToolStripMenuItem();
        mapEraseMenuItem = new ToolStripMenuItem();
        mapFillMenuItem = new ToolStripMenuItem();
        mapRectangleMenuItem = new ToolStripMenuItem();
        mapCollisionMenuItem = new ToolStripMenuItem();
        mapImportTilesetsMenuItem = new ToolStripMenuItem();
        contentMenuItem = new ToolStripMenuItem();
        contentItemsMenuItem = new ToolStripMenuItem();
        contentMobsMenuItem = new ToolStripMenuItem();
        contentNpcsMenuItem = new ToolStripMenuItem();
        contentResourcesMenuItem = new ToolStripMenuItem();
        contentTechniquesMenuItem = new ToolStripMenuItem();
        contentEffectsMenuItem = new ToolStripMenuItem();
        contentTraditionsMenuItem = new ToolStripMenuItem();
        contentProfessionsMenuItem = new ToolStripMenuItem();
        contentRecipesMenuItem = new ToolStripMenuItem();
        contentLootTablesMenuItem = new ToolStripMenuItem();
        contentSpawnTablesMenuItem = new ToolStripMenuItem();
        contentQuestsMenuItem = new ToolStripMenuItem();
        contentEventsMenuItem = new ToolStripMenuItem();
        contentDungeonsMenuItem = new ToolStripMenuItem();
        contentItemPropertiesMenuItem = new ToolStripMenuItem();
        contentTilesetsMenuItem = new ToolStripMenuItem();
        viewMenuItem = new ToolStripMenuItem();
        viewContentMenuItem = new ToolStripMenuItem();
        viewMapToolsMenuItem = new ToolStripMenuItem();
        viewTilesetsMenuItem = new ToolStripMenuItem();
        viewPropertiesMenuItem = new ToolStripMenuItem();
        viewProblemsMenuItem = new ToolStripMenuItem();
        toolsMenuItem = new ToolStripMenuItem();
        toolsValidateMenuItem = new ToolStripMenuItem();
        mainToolStrip = new ToolStrip();
        toolNewButton = new ToolStripButton();
        toolOpenButton = new ToolStripButton();
        toolSaveButton = new ToolStripButton();
        toolUndoButton = new ToolStripButton();
        toolRedoButton = new ToolStripButton();
        toolNewMapButton = new ToolStripButton();
        toolPaintButton = new ToolStripButton();
        toolEraseButton = new ToolStripButton();
        toolCollisionButton = new ToolStripButton();
        toolValidateButton = new ToolStripButton();
        mainStatusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();
        dockPanel = new DockPanel();

        mainMenuStrip.SuspendLayout();
        mainToolStrip.SuspendLayout();
        mainStatusStrip.SuspendLayout();
        SuspendLayout();

        fileNewMenuItem.Name = "fileNewMenuItem";
        fileNewMenuItem.ShortcutKeys = Keys.Control | Keys.N;
        fileNewMenuItem.Text = "&Nuevo proyecto";
        fileOpenMenuItem.Name = "fileOpenMenuItem";
        fileOpenMenuItem.ShortcutKeys = Keys.Control | Keys.O;
        fileOpenMenuItem.Text = "&Abrir contenido…";
        fileSaveMenuItem.Name = "fileSaveMenuItem";
        fileSaveMenuItem.ShortcutKeys = Keys.Control | Keys.S;
        fileSaveMenuItem.Text = "&Guardar";
        fileSaveAsMenuItem.Name = "fileSaveAsMenuItem";
        fileSaveAsMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;
        fileSaveAsMenuItem.Text = "Guardar &como…";
        fileExitMenuItem.Name = "fileExitMenuItem";
        fileExitMenuItem.Text = "&Salir";
        fileMenuItem.DropDownItems.AddRange(
        [
            fileNewMenuItem,
            fileOpenMenuItem,
            new ToolStripSeparator(),
            fileSaveMenuItem,
            fileSaveAsMenuItem,
            new ToolStripSeparator(),
            fileExitMenuItem
        ]);
        fileMenuItem.Name = "fileMenuItem";
        fileMenuItem.Text = "&Archivo";

        editUndoMenuItem.Name = "editUndoMenuItem";
        editUndoMenuItem.ShortcutKeys = Keys.Control | Keys.Z;
        editUndoMenuItem.Text = "&Deshacer";
        editRedoMenuItem.Name = "editRedoMenuItem";
        editRedoMenuItem.ShortcutKeys = Keys.Control | Keys.Y;
        editRedoMenuItem.Text = "&Rehacer";
        editCopyMenuItem.Name = "editCopyMenuItem";
        editCopyMenuItem.ShortcutKeys = Keys.Control | Keys.C;
        editCopyMenuItem.Text = "&Copiar tiles";
        editPasteMenuItem.Name = "editPasteMenuItem";
        editPasteMenuItem.ShortcutKeys = Keys.Control | Keys.V;
        editPasteMenuItem.Text = "&Pegar tiles";
        editMenuItem.DropDownItems.AddRange([editUndoMenuItem, editRedoMenuItem, new ToolStripSeparator(), editCopyMenuItem, editPasteMenuItem]);
        editMenuItem.Name = "editMenuItem";
        editMenuItem.Text = "&Editar";

        mapNewMenuItem.Name = "mapNewMenuItem";
        mapNewMenuItem.Text = "&Nuevo mapa";
        mapSaveMenuItem.Name = "mapSaveMenuItem";
        mapSaveMenuItem.Text = "&Guardar mapa";
        mapSelectMenuItem.Name = "mapSelectMenuItem";
        mapSelectMenuItem.Text = "&Seleccionar";
        mapPaintMenuItem.Name = "mapPaintMenuItem";
        mapPaintMenuItem.Text = "&Pintar tiles";
        mapEraseMenuItem.Name = "mapEraseMenuItem";
        mapEraseMenuItem.Text = "&Borrar tiles";
        mapFillMenuItem.Name = "mapFillMenuItem";
        mapFillMenuItem.Text = "&Rellenar";
        mapRectangleMenuItem.Name = "mapRectangleMenuItem";
        mapRectangleMenuItem.Text = "Pintar &rectángulo";
        mapCollisionMenuItem.Name = "mapCollisionMenuItem";
        mapCollisionMenuItem.Text = "&Colisiones";
        mapImportTilesetsMenuItem.Name = "mapImportTilesetsMenuItem";
        mapImportTilesetsMenuItem.Text = "&Importar tilesets del cliente";
        mapMenuItem.DropDownItems.AddRange(
        [
            mapNewMenuItem,
            mapSaveMenuItem,
            new ToolStripSeparator(),
            mapSelectMenuItem,
            mapPaintMenuItem,
            mapEraseMenuItem,
            mapFillMenuItem,
            mapRectangleMenuItem,
            mapCollisionMenuItem,
            new ToolStripSeparator(),
            mapImportTilesetsMenuItem
        ]);
        mapMenuItem.Name = "mapMenuItem";
        mapMenuItem.Text = "&Mapa";

        contentItemsMenuItem.Name = "contentItemsMenuItem";
        contentItemsMenuItem.Text = "Items";
        contentMobsMenuItem.Name = "contentMobsMenuItem";
        contentMobsMenuItem.Text = "Mobs";
        contentNpcsMenuItem.Name = "contentNpcsMenuItem";
        contentNpcsMenuItem.Text = "NPCs";
        contentResourcesMenuItem.Name = "contentResourcesMenuItem";
        contentResourcesMenuItem.Text = "Recursos";
        contentTechniquesMenuItem.Name = "contentTechniquesMenuItem";
        contentTechniquesMenuItem.Text = "Técnicas / Spells";
        contentEffectsMenuItem.Name = "contentEffectsMenuItem";
        contentEffectsMenuItem.Text = "Efectos";
        contentTraditionsMenuItem.Name = "contentTraditionsMenuItem";
        contentTraditionsMenuItem.Text = "Tradiciones / Clases";
        contentProfessionsMenuItem.Name = "contentProfessionsMenuItem";
        contentProfessionsMenuItem.Text = "Profesiones";
        contentRecipesMenuItem.Name = "contentRecipesMenuItem";
        contentRecipesMenuItem.Text = "Recetas";
        contentLootTablesMenuItem.Name = "contentLootTablesMenuItem";
        contentLootTablesMenuItem.Text = "Loot Tables";
        contentSpawnTablesMenuItem.Name = "contentSpawnTablesMenuItem";
        contentSpawnTablesMenuItem.Text = "Spawn Tables";
        contentQuestsMenuItem.Name = "contentQuestsMenuItem";
        contentQuestsMenuItem.Text = "Quests";
        contentEventsMenuItem.Name = "contentEventsMenuItem";
        contentEventsMenuItem.Text = "Eventos";
        contentDungeonsMenuItem.Name = "contentDungeonsMenuItem";
        contentDungeonsMenuItem.Text = "Dungeons";
        contentItemPropertiesMenuItem.Name = "contentItemPropertiesMenuItem";
        contentItemPropertiesMenuItem.Text = "Propiedades de item";
        contentTilesetsMenuItem.Name = "contentTilesetsMenuItem";
        contentTilesetsMenuItem.Text = "Tilesets";
        contentMenuItem.DropDownItems.AddRange(
        [
            contentItemsMenuItem,
            contentMobsMenuItem,
            contentNpcsMenuItem,
            contentResourcesMenuItem,
            contentTechniquesMenuItem,
            contentEffectsMenuItem,
            contentTraditionsMenuItem,
            contentProfessionsMenuItem,
            contentRecipesMenuItem,
            contentLootTablesMenuItem,
            contentSpawnTablesMenuItem,
            contentQuestsMenuItem,
            contentEventsMenuItem,
            contentDungeonsMenuItem,
            contentItemPropertiesMenuItem,
            contentTilesetsMenuItem
        ]);
        contentMenuItem.Name = "contentMenuItem";
        contentMenuItem.Text = "&Contenido";

        viewContentMenuItem.Name = "viewContentMenuItem";
        viewContentMenuItem.Text = "Contenido";
        viewMapToolsMenuItem.Name = "viewMapToolsMenuItem";
        viewMapToolsMenuItem.Text = "Mapa / Capas";
        viewTilesetsMenuItem.Name = "viewTilesetsMenuItem";
        viewTilesetsMenuItem.Text = "Tilesets";
        viewPropertiesMenuItem.Name = "viewPropertiesMenuItem";
        viewPropertiesMenuItem.Text = "Propiedades";
        viewProblemsMenuItem.Name = "viewProblemsMenuItem";
        viewProblemsMenuItem.Text = "Problemas";
        viewMenuItem.DropDownItems.AddRange(
        [
            viewContentMenuItem,
            viewMapToolsMenuItem,
            viewTilesetsMenuItem,
            viewPropertiesMenuItem,
            viewProblemsMenuItem
        ]);
        viewMenuItem.Name = "viewMenuItem";
        viewMenuItem.Text = "&Ver";

        toolsValidateMenuItem.Name = "toolsValidateMenuItem";
        toolsValidateMenuItem.Text = "&Validar proyecto";
        toolsMenuItem.DropDownItems.AddRange([toolsValidateMenuItem]);
        toolsMenuItem.Name = "toolsMenuItem";
        toolsMenuItem.Text = "&Herramientas";

        mainMenuStrip.Items.AddRange([fileMenuItem, editMenuItem, mapMenuItem, contentMenuItem, viewMenuItem, toolsMenuItem]);
        mainMenuStrip.Location = new Point(0, 0);
        mainMenuStrip.Name = "mainMenuStrip";
        mainMenuStrip.Size = new Size(1440, 24);
        mainMenuStrip.TabIndex = 0;
        mainMenuStrip.Text = "mainMenuStrip";

        toolNewButton.Name = "toolNewButton";
        toolNewButton.Text = "Nuevo";
        toolOpenButton.Name = "toolOpenButton";
        toolOpenButton.Text = "Abrir";
        toolSaveButton.Name = "toolSaveButton";
        toolSaveButton.Text = "Guardar";
        toolUndoButton.Name = "toolUndoButton";
        toolUndoButton.Text = "Deshacer";
        toolRedoButton.Name = "toolRedoButton";
        toolRedoButton.Text = "Rehacer";
        toolNewMapButton.Name = "toolNewMapButton";
        toolNewMapButton.Text = "Nuevo mapa";
        toolPaintButton.Name = "toolPaintButton";
        toolPaintButton.Text = "Pintar";
        toolEraseButton.Name = "toolEraseButton";
        toolEraseButton.Text = "Borrar";
        toolCollisionButton.Name = "toolCollisionButton";
        toolCollisionButton.Text = "Colisión";
        toolValidateButton.Name = "toolValidateButton";
        toolValidateButton.Text = "Validar";
        mainToolStrip.GripStyle = ToolStripGripStyle.Hidden;
        mainToolStrip.Items.AddRange(
        [
            toolNewButton,
            toolOpenButton,
            toolSaveButton,
            new ToolStripSeparator(),
            toolUndoButton,
            toolRedoButton,
            new ToolStripSeparator(),
            toolNewMapButton,
            toolPaintButton,
            toolEraseButton,
            toolCollisionButton,
            new ToolStripSeparator(),
            toolValidateButton
        ]);
        mainToolStrip.Location = new Point(0, 24);
        mainToolStrip.Name = "mainToolStrip";
        mainToolStrip.Size = new Size(1440, 25);
        mainToolStrip.TabIndex = 1;

        statusLabel.Name = "statusLabel";
        statusLabel.Spring = true;
        statusLabel.Text = "Listo";
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        mainStatusStrip.Items.AddRange([statusLabel]);
        mainStatusStrip.Location = new Point(0, 878);
        mainStatusStrip.Name = "mainStatusStrip";
        mainStatusStrip.Size = new Size(1440, 22);
        mainStatusStrip.TabIndex = 2;

        dockPanel.Dock = DockStyle.Fill;
        dockPanel.DocumentStyle = DocumentStyle.DockingWindow;
        dockPanel.Location = new Point(0, 49);
        dockPanel.Name = "dockPanel";
        dockPanel.Size = new Size(1440, 829);
        dockPanel.TabIndex = 3;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1440, 900);
        Controls.Add(dockPanel);
        Controls.Add(mainStatusStrip);
        Controls.Add(mainToolStrip);
        Controls.Add(mainMenuStrip);
        MainMenuStrip = mainMenuStrip;
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "NuevoMMO Editor";
        WindowState = FormWindowState.Maximized;

        mainMenuStrip.ResumeLayout(false);
        mainMenuStrip.PerformLayout();
        mainToolStrip.ResumeLayout(false);
        mainToolStrip.PerformLayout();
        mainStatusStrip.ResumeLayout(false);
        mainStatusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
