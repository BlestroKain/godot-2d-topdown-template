using NuevoMMO.Core;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

/// <summary>
/// Ventana principal del NuevoMMO Editor. Sigue el flujo de Intersect: un único ejecutable,
/// mapa como documento central y herramientas/editores acoplables alrededor.
/// </summary>
[System.ComponentModel.DesignerCategory("Form")]
public sealed partial class MainForm : Form
{
    private readonly EditorApplication application;
    private readonly TilesetImageProvider images;
    private readonly DefinitionEditorCatalog definitionEditors;
    private readonly ContentExplorerDock contentExplorer;
    private readonly PropertiesDock properties;
    private readonly ProblemsDock problems;
    private readonly MapToolsDock mapTools;
    private readonly TilesetPaletteDock tilesetPalette;
    private readonly MapGridDock mapGrid;
    private readonly MapEditorDocument mapDocument;

    public MainForm(EditorApplication application)
    {
        InitializeComponent();
        this.application = application ?? throw new ArgumentNullException(nameof(application));
        images = new TilesetImageProvider(application.Configuration, application.Definitions, application.Assets);
        definitionEditors = new DefinitionEditorCatalog(application);

        dockPanel.Theme = new VS2015DarkTheme();
        dockPanel.DocumentStyle = DocumentStyle.DockingWindow;

        contentExplorer = new ContentExplorerDock(application);
        properties = new PropertiesDock();
        problems = new ProblemsDock();
        mapTools = new MapToolsDock();
        tilesetPalette = new TilesetPaletteDock(application, images);
        mapGrid = new MapGridDock(application);
        mapDocument = new MapEditorDocument(application, images);

        contentExplorer.DefinitionActivated += OpenDefinition;
        contentExplorer.DefinitionSelected += definition =>
        {
            mapDocument.BrushDefinition = definition;
            mapDocument.SelectedPlacementDefinitionId = definition.Id;
            if (definition is not MapDefinition)
                properties.SelectedObject = definition;
        };
        definitionEditors.ContentChanged += definition =>
        {
            contentExplorer.RefreshTree();
            tilesetPalette.RefreshTilesets();
            RefreshMapToolDefinitions(mapDocument.ActiveTool);
            properties.SelectedObject = definition;
            problems.SetProblems(ValidateProject(showMessage: false));
            mapGrid.RefreshGrid();
            Text = "NuevoMMO Editor *";
        };
        mapTools.ToolSelected += tool =>
        {
            mapDocument.ActiveTool = tool;
            RefreshMapToolDefinitions(tool);
            SetStatus($"Herramienta: {tool}");
        };
        mapTools.DefinitionSelected += id => mapDocument.SelectedPlacementDefinitionId = id;
        mapTools.LayerSelected += key =>
        {
            if (application.Maps.Document is not null)
                application.Maps.Layers.Select(key);
        };
        mapDocument.MapOpened += document =>
        {
            properties.SelectedObject = document;
            RefreshMapLayers();
            RefreshMapToolDefinitions(mapDocument.ActiveTool);
            SetStatus($"Mapa abierto: {document.Name}");
        };
        mapDocument.MapChanged += () =>
        {
            properties.RefreshSelectedObject();
            Text = "NuevoMMO Editor *";
        };
        mapDocument.SelectedObjectChanged += value => properties.SelectedObject = value;
        mapDocument.EditorNotice += SetStatus;
        mapGrid.MapActivated += map =>
        {
            mapDocument.Open(map);
            RefreshMapLayers();
            mapGrid.RefreshGrid();
        };
        mapGrid.CreateRequested += CreateMapAt;

        WireDesignerEvents();
        EditorTheme.ApplyWindow(this);
        Load += (_, _) =>
        {
            InitializeDockLayout();
            EditorTheme.ApplyWindow(this);
        };
        FormClosing += OnFormClosing;
    }

