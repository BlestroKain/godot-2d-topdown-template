namespace NuevoMMO.Network;

public sealed record CharacterCreated(CharacterSummary Character) : IPacket;
