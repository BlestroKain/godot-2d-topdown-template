namespace NuevoMMO.Network;

public sealed record CharacterListResult(CharacterSummary[] Characters) : IPacket;
