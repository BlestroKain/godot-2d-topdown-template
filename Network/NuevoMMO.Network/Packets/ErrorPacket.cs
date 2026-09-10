namespace NuevoMMO.Network;

public sealed record ErrorPacket(string Code, string Message, bool Fatal) : IPacket;
