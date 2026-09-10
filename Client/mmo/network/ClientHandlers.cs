using NuevoMMO.Client;
using NuevoMMO.Network;

namespace NuevoMMO.GodotClient;

public static class ClientPacketRoutes
{
    public static void Apply(ClientWorldState state, IPacket packet, double now)
    {
        switch (packet)
        {
            case MapLoadPacket map: state.Start(map); break;
            case EntityStatePacket snapshot: state.Apply(snapshot, now); break;
        }
    }
}
