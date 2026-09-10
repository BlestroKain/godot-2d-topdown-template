namespace NuevoMMO.Network;

public sealed class HandlerRegistry<TContext>
{
    private readonly Dictionary<Type, Func<TContext, IPacket, CancellationToken, ValueTask>> handlers = [];
    public void Register<TPacket>(IPacketHandler<TContext, TPacket> handler) where TPacket : IPacket
        => handlers.Add(typeof(TPacket), (context, packet, token) => handler.HandleAsync(context, (TPacket)packet, token));
    internal bool TryGet(Type type, out Func<TContext, IPacket, CancellationToken, ValueTask>? handler) => handlers.TryGetValue(type, out handler);
}
