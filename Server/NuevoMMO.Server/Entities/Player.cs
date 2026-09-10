using NuevoMMO.Core;
using NuevoMMO.Server.Systems;

namespace NuevoMMO.Server.Entities;

public sealed class Player : LivingEntity
{
    public Player(EntityId id, AccountId account, CharacterId character, MapInstanceId mapInstance, Vector2Data position,
        ContentKey visualKey, string displayName)
        : base(id, mapInstance, position, visualKey, displayName)
    {
        if (account.Value == Guid.Empty) throw new ArgumentException("AccountId inválido.", nameof(account));
        if (character.Value == Guid.Empty) throw new ArgumentException("CharacterId inválido.", nameof(character));
        AccountId = account;
        CharacterId = character;
    }

    public AccountId AccountId { get; }
    public CharacterId CharacterId { get; }
    public int Level { get; set; } = 1;
    public long Experience { get; set; }
    public int AttributePoints { get; set; }
    public Inventory Inventory { get; } = new();
    public Equipment Equipment { get; } = new();
    public Knowledge Knowledge { get; } = new();
    public ProfessionSet Professions { get; } = new();
    public TechniqueSet Techniques { get; } = new();
    public MovementInputBuffer Inputs { get; } = new();
    public HashSet<EntityId> Interest { get; } = [];
    public bool DirtyPosition { get; private set; }

    public void ApplyMovement(Vector2Data position, Vector2Data velocity)
    {
        MoveTo(position, velocity);
        if (velocity.LengthSquared > 0) DirtyPosition = true;
    }

    public void MarkSaved() => DirtyPosition = false;

    public override EntityState ToState() => new PlayerState(Id, CharacterId, MapInstanceId, Position, Velocity,
        Direction, VisualKey, DisplayName);
}
