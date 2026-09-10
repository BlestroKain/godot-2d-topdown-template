namespace NuevoMMO.Core;

public enum MapShapeKind : byte
{
    Rectangle,
    Circle,
    Polygon
}

public sealed record MapShapeDefinition
{
    public MapShapeDefinition(
        MapShapeKind kind,
        Vector2Data center,
        Vector2Data size = default,
        float radius = 0,
        Vector2Data[]? points = null)
    {
        if (!center.IsFinite) throw new ArgumentException("Center no finito.", nameof(center));
        if (!size.IsFinite) throw new ArgumentException("Size no finito.", nameof(size));
        if (!float.IsFinite(radius) || radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
        var normalizedPoints = points?.ToArray() ?? [];
        if (normalizedPoints.Any(static point => !point.IsFinite))
            throw new ArgumentException("Points contiene valores no finitos.", nameof(points));

        switch (kind)
        {
            case MapShapeKind.Rectangle when size.X <= 0 || size.Y <= 0:
                throw new ArgumentException("Rectangle requiere Size positiva.", nameof(size));
            case MapShapeKind.Circle when radius <= 0:
                throw new ArgumentException("Circle requiere Radius positivo.", nameof(radius));
            case MapShapeKind.Polygon when normalizedPoints.Length < 3:
                throw new ArgumentException("Polygon requiere al menos tres puntos.", nameof(points));
        }

        Kind = kind;
        Center = center;
        Size = size;
        Radius = radius;
        Points = normalizedPoints;
    }

    public MapShapeKind Kind { get; }
    public Vector2Data Center { get; }
    public Vector2Data Size { get; }
    public float Radius { get; }
    public Vector2Data[] Points { get; }
}

public sealed record MapTilePlacementDefinition
{
    public MapTilePlacementDefinition(
        Vector2IntData cell,
        ContentKey tilesetKey,
        Vector2IntData atlasCell,
        int alternative = 0,
        int rotationQuarterTurns = 0,
        bool flipHorizontal = false,
        bool flipVertical = false)
    {
        if (tilesetKey.IsEmpty) throw new ArgumentException("TilesetKey vacío.", nameof(tilesetKey));
        if (alternative < 0) throw new ArgumentOutOfRangeException(nameof(alternative));
        if (rotationQuarterTurns is < 0 or > 3) throw new ArgumentOutOfRangeException(nameof(rotationQuarterTurns));

        Cell = cell;
        TilesetKey = tilesetKey;
        AtlasCell = atlasCell;
        Alternative = alternative;
        RotationQuarterTurns = rotationQuarterTurns;
        FlipHorizontal = flipHorizontal;
        FlipVertical = flipVertical;
    }

