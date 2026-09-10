using NuevoMMO.Core;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

/// <summary>
/// Ventana principal del NuevoMMO Editor. Sigue el flujo de Intersect: un único ejecutable,
/// mapa como documento central y herramientas/editores acoplables alrededor.
/// </summary>
public sealed class MainEditorForm : Form
{
    private readonly EditorApplication application;
    private readonly TilesetImageProvider images;
    private readonly DockPanel dockPanel = new() { Dock = DockStyle.Fill };
    private readonly ContentExplorerDock contentExplorer;
    private readonly PropertiesDock properties;
    private readonly ProblemsDock problems;
    private readonly MapToolsDock mapTools;
    private readonly TilesetPaletteDock tilesetPalette;
    private readonly MapEditorDocument mapDocument;
    private readonly ToolStripStatusLabel status = new("Listo");

    public MainEditorForm(EditorApplication application)
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
        images = new TilesetImageProvider(application.Configuration, application.Definitions);

        Text = "NuevoMMO Editor";
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1440;
        Height = 900;

        dockPanel.Theme = new VS2015DarkTheme();
        dockPanel.DocumentStyle = DocumentStyle.DockingWindow;

        contentExplorer = new ContentExplorerDock(application);
        properties = new PropertiesDock();
        problems = new ProblemsDock();
        mapTools = new MapToolsDock();
        tilesetPalette = new TilesetPaletteDock(application, images);
        mapDocument = new MapEditorDocument(application, images);

        contentExplorer.DefinitionActivated += OpenDefinition;
        mapTools.ToolSelected += tool => mapDocument.ActiveTool = tool;
        mapTools.LayerSelected += key =>
        {
            if (application.Maps.Document is not null)
                application.Maps.Layers.Select(key);
        };
        mapDocument.MapOpened += document =>
        {
            properties.SelectedObject = document;
            RefreshMapLayers();
            SetStatus($"Mapa abierto: {document.Name}");
        };
        mapDocument.MapChanged += () =>
        {
            properties.RefreshSelectedObject();
            Text = "NuevoMMO Editor *";
        };

        var menu = BuildMenu();
        var statusStrip = new StatusStrip();
        statusStrip.Items.Add(status);

        MainMenuStrip = menu;
        Controls.Add(dockPanel);
        Controls.Add(statusStrip);
        Controls.Add(menu);

