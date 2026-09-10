namespace NuevoMMO.Network;

public sealed class PacketDispatcher<TContext>(HandlerRegistry<TContext> registry, PacketDirection inboundDirection)
{
    public ValueTask DispatchAsync(TContext context, IPacket packet, CancellationToken cancellationToken = default)
    {
        var direction = PacketRegistry.Describe(packet).Direction;
        if (direction != inboundDirection && direction != PacketDirection.Bidirectional)
            throw new InvalidDataException("Dirección de paquete no permitida.");
        if (!registry.TryGet(packet.GetType(), out var handler) || handler is null)
            throw new InvalidDataException("No existe handler para el paquete.");
        return handler(context, packet, cancellationToken);
    }
}