    public Vector2IntData Cell { get; }
    public ContentKey TilesetKey { get; }
    public Vector2IntData AtlasCell { get; }
    public int Alternative { get; }
    public int RotationQuarterTurns { get; }
    public bool FlipHorizontal { get; }
    public bool FlipVertical { get; }
}

public sealed record MapLayerDefinition
{
    public MapLayerDefinition(
        string key,
        int order,
        MapTilePlacementDefinition[]? tiles = null,
        bool visible = true,
        float parallaxFactor = 1,
        Dictionary<string, float>? parameters = null)
    {
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Layer key requerida.", nameof(key));
        if (!float.IsFinite(parallaxFactor) || parallaxFactor < 0) throw new ArgumentOutOfRangeException(nameof(parallaxFactor));
        var normalizedTiles = tiles?.ToArray() ?? [];
        if (normalizedTiles.Select(static tile => tile.Cell).Distinct().Count() != normalizedTiles.Length)
            throw new ArgumentException("Una capa no puede contener más de un tile en la misma celda.", nameof(tiles));

        Key = key.Trim();
        Order = order;
        Tiles = normalizedTiles;
        Visible = visible;
        ParallaxFactor = parallaxFactor;
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public string Key { get; }
    public int Order { get; }
    public MapTilePlacementDefinition[] Tiles { get; }
    public bool Visible { get; }
    public float ParallaxFactor { get; }
    public Dictionary<string, float> Parameters { get; }
}

public sealed record MapCollisionDefinition
{
    public MapCollisionDefinition(
        Guid id,
        MapShapeDefinition shape,
        bool blocksMovement = true,
        bool blocksProjectiles = true,
        bool blocksVision = false,
        bool navigationObstacle = true,
        Dictionary<string, float>? parameters = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Collision ID vacío.", nameof(id));
        Id = id;
        Shape = shape ?? throw new ArgumentNullException(nameof(shape));
        BlocksMovement = blocksMovement;
        BlocksProjectiles = blocksProjectiles;
        BlocksVision = blocksVision;
        NavigationObstacle = navigationObstacle;
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public Guid Id { get; }
    public MapShapeDefinition Shape { get; }
    public bool BlocksMovement { get; }
    public bool BlocksProjectiles { get; }
    public bool BlocksVision { get; }
    public bool NavigationObstacle { get; }
    public Dictionary<string, float> Parameters { get; }
}

public sealed record MapContentPlacementDefinition
{
    public MapContentPlacementDefinition(
        Guid id,
        SpawnEntityKind kind,
        DefinitionId definitionId,
        Vector2Data position,
        Direction direction = Direction.Down,
        Dictionary<string, float>? parameters = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Placement ID vacío.", nameof(id));
        if (definitionId.IsEmpty) throw new ArgumentException("DefinitionId vacío.", nameof(definitionId));
        if (!position.IsFinite) throw new ArgumentException("Position no finita.", nameof(position));

        Id = id;
        Kind = kind;
        DefinitionId = definitionId;
        Position = position;
        Direction = direction;
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public Guid Id { get; }
    public SpawnEntityKind Kind { get; }
    public DefinitionId DefinitionId { get; }
    public Vector2Data Position { get; }
    public Direction Direction { get; }
    public Dictionary<string, float> Parameters { get; }
}

public sealed record MapSpawnZoneDefinition
{
    public MapSpawnZoneDefinition(
        Guid id,
        DefinitionId spawnTableId,
        MapShapeDefinition area,
        int maximumAliveOverride = 0,
        Dictionary<string, float>? parameters = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("SpawnZone ID vacío.", nameof(id));
        if (spawnTableId.IsEmpty) throw new ArgumentException("SpawnTableId vacío.", nameof(spawnTableId));
        if (maximumAliveOverride < 0) throw new ArgumentOutOfRangeException(nameof(maximumAliveOverride));

        Id = id;
        SpawnTableId = spawnTableId;
        Area = area ?? throw new ArgumentNullException(nameof(area));
        MaximumAliveOverride = maximumAliveOverride;
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public Guid Id { get; }
    public DefinitionId SpawnTableId { get; }
    public MapShapeDefinition Area { get; }
    public int MaximumAliveOverride { get; }
    public Dictionary<string, float> Parameters { get; }
}

public sealed record MapPortalDefinition
{
    public MapPortalDefinition(
        Guid id,
        MapShapeDefinition triggerArea,
        DefinitionId destinationMapId,
        Vector2Data destination,
        Direction destinationDirection = Direction.Down,
        ConditionGroupDefinition? requirements = null,
        DefinitionId? eventId = null,
        ContentKey? transitionKey = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Portal ID vacío.", nameof(id));
        if (destinationMapId.IsEmpty) throw new ArgumentException("DestinationMapId vacío.", nameof(destinationMapId));
        if (!destination.IsFinite) throw new ArgumentException("Destination no finita.", nameof(destination));
        if (eventId is { } evt && evt.IsEmpty) throw new ArgumentException("EventId vacío.", nameof(eventId));
        if (transitionKey is { } transition && transition.IsEmpty) throw new ArgumentException("TransitionKey vacío.", nameof(transitionKey));

        Id = id;
        TriggerArea = triggerArea ?? throw new ArgumentNullException(nameof(triggerArea));
        DestinationMapId = destinationMapId;
        Destination = destination;
        DestinationDirection = destinationDirection;
        Requirements = requirements ?? ConditionGroupDefinition.Empty;
        EventId = eventId;
        TransitionKey = transitionKey;
    }

