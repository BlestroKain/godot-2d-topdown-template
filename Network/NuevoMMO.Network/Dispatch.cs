namespace NuevoMMO.Network;

public interface IPacketHandler<in TContext, in TPacket> where TPacket : IPacket
{
    ValueTask HandleAsync(TContext context, TPacket packet, CancellationToken cancellationToken);
}

public sealed class HandlerRegistry<TContext>
{
    private readonly Dictionary<Type, Func<TContext, IPacket, CancellationToken, ValueTask>> handlers = [];
    public void Register<TPacket>(IPacketHandler<TContext, TPacket> handler) where TPacket : IPacket
        => handlers.Add(typeof(TPacket), (context, packet, token) => handler.HandleAsync(context, (TPacket)packet, token));
    internal bool TryGet(Type type, out Func<TContext, IPacket, CancellationToken, ValueTask>? handler) => handlers.TryGetValue(type, out handler);
}

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
