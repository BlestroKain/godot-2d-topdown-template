using NuevoMMO.Contracts;

namespace NuevoMMO.Domain;

public sealed class PlayerEntity
{
    public EntityId Id { get; }
    public CharacterId Character { get; }
    public MapInstanceId Instance { get; }
    public string Name { get; }
    public WorldPosition Position { get; private set; }
    public WorldPosition Velocity { get; private set; }
    public MovementInputBuffer Inputs { get; } = new();

    public PlayerEntity(EntityId id, CharacterId character, MapInstanceId instance, string name, WorldPosition spawn)
    {
        if (id.Value <= 0 || character.Value == Guid.Empty || instance.Value <= 0 || !spawn.IsFinite || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Entidad inválida.");
        Id = id; Character = character; Instance = instance; Name = name; Position = spawn;
    }

    public void ApplyMovement(WorldPosition position, WorldPosition velocity)
    {
        if (!position.IsFinite || !velocity.IsFinite) throw new ArgumentException("Movimiento inválido.");
        Position = position; Velocity = velocity;
    }
}
