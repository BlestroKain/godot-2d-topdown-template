namespace NuevoMMO.Network;

public sealed record ConnectRequest(string ClientVersion, ushort ProtocolVersion) : IPacket;
