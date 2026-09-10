using NuevoMMO.Core;

namespace NuevoMMO.Network;

public sealed record RegisterResult(bool Succeeded, string Error, AccountId Account) : IPacket;
