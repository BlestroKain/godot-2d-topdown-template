using NuevoMMO.Contracts;

namespace NuevoMMO.Application;

public sealed record WorldOptions(MapId Map, MapInstanceId Instance, float Width, float Height,
    WorldPosition Spawn, float Speed, int TickMilliseconds, float InterestRadius, int MaxPlayers);
