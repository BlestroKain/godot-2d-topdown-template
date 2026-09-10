namespace NuevoMMO.Network;

public sealed record MoveRequest(InputFrame Input) : IPacket;
