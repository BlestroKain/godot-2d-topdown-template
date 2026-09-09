namespace NuevoMMO.Contracts;

public interface IMessage;
public sealed record HandshakeRequest(string Name) : IMessage;
public sealed record HandshakeAccepted(EntityId Self, CharacterId Character, MapProjection Map) : IMessage;
public sealed record HandshakeRejected(string Reason) : IMessage;
public sealed record MoveCommand(long Number, float X, float Y) : IMessage;
public sealed record Ping(long Nonce) : IMessage;
public sealed record Pong(long Nonce) : IMessage;

// A projection, never the server's MapDefinition or PlayerEntity.
public sealed record MapProjection(MapId Id, MapInstanceId Instance, float Width, float Height,
    float MovementSpeed, int TickMilliseconds);
public sealed record EntityProjection(EntityId Id, string Name, WorldPosition Position,
    WorldPosition Velocity, string VisualKey);
public sealed record MovementCorrection(EntityId Self, WorldPosition Position, long LastProcessedInput);
public sealed record WorldSnapshot(long Tick, bool Full, MovementCorrection Correction,
    EntityProjection[] Upserts, EntityId[] Despawns) : IMessage;