    private void WireDesignerEvents()
    {
        fileReloadMenuItem.Click += (_, _) => ReloadContent();
        fileSaveMenuItem.Click += (_, _) => SaveContent();
        fileExitMenuItem.Click += (_, _) => Close();

        editUndoMenuItem.Click += (_, _) => Undo();
        editRedoMenuItem.Click += (_, _) => Redo();
        editCopyMenuItem.Click += (_, _) => CopyMapSelection();
        editPasteMenuItem.Click += (_, _) => PasteMapSelection();

        mapNewMenuItem.Click += (_, _) => mapGrid.Show(dockPanel, DockState.DockLeft);
        mapSaveMenuItem.Click += (_, _) => SaveCurrentMap();
        mapSelectMenuItem.Click += (_, _) => SetMapTool(MapEditorTool.Select);
        mapPaintMenuItem.Click += (_, _) => SetMapTool(MapEditorTool.PaintTile);
        mapEraseMenuItem.Click += (_, _) => SetMapTool(MapEditorTool.EraseTile);
        mapFillMenuItem.Click += (_, _) => SetMapTool(MapEditorTool.Fill);
        mapRectangleMenuItem.Click += (_, _) => SetMapTool(MapEditorTool.Rectangle);
        mapCollisionMenuItem.Click += (_, _) => SetMapTool(MapEditorTool.Collision);

        contentItemsMenuItem.Click += (_, _) => definitionEditors.Open(typeof(ItemDefinition), dockPanel);
        contentMobsMenuItem.Click += (_, _) => definitionEditors.Open(typeof(MobDefinition), dockPanel);
        contentNpcsMenuItem.Click += (_, _) => definitionEditors.Open(typeof(NpcDefinition), dockPanel);
        contentResourcesMenuItem.Click += (_, _) => definitionEditors.Open(typeof(ResourceDefinition), dockPanel);
        contentTechniquesMenuItem.Click += (_, _) => definitionEditors.Open(typeof(TechniqueDefinition), dockPanel);
        contentEffectsMenuItem.Click += (_, _) => definitionEditors.Open(typeof(EffectDefinition), dockPanel);
        contentTraditionsMenuItem.Click += (_, _) => definitionEditors.Open(typeof(TraditionDefinition), dockPanel);
        contentProfessionsMenuItem.Click += (_, _) => definitionEditors.Open(typeof(ProfessionDefinition), dockPanel);
        contentRecipesMenuItem.Click += (_, _) => definitionEditors.Open(typeof(RecipeDefinition), dockPanel);
        contentLootTablesMenuItem.Click += (_, _) => definitionEditors.Open(typeof(LootTableDefinition), dockPanel);
        contentSpawnTablesMenuItem.Click += (_, _) => definitionEditors.Open(typeof(SpawnTableDefinition), dockPanel);
        contentQuestsMenuItem.Click += (_, _) => definitionEditors.Open(typeof(QuestDefinition), dockPanel);
        contentEventsMenuItem.Click += (_, _) => definitionEditors.Open(typeof(EventDefinition), dockPanel);
        contentDungeonsMenuItem.Click += (_, _) => definitionEditors.Open(typeof(DungeonDefinition), dockPanel);
        contentItemPropertiesMenuItem.Click += (_, _) => definitionEditors.Open(typeof(ItemPropertyDefinition), dockPanel);
        contentTilesetsMenuItem.Click += (_, _) => definitionEditors.Open(typeof(TilesetDefinition), dockPanel);

        viewWorldMenuItem.Click += (_, _) => mapGrid.Show(dockPanel, DockState.DockLeft);
        viewContentMenuItem.Click += (_, _) => contentExplorer.Show(dockPanel, DockState.DockRight);
        viewMapToolsMenuItem.Click += (_, _) => mapTools.Show(dockPanel, DockState.DockLeft);
        viewTilesetsMenuItem.Click += (_, _) => tilesetPalette.Show(dockPanel, DockState.DockRight);
        viewPropertiesMenuItem.Click += (_, _) => properties.Show(dockPanel, DockState.DockRight);
        viewProblemsMenuItem.Click += (_, _) => problems.Show(dockPanel, DockState.DockBottomAutoHide);

        toolsValidateMenuItem.Click += (_, _) => ValidateProject(showMessage: true);

        toolReloadButton.Click += (_, _) => ReloadContent();
        toolSaveButton.Click += (_, _) => SaveContent();
        toolUndoButton.Click += (_, _) => Undo();
        toolRedoButton.Click += (_, _) => Redo();
        toolNewMapButton.Click += (_, _) => mapGrid.Show(dockPanel, DockState.DockLeft);
        toolPaintButton.Click += (_, _) => SetMapTool(MapEditorTool.PaintTile);
        toolEraseButton.Click += (_, _) => SetMapTool(MapEditorTool.EraseTile);
        toolCollisionButton.Click += (_, _) => SetMapTool(MapEditorTool.Collision);
        toolValidateButton.Click += (_, _) => ValidateProject(showMessage: true);
    }

