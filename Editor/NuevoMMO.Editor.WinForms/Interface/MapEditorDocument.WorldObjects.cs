using NuevoMMO.Core;
using System.Windows.Forms;

namespace NuevoMMO.Editor;

public sealed partial class MapEditorDocument
{
    private bool worldObjectHandlersBound;
    private Vector2Data? pendingPortalStart;
    private Vector2Data? pendingRegionStart;
    private Guid? selectedPortalObjectId;
    private Guid? selectedRegionObjectId;
    private Guid? selectedLightObjectId;
    private DefinitionId? selectedMapEventId;

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (worldObjectHandlersBound) return;
        worldObjectHandlersBound = true;
        viewport.EnableWorldObjectOverlays();
        viewport.WorldClicked += OnWorldObjectClicked;
        viewport.KeyDown += OnWorldObjectKeyDown;
        MapOpened += _ => ClearWorldObjectSelection();
        SelectedObjectChanged += value =>
        {
            if (value is MapContentPlacementDefinition or MapSpawnZoneDefinition or MapDocument)
                ClearWorldObjectSelection(invalidateProperties: false);
        };
    }

    private void OnWorldObjectClicked(Vector2Data world, MouseButtons button)
    {
        var editor = application;
        if (editor?.Maps.Document is not { } map) return;

        if (button == MouseButtons.Right || ActiveTool == MapEditorTool.Select)
        {
            if (TrySelectWorldObject(world)) return;
            if (button == MouseButtons.Right) return;
        }

        if (button != MouseButtons.Left || !map.Bounds.Contains(world)) return;
        switch (ActiveTool)
        {
            case MapEditorTool.Portal:
                AddPortalPoint(world);
                break;
            case MapEditorTool.Region:
                AddRegionPoint(world);
                break;
            case MapEditorTool.Light:
                AddLight(world);
                break;
            case MapEditorTool.Event:
                AddMapEvent(world);
                break;
        }
    }

    private bool TrySelectWorldObject(Vector2Data world)
    {
        if (viewport.HitTestMapEvent(world) is { } evt)
        {
            SelectMapEvent(evt);
            return true;
        }
        if (viewport.HitTestLight(world) is { } light)
        {
            SelectLight(light);
            return true;
        }
        if (viewport.HitTestPortal(world) is { } portal)
        {
            SelectPortal(portal);
            return true;
        }
        if (viewport.HitTestRegion(world) is { } region)
        {
            SelectRegion(region);
            return true;
        }
        return false;
    }

    private void AddPortalPoint(Vector2Data world)
    {
        var editor = RequireApplication();
        if (SelectedPlacementDefinitionId is not { } destinationMapId ||
            !editor.Definitions.TryGet<MapDefinition>(destinationMapId, out var destinationMap) || destinationMap is null)
        {
            EditorNotice?.Invoke("Seleccione un mapa destino antes de dibujar el portal.");
            return;
        }

        if (pendingPortalStart is null)
        {
            pendingPortalStart = world;
            EditorNotice?.Invoke("Portal: marque la esquina opuesta del área de activación.");
            return;
        }

        var start = pendingPortalStart.Value;
        pendingPortalStart = null;
        if (!TryRectangle(start, world, out var area))
        {
            EditorNotice?.Invoke("El área del portal es demasiado pequeña.");
            return;
        }

        var document = editor.Maps.Document!;
        var portal = new MapPortalDefinition(
            Guid.NewGuid(), area, destinationMap.Id, destinationMap.Spawn, Direction.Down);
        editor.History.Push(new ChangeSet(
            $"Crear portal a {destinationMap.Name}",
            () =>
            {
                if (document.Portals.All(value => value.Id != portal.Id)) document.Portals.Add(portal);
                editor.Dirty.Mark();
            },
            () =>
            {
                document.Portals.RemoveAll(value => value.Id == portal.Id);
                editor.Dirty.Mark();
            }));

        SelectPortal(portal);
        viewport.RefreshMap(clearAutotileCache: false);
        MapChanged?.Invoke();
        EditorNotice?.Invoke($"Portal creado → {destinationMap.Name} @ {destinationMap.Spawn}.");
    }

    private void AddRegionPoint(Vector2Data world)
    {
        var editor = RequireApplication();
        if (pendingRegionStart is null)
        {
            pendingRegionStart = world;
            EditorNotice?.Invoke("Region: marque la esquina opuesta.");
            return;
        }

        var start = pendingRegionStart.Value;
        pendingRegionStart = null;
        if (!TryRectangle(start, world, out var area))
        {
            EditorNotice?.Invoke("La región es demasiado pequeña.");
            return;
        }

        var document = editor.Maps.Document!;
        var index = 1;
        string key;
        do key = $"region_{index++:000}";
        while (document.Regions.Any(value => string.Equals(value.Key, key, StringComparison.OrdinalIgnoreCase)));
        var region = new MapRegionDefinition(Guid.NewGuid(), key, $"Region {index - 1:000}", area);

        editor.History.Push(new ChangeSet(
            $"Crear región {region.Key}",
            () =>
            {
                if (document.Regions.All(value => value.Id != region.Id)) document.Regions.Add(region);
                editor.Dirty.Mark();
            },
            () =>
            {
                document.Regions.RemoveAll(value => value.Id == region.Id);
                editor.Dirty.Mark();
            }));

        SelectRegion(region);
        viewport.RefreshMap(clearAutotileCache: false);
        MapChanged?.Invoke();
        EditorNotice?.Invoke($"Región creada: {region.Key}.");
    }

    private void AddLight(Vector2Data world)
    {
        var editor = RequireApplication();
        var document = editor.Maps.Document!;
        var defaultRadius = Math.Max(document.TileSize.X, document.TileSize.Y) * 3f;
        var light = new MapLightDefinition(Guid.NewGuid(), world, defaultRadius, 1f);

        editor.History.Push(new ChangeSet(
            $"Crear luz {light.Id}",
            () =>
            {
                if (document.Lights.All(value => value.Id != light.Id)) document.Lights.Add(light);
                editor.Dirty.Mark();
            },
            () =>
            {
                document.Lights.RemoveAll(value => value.Id == light.Id);
                editor.Dirty.Mark();
            }));

        SelectLight(light);
        viewport.RefreshMap(clearAutotileCache: false);
        MapChanged?.Invoke();
        EditorNotice?.Invoke($"Luz creada: radio técnico inicial {defaultRadius:0.##}." );
    }

    private void AddMapEvent(Vector2Data world)
    {
        var editor = RequireApplication();
        var map = editor.Maps.Document!;
        var index = 1;
        ContentKey key;
        do key = new ContentKey($"events.map_{index++:000}");
        while (editor.Definitions.Contains(key));

        var rootListId = Guid.NewGuid();
        var page = new EventPageDefinition(
            Guid.NewGuid(),
            EventTrigger.Action,
            commandLists: new Dictionary<Guid, EventCommandDefinition[]> { [rootListId] = [] },
            rootCommandListId: rootListId);
        var evt = new EventDefinition(
            DefinitionId.New(),
            key,
            $"Map Event {index - 1:000}",
            string.Empty,
            true,
            1,
            ["map_event"],
            EventScope.Map,
            new EventPlacementDefinition(map.Id, world, Direction.Down),
            [page]);

        editor.History.Push(new ChangeSet(
            $"Crear evento {evt.Key}",
            () =>
            {
                if (!editor.Definitions.Contains(evt.Id)) editor.Definitions.Register(evt);
                editor.Content.Persist(evt);
            },
            () =>
            {
                editor.Definitions.Unregister(evt.Id);
                editor.Dirty.Mark();
            }));

        SelectMapEvent(evt);
        viewport.RefreshMap(clearAutotileCache: false);
        MapChanged?.Invoke();
        EditorNotice?.Invoke($"Evento de mapa creado: {evt.Key}." );
    }

    private void OnWorldObjectKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            pendingPortalStart = null;
            pendingRegionStart = null;
            ClearWorldObjectSelection();
            return;
        }
        if (e.KeyCode != Keys.Delete) return;

        var editor = application;
        var document = editor?.Maps.Document;
        if (editor is null || document is null) return;

        if (selectedPortalObjectId is { } portalId)
        {
            var portal = document.Portals.FirstOrDefault(value => value.Id == portalId);
            if (portal is not null)
                PushDelete(editor, $"Borrar portal {portal.Id}", document.Portals, portal, static value => value.Id);
        }
        else if (selectedRegionObjectId is { } regionId)
        {
            var region = document.Regions.FirstOrDefault(value => value.Id == regionId);
            if (region is not null)
                PushDelete(editor, $"Borrar región {region.Key}", document.Regions, region, static value => value.Id);
        }
        else if (selectedLightObjectId is { } lightId)
        {
            var light = document.Lights.FirstOrDefault(value => value.Id == lightId);
            if (light is not null)
                PushDelete(editor, $"Borrar luz {light.Id}", document.Lights, light, static value => value.Id);
        }
        else if (selectedMapEventId is { } eventId && editor.Definitions.TryGet<EventDefinition>(eventId, out var evt) && evt is not null)
        {
            editor.History.Push(new ChangeSet(
                $"Borrar evento {evt.Key}",
                () => { editor.Definitions.Unregister(evt.Id); editor.Dirty.Mark(); },
                () => { if (!editor.Definitions.Contains(evt.Id)) editor.Definitions.Register(evt); editor.Dirty.Mark(); }));
        }
        else return;

        ClearWorldObjectSelection();
        viewport.RefreshMap(clearAutotileCache: false);
        MapChanged?.Invoke();
        e.Handled = true;
    }

    private void SelectPortal(MapPortalDefinition portal)
    {
        ClearSelection();
        ClearWorldObjectSelection(invalidateProperties: false);
        selectedPortalObjectId = portal.Id;
        viewport.SelectedPortalId = portal.Id;
        SelectedObjectChanged?.Invoke(portal);
    }

    private void SelectRegion(MapRegionDefinition region)
    {
        ClearSelection();
        ClearWorldObjectSelection(invalidateProperties: false);
        selectedRegionObjectId = region.Id;
        viewport.SelectedRegionId = region.Id;
        SelectedObjectChanged?.Invoke(region);
    }

    private void SelectLight(MapLightDefinition light)
    {
        ClearSelection();
        ClearWorldObjectSelection(invalidateProperties: false);
        selectedLightObjectId = light.Id;
        viewport.SelectedLightId = light.Id;
        SelectedObjectChanged?.Invoke(light);
    }

    private void SelectMapEvent(EventDefinition evt)
    {
        ClearSelection();
        ClearWorldObjectSelection(invalidateProperties: false);
        selectedMapEventId = evt.Id;
        viewport.SelectedEventId = evt.Id;
        SelectedObjectChanged?.Invoke(evt);
    }

    private void ClearWorldObjectSelection(bool invalidateProperties = true)
    {
        selectedPortalObjectId = null;
        selectedRegionObjectId = null;
        selectedLightObjectId = null;
        selectedMapEventId = null;
        viewport.ClearWorldObjectSelection();
        if (invalidateProperties) SelectedObjectChanged?.Invoke(application?.Maps.Document);
    }

    private static bool TryRectangle(Vector2Data start, Vector2Data end, out MapShapeDefinition area)
    {
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(end.Y - start.Y);
        if (width < 1f || height < 1f)
        {
            area = null!;
            return false;
        }
        area = new MapShapeDefinition(
            MapShapeKind.Rectangle,
            new Vector2Data((start.X + end.X) / 2f, (start.Y + end.Y) / 2f),
            new Vector2Data(width, height));
        return true;
    }

    private static void PushDelete<T>(
        EditorApplication editor,
        string description,
        List<T> list,
        T value,
        Func<T, Guid> id)
    {
        var key = id(value);
        editor.History.Push(new ChangeSet(
            description,
            () => { list.RemoveAll(item => id(item) == key); editor.Dirty.Mark(); },
            () => { if (list.All(item => id(item) != key)) list.Add(value); editor.Dirty.Mark(); }));
    }
}
