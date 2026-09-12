using System.ComponentModel;
using System.Drawing;
using NuevoMMO.Core;
using WeifenLuo.WinFormsUI.Docking;

namespace NuevoMMO.Editor;

/// <summary>
/// Explorador de mundo al estilo de un editor MMO clásico. Mantiene la topología en
/// coordenadas del modelo, pero presenta los mapas como un árbol compacto para no
/// desperdiciar espacio de edición con una cuadrícula de navegación permanente.
/// </summary>
[DesignerCategory("Form")]
public sealed class MapGridDock : DockContent
{
    private readonly EditorApplication application;
    private readonly TreeView worldTree = new()
    {
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.None,
        FullRowSelect = true,
        HideSelection = false,
        HotTracking = true,
        ShowLines = true,
        ShowNodeToolTips = true
    };
    private readonly ToolStrip worldToolbar = new()
    {
        Dock = DockStyle.Top,
        GripStyle = ToolStripGripStyle.Hidden,
        Padding = new Padding(2, 1, 2, 1)
    };
    private readonly ToolStripButton refreshButton = new("Recargar") { DisplayStyle = ToolStripItemDisplayStyle.Text };
    private readonly ToolStripButton openButton = new("Abrir") { DisplayStyle = ToolStripItemDisplayStyle.Text };
    private readonly ContextMenuStrip mapMenu = new();

    public MapGridDock(EditorApplication application)
    {
        this.application = application ?? throw new ArgumentNullException(nameof(application));
        Text = "Mundo";
        TabText = "Mundo";
        HideOnClose = true;
        ShowHint = DockState.DockRight;
        ClientSize = new Size(270, 480);

        worldToolbar.Items.AddRange([refreshButton, openButton]);
        Controls.Add(worldTree);
        Controls.Add(worldToolbar);

        refreshButton.Click += (_, _) => RefreshGrid();
        openButton.Click += (_, _) => ActivateSelected();
        worldTree.NodeMouseDoubleClick += (_, e) => ActivateNode(e.Node);
        worldTree.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            ActivateSelected();
            e.Handled = true;
        };
        worldTree.AfterSelect += (_, e) =>
        {
            if (e.Node?.Tag is MapDefinition map)
                MapSelected?.Invoke(map);
        };
        worldTree.NodeMouseClick += OnNodeMouseClick;

        BuildContextMenu();
        EditorTheme.ApplyWindow(this);
        RefreshGrid();
    }

    public event Action<MapDefinition>? MapActivated;
    public event Action<MapDefinition>? MapSelected;
    public event Action<int, int>? CreateRequested;

    public void RefreshGrid()
    {
        var maps = application.Definitions.GetAll<MapDefinition>()
            .OrderBy(static map => map.GridY)
            .ThenBy(static map => map.GridX)
            .ThenBy(static map => map.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var activeId = application.Maps.Document?.Id;

        worldTree.BeginUpdate();
        worldTree.Nodes.Clear();

        var root = new TreeNode("Mundo")
        {
            Name = "world-root",
            ToolTipText = $"{maps.Length} mapa(s)"
        };
        var mapsFolder = new TreeNode("Mapas")
        {
            Name = "maps-root",
            ToolTipText = "Mapas del mundo"
        };
        root.Nodes.Add(mapsFolder);

        foreach (var map in maps)
        {
            var node = new TreeNode(map.Name)
            {
                Name = map.Id.ToString(),
                Tag = map,
                ToolTipText = $"{map.Name} · ({map.GridX},{map.GridY})"
            };
            if (map.Id == activeId)
            {
                node.BackColor = EditorTheme.Accent;
                node.ForeColor = Color.White;
                worldTree.SelectedNode = node;
            }
            mapsFolder.Nodes.Add(node);
        }

        worldTree.Nodes.Add(root);
        root.Expand();
        mapsFolder.Expand();
        worldTree.EndUpdate();

        openButton.Enabled = worldTree.SelectedNode?.Tag is MapDefinition;
    }

    private void BuildContextMenu()
    {
        var open = new ToolStripMenuItem("Abrir mapa");
        var properties = new ToolStripMenuItem("Propiedades");
        var createNorth = new ToolStripMenuItem("Nuevo mapa al norte");
        var createSouth = new ToolStripMenuItem("Nuevo mapa al sur");
        var createWest = new ToolStripMenuItem("Nuevo mapa al oeste");
        var createEast = new ToolStripMenuItem("Nuevo mapa al este");
        var createInitial = new ToolStripMenuItem("Crear mapa inicial");

        open.Click += (_, _) => ActivateSelected();
        properties.Click += (_, _) =>
        {
            if (worldTree.SelectedNode?.Tag is MapDefinition map)
                MapSelected?.Invoke(map);
        };
        createNorth.Click += (_, _) => CreateRelative(0, -1);
        createSouth.Click += (_, _) => CreateRelative(0, 1);
        createWest.Click += (_, _) => CreateRelative(-1, 0);
        createEast.Click += (_, _) => CreateRelative(1, 0);
        createInitial.Click += (_, _) => CreateRequested?.Invoke(0, 0);

        mapMenu.Items.AddRange([
            open,
            properties,
            new ToolStripSeparator(),
            createNorth,
            createSouth,
            createWest,
            createEast,
            new ToolStripSeparator(),
            createInitial
        ]);
        mapMenu.Opening += (_, _) =>
        {
            var selected = worldTree.SelectedNode?.Tag as MapDefinition;
            open.Enabled = selected is not null;
            properties.Enabled = selected is not null;
            createNorth.Enabled = CanCreateRelative(selected, 0, -1);
            createSouth.Enabled = CanCreateRelative(selected, 0, 1);
            createWest.Enabled = CanCreateRelative(selected, -1, 0);
            createEast.Enabled = CanCreateRelative(selected, 1, 0);
            createInitial.Enabled = !application.Definitions.GetAll<MapDefinition>().Any();
        };
        EditorTheme.Apply(mapMenu);
    }

    private void OnNodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
    {
        worldTree.SelectedNode = e.Node;
        openButton.Enabled = e.Node.Tag is MapDefinition;
        if (e.Button == MouseButtons.Right)
            mapMenu.Show(worldTree, e.Location);
    }

    private void ActivateSelected()
    {
        if (worldTree.SelectedNode is { } node)
            ActivateNode(node);
    }

    private void ActivateNode(TreeNode? node)
    {
        if (node?.Tag is MapDefinition map)
            MapActivated?.Invoke(map);
    }

    private bool CanCreateRelative(MapDefinition? map, int dx, int dy)
    {
        if (map is null) return false;
        return MapWorldGrid.CanCreate(application.Definitions, map.GridX + dx, map.GridY + dy);
    }

    private void CreateRelative(int dx, int dy)
    {
        if (worldTree.SelectedNode?.Tag is not MapDefinition map) return;
        var x = map.GridX + dx;
        var y = map.GridY + dy;
        if (!MapWorldGrid.CanCreate(application.Definitions, x, y)) return;
        CreateRequested?.Invoke(x, y);
    }
}