    private void InitializeDockLayout()
    {
        dockPanel.DockLeftPortion = 0.22;
        dockPanel.DockRightPortion = 0.28;
        mapGrid.Show(dockPanel, DockState.DockLeft);
        if (mapGrid.Pane is not null)
            mapTools.Show(mapGrid.Pane, DockAlignment.Bottom, 0.42);
        else
            mapTools.Show(dockPanel, DockState.DockLeft);
        tilesetPalette.Show(dockPanel, DockState.DockRight);
        if (tilesetPalette.Pane is not null)
            contentExplorer.Show(tilesetPalette.Pane, DockAlignment.Bottom, 0.48);
        else
            contentExplorer.Show(dockPanel, DockState.DockRight);
        if (contentExplorer.Pane is not null)
            properties.Show(contentExplorer.Pane, DockAlignment.Bottom, 0.42);
        else
            properties.Show(dockPanel, DockState.DockRight);
        problems.Show(dockPanel, DockState.DockBottomAutoHide);
        mapDocument.Show(dockPanel, DockState.Document);
        RefreshMapToolDefinitions(mapDocument.ActiveTool);
        SyncGraphics();
        MapWorldGrid.AssignUniqueCells(application.Definitions);
        mapGrid.RefreshGrid();
        ValidateProject(showMessage: false);
        SetStatus($"GameData: {application.Content.CurrentPath ?? application.Configuration.ContentPath}");
    }

