namespace NuevoMMO.Network;

public interface IPacketHandler<in TContext, in TPacket> where TPacket : IPacket
{
    ValueTask HandleAsync(TContext context, TPacket packet, CancellationToken cancellationToken);
}
