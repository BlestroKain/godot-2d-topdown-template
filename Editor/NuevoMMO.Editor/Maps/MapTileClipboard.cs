using NuevoMMO.Core;

namespace NuevoMMO.Editor;

public sealed record MapTileClipboard(Vector2IntData Origin, MapTilePlacementDefinition[] Tiles);