    public Guid Id { get; }
    public MapShapeDefinition TriggerArea { get; }
    public DefinitionId DestinationMapId { get; }
    public Vector2Data Destination { get; }
    public Direction DestinationDirection { get; }
    public ConditionGroupDefinition Requirements { get; }
    public DefinitionId? EventId { get; }
    public ContentKey? TransitionKey { get; }
}

public sealed record MapRegionDefinition
{
    public MapRegionDefinition(
        Guid id,
        string key,
        string name,
        MapShapeDefinition area,
        string[]? tags = null,
        Dictionary<string, DefinitionId>? eventHooks = null,
        Dictionary<string, float>? parameters = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Region ID vacío.", nameof(id));
        if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Region key requerida.", nameof(key));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Region name requerido.", nameof(name));

        Id = id;
        Key = key.Trim();
        Name = name.Trim();
        Area = area ?? throw new ArgumentNullException(nameof(area));
        Tags = DefinitionCollectionGuards.CopyStrings(tags);
        EventHooks = DefinitionModelGuards.CopyDefinitionHooks(eventHooks, nameof(eventHooks));
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public Guid Id { get; }
    public string Key { get; }
    public string Name { get; }
    public MapShapeDefinition Area { get; }
    public string[] Tags { get; }
    public Dictionary<string, DefinitionId> EventHooks { get; }
    public Dictionary<string, float> Parameters { get; }
}

public sealed record MapLightDefinition
{
    public MapLightDefinition(
        Guid id,
        Vector2Data position,
        float radius,
        float intensity = 1,
        ContentKey? visualKey = null,
        Dictionary<string, float>? parameters = null)
    {
        if (id == Guid.Empty) throw new ArgumentException("Light ID vacío.", nameof(id));
        if (!position.IsFinite) throw new ArgumentException("Position no finita.", nameof(position));
        if (!float.IsFinite(radius) || radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        if (!float.IsFinite(intensity) || intensity < 0) throw new ArgumentOutOfRangeException(nameof(intensity));
        if (visualKey is { } visual && visual.IsEmpty) throw new ArgumentException("VisualKey vacío.", nameof(visualKey));

        Id = id;
        Position = position;
        Radius = radius;
        Intensity = intensity;
        VisualKey = visualKey;
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public Guid Id { get; }
    public Vector2Data Position { get; }
    public float Radius { get; }
    public float Intensity { get; }
    public ContentKey? VisualKey { get; }
    public Dictionary<string, float> Parameters { get; }
}

public sealed record MapEnvironmentDefinition
{
    public MapEnvironmentDefinition(
        bool isIndoors = false,
        float brightnessPercent = 100,
        ContentKey? musicKey = null,
        ContentKey? ambientSoundKey = null,
        ContentKey? panoramaKey = null,
        ContentKey? fogKey = null,
        ContentKey? overlayKey = null,
        ContentKey? weatherKey = null,
        Vector2Data fogVelocity = default,
        Vector2Data weatherVelocity = default,
        float weatherIntensity = 0,
        float playerLightRadius = 0,
        Dictionary<string, float>? parameters = null)
    {
        if (!float.IsFinite(brightnessPercent) || brightnessPercent < 0) throw new ArgumentOutOfRangeException(nameof(brightnessPercent));
        if (!fogVelocity.IsFinite) throw new ArgumentException("FogVelocity no finita.", nameof(fogVelocity));
        if (!weatherVelocity.IsFinite) throw new ArgumentException("WeatherVelocity no finita.", nameof(weatherVelocity));
        if (!float.IsFinite(weatherIntensity) || weatherIntensity < 0) throw new ArgumentOutOfRangeException(nameof(weatherIntensity));
        if (!float.IsFinite(playerLightRadius) || playerLightRadius < 0) throw new ArgumentOutOfRangeException(nameof(playerLightRadius));

        ValidateKey(musicKey, nameof(musicKey));
        ValidateKey(ambientSoundKey, nameof(ambientSoundKey));
        ValidateKey(panoramaKey, nameof(panoramaKey));
        ValidateKey(fogKey, nameof(fogKey));
        ValidateKey(overlayKey, nameof(overlayKey));
        ValidateKey(weatherKey, nameof(weatherKey));

        IsIndoors = isIndoors;
        BrightnessPercent = brightnessPercent;
        MusicKey = musicKey;
        AmbientSoundKey = ambientSoundKey;
        PanoramaKey = panoramaKey;
        FogKey = fogKey;
        OverlayKey = overlayKey;
        WeatherKey = weatherKey;
        FogVelocity = fogVelocity;
        WeatherVelocity = weatherVelocity;
        WeatherIntensity = weatherIntensity;
        PlayerLightRadius = playerLightRadius;
        Parameters = DefinitionModelGuards.CopyFinite(parameters, nameof(parameters));
    }

    public bool IsIndoors { get; }
    public float BrightnessPercent { get; }
    public ContentKey? MusicKey { get; }
    public ContentKey? AmbientSoundKey { get; }
    public ContentKey? PanoramaKey { get; }
    public ContentKey? FogKey { get; }
    public ContentKey? OverlayKey { get; }
    public ContentKey? WeatherKey { get; }
    public Vector2Data FogVelocity { get; }
    public Vector2Data WeatherVelocity { get; }
    public float WeatherIntensity { get; }
    public float PlayerLightRadius { get; }
    public Dictionary<string, float> Parameters { get; }

    private static void ValidateKey(ContentKey? key, string parameterName)
    {
        if (key is { } value && value.IsEmpty) throw new ArgumentException("ContentKey vacío.", parameterName);
    }
}

public sealed record MapContentDefinition
{
    public MapContentDefinition(
        MapLayerDefinition[]? layers = null,
        MapCollisionDefinition[]? collisions = null,
        MapContentPlacementDefinition[]? placements = null,
        MapSpawnZoneDefinition[]? spawnZones = null,
        MapPortalDefinition[]? portals = null,
        MapRegionDefinition[]? regions = null,
        MapLightDefinition[]? lights = null,
        MapEnvironmentDefinition? environment = null,
        Dictionary<string, string>? metadata = null)
    {
        Layers = layers?.OrderBy(static layer => layer.Order).ToArray() ?? [];
        Collisions = collisions?.ToArray() ?? [];
        Placements = placements?.ToArray() ?? [];
        SpawnZones = spawnZones?.ToArray() ?? [];
        Portals = portals?.ToArray() ?? [];
        Regions = regions?.ToArray() ?? [];
        Lights = lights?.ToArray() ?? [];
        Environment = environment ?? new MapEnvironmentDefinition();
        Metadata = DefinitionCollectionGuards.CopyText(metadata, nameof(metadata));

        EnsureUnique(Layers.Select(static layer => layer.Key), nameof(layers));
        EnsureUnique(Collisions.Select(static value => value.Id), nameof(collisions));
        EnsureUnique(Placements.Select(static value => value.Id), nameof(placements));
        EnsureUnique(SpawnZones.Select(static value => value.Id), nameof(spawnZones));
        EnsureUnique(Portals.Select(static value => value.Id), nameof(portals));
        EnsureUnique(Regions.Select(static value => value.Id), nameof(regions));
        EnsureUnique(Lights.Select(static value => value.Id), nameof(lights));
    }

    public MapLayerDefinition[] Layers { get; }
    public MapCollisionDefinition[] Collisions { get; }
    public MapContentPlacementDefinition[] Placements { get; }
    public MapSpawnZoneDefinition[] SpawnZones { get; }
    public MapPortalDefinition[] Portals { get; }
    public MapRegionDefinition[] Regions { get; }
    public MapLightDefinition[] Lights { get; }
    public MapEnvironmentDefinition Environment { get; }
    public Dictionary<string, string> Metadata { get; }

    private static void EnsureUnique<T>(IEnumerable<T> values, string parameterName) where T : notnull
    {
        var array = values.ToArray();
        if (array.Distinct().Count() != array.Length)
            throw new ArgumentException("La colección contiene identificadores duplicados.", parameterName);
    }
}