        Load += (_, _) => InitializeDockLayout();
        FormClosing += OnFormClosing;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) images.Dispose();
        base.Dispose(disposing);
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip();

        var file = new ToolStripMenuItem("Archivo");
        file.DropDownItems.Add("Nuevo proyecto", null, (_, _) => NewProject());
        file.DropDownItems.Add("Abrir contenido…", null, (_, _) => OpenContent());
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("Guardar", null, (_, _) => SaveContent(saveAs: false));
        file.DropDownItems.Add("Guardar como…", null, (_, _) => SaveContent(saveAs: true));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add("Salir", null, (_, _) => Close());

        var edit = new ToolStripMenuItem("Editar");
        edit.DropDownItems.Add("Deshacer", null, (_, _) => Undo())
            .ShortcutKeys = Keys.Control | Keys.Z;
        edit.DropDownItems.Add("Rehacer", null, (_, _) => Redo())
            .ShortcutKeys = Keys.Control | Keys.Y;

        var map = new ToolStripMenuItem("Mapa");
        map.DropDownItems.Add("Nuevo mapa", null, (_, _) => CreateMap());
        map.DropDownItems.Add("Guardar mapa", null, (_, _) => SaveCurrentMap());
        map.DropDownItems.Add(new ToolStripSeparator());
        map.DropDownItems.Add("Seleccionar", null, (_, _) => SetMapTool(MapEditorTool.Select));
        map.DropDownItems.Add("Pintar tiles", null, (_, _) => SetMapTool(MapEditorTool.PaintTile));
        map.DropDownItems.Add("Borrar tiles", null, (_, _) => SetMapTool(MapEditorTool.EraseTile));
        map.DropDownItems.Add("Colisiones", null, (_, _) => SetMapTool(MapEditorTool.Collision));
        map.DropDownItems.Add(new ToolStripSeparator());
        map.DropDownItems.Add("Importar tilesets del cliente", null, (_, _) => ImportTilesets());

        var content = new ToolStripMenuItem("Contenido");
        AddContentEntry(content, "Items", typeof(ItemDefinition));
        AddContentEntry(content, "Mobs", typeof(MobDefinition));
        AddContentEntry(content, "NPCs", typeof(NpcDefinition));
        AddContentEntry(content, "Técnicas / Spells", typeof(TechniqueDefinition));
        AddContentEntry(content, "Efectos", typeof(EffectDefinition));
        AddContentEntry(content, "Tradiciones / Clases", typeof(TraditionDefinition));
        AddContentEntry(content, "Profesiones", typeof(ProfessionDefinition));
        AddContentEntry(content, "Recetas", typeof(RecipeDefinition));
        AddContentEntry(content, "Quests", typeof(QuestDefinition));
        AddContentEntry(content, "Eventos", typeof(EventDefinition));
        AddContentEntry(content, "Tilesets", typeof(TilesetDefinition));

        var view = new ToolStripMenuItem("Ver");
        view.DropDownItems.Add("Contenido", null, (_, _) => contentExplorer.Show(dockPanel, DockState.DockRight));
        view.DropDownItems.Add("Mapa / Capas", null, (_, _) => mapTools.Show(dockPanel, DockState.DockLeft));
        view.DropDownItems.Add("Tilesets", null, (_, _) => tilesetPalette.Show(dockPanel, DockState.DockLeft));
        view.DropDownItems.Add("Propiedades", null, (_, _) => properties.Show(dockPanel, DockState.DockRight));
        view.DropDownItems.Add("Problemas", null, (_, _) => problems.Show(dockPanel, DockState.DockBottom));

        var tools = new ToolStripMenuItem("Herramientas");
        tools.DropDownItems.Add("Validar proyecto", null, (_, _) => ValidateProject(showMessage: true));

        menu.Items.AddRange([file, edit, map, content, view, tools]);
        return menu;
    }

    private void InitializeDockLayout()
    {
        mapTools.Show(dockPanel, DockState.DockLeft);
        tilesetPalette.Show(dockPanel, DockState.DockLeft);
        contentExplorer.Show(dockPanel, DockState.DockRight);
        properties.Show(dockPanel, DockState.DockRight);
        problems.Show(dockPanel, DockState.DockBottom);
        mapDocument.Show(dockPanel, DockState.Document);
        ValidateProject(showMessage: false);
    }

    private void NewProject()
    {
        if (!ConfirmDiscardChanges()) return;
        application.Content.New();
        application.Maps.Close();
        contentExplorer.RefreshTree();
        tilesetPalette.RefreshTilesets();
        properties.SelectedObject = null;
        problems.SetProblems([]);
        SetCleanTitle();
        SetStatus("Proyecto nuevo.");
    }

    private void OpenContent()
    {
        if (!ConfirmDiscardChanges()) return;
        using var dialog = new OpenFileDialog
        {
            Filter = "NuevoMMO Content (*.json)|*.json|Todos los archivos (*.*)|*.*",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            application.Content.Load(dialog.FileName);
            application.Maps.Close();
            images.Clear();
            contentExplorer.RefreshTree();
            tilesetPalette.RefreshTilesets();
            properties.SelectedObject = null;
            ValidateProject(showMessage: false);
            SetCleanTitle();
            SetStatus($"Contenido cargado: {dialog.FileName}");
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "No se pudo abrir", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SaveContent(bool saveAs)
    {
        try
        {
            SaveCurrentMap();
            var validation = ValidateProject(showMessage: false);
            if (validation.Count > 0)
            {
                MessageBox.Show(
                    this,
                    "El proyecto contiene errores. Revise el panel Problemas antes de guardar.",
                    "Contenido inválido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string? path = saveAs ? null : application.Content.CurrentPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                using var dialog = new SaveFileDialog
                {
                    Filter = "NuevoMMO Content (*.json)|*.json|Todos los archivos (*.*)|*.*",
                    FileName = "gamedata.json"
                };
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                path = dialog.FileName;
            }

            application.Content.Save(path);
            SetCleanTitle();
            SetStatus($"Guardado: {path}");
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "No se pudo guardar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CreateMap()
    {
        var existing = application.Definitions.GetAll<MapDefinition>().Count;
        var suffix = existing + 1;
        ContentKey key;
        do key = new ContentKey($"maps.map_{suffix++:000}");
        while (application.Definitions.Contains(key));

        var definition = new MapDefinition(
            DefinitionId.New(),
            key,
            $"Map {existing + 1}",
            string.Empty,
            enabled: true,
            version: 1,
            tags: ["map"],
            visualKey: new ContentKey("maps.default"),
            bounds: new BoundsData(new(0, 0), new(960, 640)),
            spawn: new Vector2Data(64, 64),
            tileSize: new Vector2IntData(32, 32));

        application.Maps.Create(definition);
        mapDocument.Open(application.Maps.PreviewDefinition());
        application.Dirty.Mark();
        contentExplorer.RefreshTree();
        RefreshMapLayers();
        SetStatus($"Mapa creado: {definition.Name}");
    }

    private void OpenDefinition(GameDefinition definition)
    {
        if (definition is MapDefinition map)
        {
            mapDocument.Open(map);
            RefreshMapLayers();
            return;
        }

        properties.SelectedObject = definition;
        properties.Show(dockPanel, DockState.DockRight);
        SetStatus($"Seleccionado: {definition.Name} ({definition.GetType().Name})");
    }

    private void SaveCurrentMap()
    {
        if (application.Maps.Document is null) return;
        mapDocument.SaveMap();
        contentExplorer.RefreshTree();
        RefreshMapLayers();
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

    private void ImportTilesets()
    {
        var importer = new TilesetImporter(application.Definitions, images);
        var size = application.Maps.Document?.TileSize ?? new Vector2IntData(32, 32);
        try
        {
            var imported = importer.ImportClientTilesets(size);
            if (imported.Count > 0) application.Dirty.Mark();
            tilesetPalette.RefreshTilesets();
            contentExplorer.RefreshTree();
            SetStatus($"Tilesets importados: {imported.Count}");
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Importar tilesets", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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

    private void SetMapTool(MapEditorTool tool)
    {
        mapDocument.ActiveTool = tool;
        SetStatus($"Herramienta: {tool}");
    }

    private void AddContentEntry(ToolStripMenuItem parent, string label, Type type)
        => parent.DropDownItems.Add(label, null, (_, _) =>
        {
            var first = application.Content.Snapshot().All().FirstOrDefault(type.IsInstanceOfType);
            if (first is null)
            {
                MessageBox.Show(this, $"Todavía no hay contenido de tipo {label}.", "Contenido");
                return;
            }
            OpenDefinition(first);
        });

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

    private void SetStatus(string value) => status.Text = value;

    private void SetCleanTitle() => Text = "NuevoMMO Editor";
}
