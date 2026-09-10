using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed record CharacterSelected(CharacterId Character) : IPacket;
