namespace NuevoMMO.Core;

public sealed record MapPortal(DefinitionId SourceMapId, Vector2Data Source, DefinitionId DestinationMapId, Vector2Data Destination);
