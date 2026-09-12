using NuevoMMO.Core;

namespace NuevoMMO.Client;

public class ClientEntity : IClientEntity
{
	public ClientEntity(EntityState state)
	{
		ArgumentNullException.ThrowIfNull(state);
		State = state;
	}

	public EntityState State { get; set; }
	public EntityId Id => State.Id;
	public EntityKind Kind => State.Kind;
	public string DisplayName => State.DisplayName;
	public ContentKey VisualKey => State.VisualKey;
	public Vector2Data Position => State.Position;
	public Vector2Data Velocity => State.Velocity;
	public Direction Facing => State.Facing;
	public MapInstanceId MapInstance => State.MapInstance;
	public DefinitionId? DefinitionId => State.Definition;
	public bool InView { get; set; } = true;
	public bool IsMoving => Velocity.X * Velocity.X + Velocity.Y * Velocity.Y > 0.0001f;
}

public class ClientLivingEntity : ClientEntity, IClientLivingEntity
{
	public ClientLivingEntity(EntityState state) : base(state) { }
}

public sealed class ClientPlayer : ClientEntity, IClientPlayer
{
	public ClientPlayer(EntityState state) : base(state) { }

	public EntityId? TargetId { get; private set; }
	public bool HasParty { get; set; }

	public bool TryTarget(EntityId id)
	{
		if (id.Value <= 0 || id == Id) return false;
		TargetId = id;
		return true;
	}

	public void ClearTarget() => TargetId = null;
}

public sealed class ClientMob : ClientLivingEntity
{
	public ClientMob(MobState state) : base(state) { }
	public DefinitionId MobDefinition => State is MobState mob ? mob.MobDefinition : DefinitionId ?? default;
}

public sealed class ClientNpc : ClientLivingEntity
{
	public ClientNpc(NpcState state) : base(state) { }
}

public sealed class ClientResource : ClientEntity, IClientResource
{
	public ClientResource(ResourceState state) : base(state) { }
	public DefinitionId ResourceDefinition => State is ResourceState resource ? resource.ResourceDefinition : DefinitionId ?? default;
}

public sealed class ClientProjectile : ClientEntity
{
	public ClientProjectile(ProjectileState state) : base(state) { }
}

public sealed class ClientWorldItem : ClientEntity
{
	public ClientWorldItem(WorldItemState state) : base(state) { }
}

public static class ClientEntityFactory
{
	public static ClientEntity FromState(EntityState state)
	{
		ArgumentNullException.ThrowIfNull(state);
		return state switch
		{
			PlayerState player => new ClientPlayer(player),
			MobState mob => new ClientMob(mob),
			NpcState npc => new ClientNpc(npc),
			ResourceState resource => new ClientResource(resource),
			ProjectileState projectile => new ClientProjectile(projectile),
			WorldItemState item => new ClientWorldItem(item),
			_ => new ClientEntity(state)
		};
	}
}
