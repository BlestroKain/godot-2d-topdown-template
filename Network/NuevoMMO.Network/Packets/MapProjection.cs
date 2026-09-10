using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed record MapProjection(DefinitionId Definition, MapInstanceId Instance, ContentKey VisualKey,
    BoundsData Bounds, float MovementSpeed, int TickMilliseconds, string ContentVersion);
