namespace NuevoMMO.Network;

public static class HandshakeRules
{
    public const int MaxClientVersionLength = 64;
    public static bool IsCompatible(ushort protocolVersion) => protocolVersion == ProtocolVersion.Current;
}
