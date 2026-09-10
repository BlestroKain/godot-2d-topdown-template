namespace NuevoMMO.Network;

public sealed record DisconnectRequest(string Reason) : IPacket;