    private void ReloadContent()
    {
        if (!ConfirmDiscardChanges()) return;
        var path = application.Configuration.ContentPath;
        try
        {
            application.Maps.Close();
            images.Clear();
            if (File.Exists(path))
                application.Content.Load(path);
            else
            {
                application.Content.New();
                application.Content.BindPath(path);
            }

            MapWorldGrid.AssignUniqueCells(application.Definitions);
            SyncGraphics();
            definitionEditors.RefreshOpenEditors();
            contentExplorer.RefreshTree();
            tilesetPalette.RefreshTilesets();
            mapGrid.RefreshGrid();
            RefreshMapToolDefinitions(mapDocument.ActiveTool);
            properties.SelectedObject = null;
            ValidateProject(showMessage: false);
            SetCleanTitle();
            SetStatus($"Recargado: {path}");
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "No se pudo recargar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveContent()
    {
        try
        {
            SaveCurrentMap();
            var validation = ValidateProject(showMessage: false);
            if (validation.Count > 0)
            {
                var proceed = MessageBox.Show(
                    this,
                    "El proyecto contiene problemas. ¿Guardar game.db de todos modos?",
                    "Contenido con avisos",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (proceed != DialogResult.Yes) return;
            }

            var path = application.Content.CurrentPath ?? application.Configuration.ContentPath;
            application.Content.Save(path);
            SetCleanTitle();
            SetStatus($"Guardado: {path}");
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "No se pudo guardar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CreateMapAt(int gridX, int gridY)
    {
        try
        {
            var document = application.Maps.CreateAt(gridX, gridY);
            mapDocument.Open(document.ToDefinition());
            application.Content.Persist(document.ToDefinition());
            foreach (var neighbor in new[] { document.NorthMapId, document.SouthMapId, document.WestMapId, document.EastMapId })
            {
                if (neighbor is { } id && application.Definitions.TryGet<MapDefinition>(id, out var map) && map is not null)
                    application.Content.Persist(map);
            }

            contentExplorer.RefreshTree();
            RefreshMapLayers();
            mapGrid.RefreshGrid();
            SetStatus($"Mapa creado en ({gridX},{gridY}): {document.Name}");
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "No se pudo crear el mapa", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenDefinition(GameDefinition definition)
    {
        if (definition is MapDefinition map)
        {
            mapDocument.Open(map);
            RefreshMapLayers();
            mapGrid.RefreshGrid();
            return;
        }

        definitionEditors.Open(definition, dockPanel);
        properties.SelectedObject = definition;
        SetStatus($"Editor abierto: {definition.Name} ({definition.GetType().Name})");
    }

    private void SaveCurrentMap()
    {
        if (application.Maps.Document is null) return;
        mapDocument.SaveMap();
        var map = application.Maps.Document.ToDefinition();
        application.Content.Persist(map);
        contentExplorer.RefreshTree();
        RefreshMapLayers();
        mapGrid.RefreshGrid();
    }

    private void RefreshMapLayers()
    {
        if (application.Maps.Document is null)
        {
            mapTools.SetLayers([]);
            return;
        }
        mapTools.SetLayers(application.Maps.Layers.Layers, application.Maps.Layers.ActiveLayerKey);
    }

    private void RefreshMapToolDefinitions(MapEditorTool tool)
    {
        IEnumerable<GameDefinition> values = tool switch
        {
            MapEditorTool.Mob => application.Maps.PlacementDefinitions(SpawnEntityKind.Mob),
            MapEditorTool.Npc => application.Maps.PlacementDefinitions(SpawnEntityKind.Npc),
            MapEditorTool.Resource => application.Maps.PlacementDefinitions(SpawnEntityKind.Resource),
            MapEditorTool.SpawnZone => application.Maps.SpawnTableDefinitions().Cast<GameDefinition>(),
            MapEditorTool.Portal => application.Definitions.GetAll<MapDefinition>().Cast<GameDefinition>(),
            _ => []
        };

        var definitionsForTool = values.ToArray();
        if (definitionsForTool.Length == 0)
        {
            mapTools.ClearDefinitions();
            mapDocument.SelectedPlacementDefinitionId = null;
            return;
        }

        mapTools.SetDefinitions(definitionsForTool, mapDocument.SelectedPlacementDefinitionId);
        mapDocument.SelectedPlacementDefinitionId = mapTools.SelectedDefinitionId;
    }

    private void SyncGraphics()
    {
        var importer = new TilesetImporter(application.Definitions, images);
        var size = application.Maps.Document?.TileSize ?? new Vector2IntData(32, 32);
        var imported = importer.ImportClientTilesets(size);
        if (imported.Count == 0)
        {
            tilesetPalette.RefreshTilesets();
            return;
        }

        foreach (var tileset in imported)
            application.Content.Persist(tileset);
        tilesetPalette.RefreshTilesets();
        contentExplorer.RefreshTree();
        SetStatus($"Tilesets cargados: {imported.Count}");
    }

    private IReadOnlyList<string> ValidateProject(bool showMessage)
    {
        IReadOnlyList<string> validation;
        try
        {
            validation = application.Validator.Validate(application.Content.PackageVersion);
        }
        catch (Exception exception)
        {
            validation = [exception.Message];
        }
        problems.SetProblems(validation);
        if (showMessage)
        {
            MessageBox.Show(
                this,
                validation.Count == 0 ? "Proyecto válido." : $"Se encontraron {validation.Count} problema(s).",
                "Validación",
                MessageBoxButtons.OK,
                validation.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        return validation;
    }

    private void Undo()
    {
        application.History.Undo();
        mapDocument.RefreshView();
        properties.RefreshSelectedObject();
        Text = "NuevoMMO Editor *";
    }

    private void Redo()
    {
        application.History.Redo();
        mapDocument.RefreshView();
        properties.RefreshSelectedObject();
        Text = "NuevoMMO Editor *";
    }

    private void CopyMapSelection()
    {
        if (!mapDocument.CopySelection()) return;
        SetStatus("Tiles copiados.");
    }

    private void PasteMapSelection()
    {
        if (!mapDocument.PasteAtSelection()) return;
        Text = "NuevoMMO Editor *";
        SetStatus("Tiles pegados.");
    }

    private void SetMapTool(MapEditorTool tool)
    {
        mapDocument.ActiveTool = tool;
        mapTools.SelectTool(tool);
        RefreshMapToolDefinitions(tool);
        SetStatus($"Herramienta: {tool}");
    }

    private bool ConfirmDiscardChanges()
    {
        if (!application.Dirty.IsDirty) return true;
        var result = MessageBox.Show(
            this,
            "Hay cambios sin guardar. ¿Descartarlos?",
            "NuevoMMO Editor",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        return result == DialogResult.Yes;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!application.Dirty.IsDirty) return;
        if (ConfirmDiscardChanges()) return;
        e.Cancel = true;
    }

    private void SetStatus(string value) => statusLabel.Text = value;

    private void SetCleanTitle() => Text = "NuevoMMO Editor";
}
