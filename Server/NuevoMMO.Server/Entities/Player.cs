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
        ConfigureCollision(CanonicalCollisionProfiles.Player.ToRuntime());
        Progression = ProgressionRules.CreateInitial();
        ApplyInitialProgressionInvariant();
    }

    public AccountId AccountId { get; }
    public CharacterId CharacterId { get; }
    public PlayerProgressionState Progression { get; private set; }
    public int Level => Progression.Level;
    public long Experience => Progression.Experience;
    public int AttributePoints => Progression.AvailableAttributePoints;
    public Inventory Inventory { get; } = new();
    public Equipment Equipment { get; } = new();
    public Knowledge Knowledge { get; } = new();
    public ProfessionSet Professions { get; } = new();
    public TechniqueSet Techniques { get; } = new();
    public PlayerEventState Events { get; } = new();
    public EntityId? TargetId { get; set; }
    public MovementInputBuffer Inputs { get; } = new();
    public HashSet<EntityId> Interest { get; } = [];
    public bool DirtyPosition { get; private set; }

    public void SetProgression(PlayerProgressionState progression)
    {
        ProgressionRules.Validate(progression);
        Progression = progression;
    }

    public void ApplyMovement(Vector2Data position, Vector2Data velocity)
    {
        MoveTo(position, velocity);
        if (velocity.LengthSquared > 0) DirtyPosition = true;
    }

    public void TransferTo(MapInstanceId mapInstance, Vector2Data position, Direction facing = Direction.Down)
    {
        if (mapInstance.Value <= 0) throw new ArgumentException("MapInstanceId inválido.", nameof(mapInstance));
        if (!position.IsFinite) throw new ArgumentException("Posición inválida.", nameof(position));
        SetMapInstance(mapInstance);
        MoveTo(position, Vector2Data.Zero, facing);
        TargetId = null;
        Interest.Clear();
        Inputs.Reset();
        LeaveCombat();
        DirtyPosition = true;
    }

    public void MarkSaved() => DirtyPosition = false;

    public override EntityState ToState() => new PlayerState(Id, CharacterId, MapInstanceId, Position, Velocity,
        Direction, VisualKey, DisplayName);

    private void ApplyInitialProgressionInvariant()
    {
        var natural = Progression.NaturalAttributes;
        Stats.Primary.Strength = natural.Strength;
        Stats.Primary.Intelligence = natural.Intelligence;
        Stats.Primary.Agility = natural.Agility;
        Stats.Primary.Spirit = natural.Spirit;
        Stats.Primary.Vitality = natural.Vitality;
        var derived = CanonicalStatCalculator.Calculate(Progression.Level, Stats.Primary);
        SetMaximumVitals(derived.MaxHealth, derived.MaxMana, refill: true);
    }
}
