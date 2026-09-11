namespace NuevoMMO.Network;

public sealed record PlayerStatsPacket(PlayerStatsSnapshot Stats) : IPacket;
