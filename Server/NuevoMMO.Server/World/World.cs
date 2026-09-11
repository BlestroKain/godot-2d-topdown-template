using NuevoMMO.Core;
using NuevoMMO.Network;
using NuevoMMO.Server.Entities;
using NuevoMMO.Server.Systems;

namespace NuevoMMO.Server.World;

public sealed class EntityRegistry
{
    private readonly Dictionary<EntityId, Entity> entities = [];
    public int Count => entities.Count;
    public IEnumerable<Entity> All => entities.Values;
    public IEnumerable<Entity> Enumerate() => entities.Values;
    public void Add(Entity entity)
    {
        if (!entities.TryAdd(entity.Id, entity)) throw new InvalidOperationException("EntityId duplicado.");
    }
    public bool Remove(EntityId id, out Entity? entity) => entities.Remove(id, out entity);
    public Entity Get(EntityId id) => entities.TryGetValue(id, out var entity) ? entity : throw new KeyNotFoundException("Entidad inexistente.");
    public T Get<T>(EntityId id) where T : Entity => entities.TryGetValue(id, out var entity) && entity is T typed
        ? typed : throw new KeyNotFoundException("Entidad inexistente o de tipo incorrecto.");
    public bool TryGet(EntityId id, out Entity? entity) => entities.TryGetValue(id, out entity);
    public void Clear() => entities.Clear();
}

public sealed class SpatialIndex(float cellSize)
{
    private readonly Dictionary<(int X, int Y), HashSet<EntityId>> cells = [];
    private readonly Dictionary<EntityId, (int X, int Y)> locations = [];
    private (int X, int Y) Cell(Vector2Data position) => ((int)MathF.Floor(position.X / cellSize), (int)MathF.Floor(position.Y / cellSize));
    public void Update(EntityId id, Vector2Data position)
    {
        var cell = Cell(position);
        if (locations.TryGetValue(id, out var previous) && previous == cell) return;
        Remove(id);
        if (!cells.TryGetValue(cell, out var bucket)) cells[cell] = bucket = [];
        bucket.Add(id);
        locations[id] = cell;
    }
    public void Remove(EntityId id)
    {
        if (!locations.Remove(id, out var cell)) return;
        cells[cell].Remove(id);
        if (cells[cell].Count == 0) cells.Remove(cell);
    }
    public IEnumerable<EntityId> Query(Vector2Data center, float radius)
    {
        var min = Cell(new(center.X - radius, center.Y - radius));
        var max = Cell(new(center.X + radius, center.Y + radius));
        for (var x = min.X; x <= max.X; x++)
            for (var y = min.Y; y <= max.Y; y++)
                if (cells.TryGetValue((x, y), out var bucket))
                    foreach (var id in bucket) yield return id;
    }
}

public sealed class InterestManager(SpatialIndex spatialIndex, float radius)
{
    public IEnumerable<Entity> Relevant(Player viewer, EntityRegistry registry)
    {
        foreach (var id in spatialIndex.Query(viewer.Position, radius))
        {
            if (!registry.TryGet(id, out var entity) || entity is null || entity.MapInstanceId != viewer.MapInstanceId) continue;
            var dx = entity.Position.X - viewer.Position.X;
            var dy = entity.Position.Y - viewer.Position.Y;
            if (dx * dx + dy * dy <= radius * radius) yield return entity;
        }
    }
}

public sealed class SpawnManager
{
    private long nextEntity;
    public EntityId NextId() => new(Interlocked.Increment(ref nextEntity));
    public Player Player(CharacterSpawn spawn, MapInstanceId instance) => new(NextId(), spawn.Account, spawn.Character, instance,
        spawn.Position, new("template.player"), spawn.Name);
    public Mob Mob(MobDefinition definition, MapInstanceId instance, Vector2Data position) => new(NextId(), definition,
        instance, position);
    public Npc Npc(NpcDefinition definition, MapInstanceId instance, Vector2Data position) => new(NextId(), definition,
        instance, position);
    public ResourceEntity Resource(ResourceDefinition definition, MapInstanceId instance, Vector2Data position)
    {
        var health = definition.Harvest.HealthRange is { } range
            ? Math.Max(1f, range.Maximum)
            : 1f;
        return new(NextId(), definition, instance, position, health);
    }
}

public sealed class MapInstance(MapInstanceId id, MapDefinition definition, float interestRadius)
{
    public MapInstanceId Id { get; } = id.Value > 0 ? id : throw new ArgumentException("Instancia inválida.");
    public MapDefinition Definition { get; } = definition;
    public EntityRegistry Entities { get; } = new();
    public SpatialIndex Spatial { get; } = new(interestRadius);
    public void Add(Entity entity) { Entities.Add(entity); Spatial.Update(entity.Id, entity.Position); }
    public bool Remove(EntityId id, out Entity? entity) { Spatial.Remove(id); return Entities.Remove(id, out entity); }
    public void Refresh(Entity entity) => Spatial.Update(entity.Id, entity.Position);
}

public sealed class WorldManager
{
    private readonly Dictionary<MapInstanceId, MapInstance> maps = [];
    public int Count => maps.Count;
    public IEnumerable<MapInstance> All => maps.Values;
    public void Add(MapInstance map) { if (!maps.TryAdd(map.Id, map)) throw new InvalidOperationException("Instancia duplicada."); }
    public MapInstance Get(MapInstanceId id) => maps.TryGetValue(id, out var map) ? map : throw new KeyNotFoundException("Instancia inexistente.");
    public bool TryGet(MapInstanceId id, out MapInstance? map) => maps.TryGetValue(id, out map);
}

public enum PlayerSessionState { Connected, ProtocolAccepted, Authenticated, CharacterSelected, WaitingForMap, InWorld, Disconnected }

public sealed class PlayerSession(ConnectionId connection)
{
    public ConnectionId Connection { get; } = connection;
    public PlayerSessionState State { get; set; } = PlayerSessionState.Connected;
    public AccountId Account { get; set; }
    public SessionId Session { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public CharacterId Character { get; set; }
    public Player? Player { get; set; }
    public Dictionary<EntityId, EntityState> Baseline { get; } = [];
    public bool HasSnapshot { get; set; }
}

public sealed record CharacterSpawn(AccountId Account, CharacterId Character, string Name, DefinitionId MapDefinition, Vector2Data Position);

public sealed record WorldOptions(MapInstanceId Instance, float MovementSpeed, float MobSpeed, int TickMilliseconds,
    float InterestRadius, int MaxPlayers);

public sealed class WorldRuntime
{
    private readonly object gate = new();
    private readonly WorldOptions options;
    private readonly MapInstance map;
    private readonly MovementSystem movement;
    private readonly MobMovementSystem mobMovement;
    private readonly ProjectileSystem projectiles;
    private readonly GameSystems? systems;
    private readonly InterestManager interest;
    private readonly SpawnManager spawns = new();
    private readonly Dictionary<ConnectionId, PlayerSession> sessions = [];
    private long tick;
    public int TickMilliseconds => options.TickMilliseconds;
    public int PlayerCount { get { lock (gate) return sessions.Values.Count(value => value.State == PlayerSessionState.InWorld); } }
    public int EntityCount { get { lock (gate) return map.Entities.Count; } }
    public long Tick { get { lock (gate) return tick; } }
    public MapDefinition Map => map.Definition;
    public MapInstanceId Instance => map.Id;
    public MapInstance MapInstance => map;
    public GameSystems? Systems => systems;

    public WorldRuntime(MapDefinition definition, WorldOptions options, IMobMovementPolicy mobPolicy, GameSystems? systems = null)
    {
        if (options.MaxPlayers is < 1 or > 4096 || options.InterestRadius <= 0 || !float.IsFinite(options.InterestRadius))
            throw new ArgumentException("Opciones de mundo inválidas.");
        this.options = options;
        this.systems = systems;
        map = new(options.Instance, definition, options.InterestRadius);
        movement = new(options.MovementSpeed, options.TickMilliseconds);
        mobMovement = new(options.MobSpeed, options.TickMilliseconds, mobPolicy);
        projectiles = systems?.Projectiles ?? new ProjectileSystem();
        interest = new(map.Spatial, options.InterestRadius);
        systems?.Techniques.BindWorld(new TechniqueMapAccess(this));
    }

    public WorldRuntime(MapDefinition definition, MobDefinition mobDefinition, WorldOptions options, IMobMovementPolicy mobPolicy,
        Vector2Data mobSpawn, GameSystems? systems = null)
        : this(definition, options, mobPolicy, systems)
    {
        map.Add(spawns.Mob(mobDefinition, map.Id, mobSpawn));
    }

    public Mob SpawnMob(MobDefinition definition, Vector2Data position)
    {
        ArgumentNullException.ThrowIfNull(definition);
        lock (gate)
        {
            var mob = spawns.Mob(definition, map.Id, map.Definition.Bounds.Clamp(position));
            map.Add(mob);
            return mob;
        }
    }

    public Npc SpawnNpc(NpcDefinition definition, Vector2Data position)
    {
        ArgumentNullException.ThrowIfNull(definition);
        lock (gate)
        {
            var npc = spawns.Npc(definition, map.Id, map.Definition.Bounds.Clamp(position));
            map.Add(npc);
            return npc;
        }
    }

    public ResourceEntity SpawnResource(ResourceDefinition definition, Vector2Data position)
    {
        ArgumentNullException.ThrowIfNull(definition);
        lock (gate)
        {
            var resource = spawns.Resource(definition, map.Id, map.Definition.Bounds.Clamp(position));
            map.Add(resource);
            return resource;
        }
    }

    public PlayerSession AddConnection(ConnectionId connection)
    {
        lock (gate)
        {
            if (sessions.Count >= options.MaxPlayers || sessions.ContainsKey(connection)) throw new InvalidOperationException("Sesión no disponible.");
            var session = new PlayerSession(connection);
            sessions.Add(connection, session);
            return session;
        }
    }

    public PlayerSession Session(ConnectionId connection)
    {
        lock (gate) return sessions.TryGetValue(connection, out var session) ? session : throw new KeyNotFoundException("Conexión inexistente.");
    }

    public bool TrySession(ConnectionId connection, out PlayerSession? session)
    {
        lock (gate) return sessions.TryGetValue(connection, out session);
    }

    public void AcceptProtocol(PlayerSession session)
    {
        lock (gate)
        {
            if (session.State != PlayerSessionState.Connected) throw new InvalidOperationException("Protocolo ya aceptado.");
            session.State = PlayerSessionState.ProtocolAccepted;
        }
    }

    public void Authenticate(PlayerSession session, AccountId account, SessionId sessionId, string sessionToken)
    {
        lock (gate)
        {
            if (session.State != PlayerSessionState.ProtocolAccepted) throw new InvalidOperationException("Autenticación fuera de orden.");
            session.Account = account;
            session.Session = sessionId;
            session.SessionToken = sessionToken;
            session.State = PlayerSessionState.Authenticated;
        }
    }

    public Player Join(PlayerSession session, CharacterSpawn character)
    {
        lock (gate)
        {
            if (session.State != PlayerSessionState.Authenticated || character.MapDefinition != map.Definition.Id)
                throw new InvalidOperationException("Selección de personaje inválida.");
            var position = IsValidPosition(character.Position) ? character.Position : map.Definition.Spawn;
            var player = spawns.Player(character with { Position = position }, map.Id);
            player.Techniques.Learn(CanonicalCombatContent.BasicAttackId);
            map.Add(player);
            session.Character = character.Character;
            session.Player = player;
            session.State = PlayerSessionState.WaitingForMap;
            return player;
        }
    }

    public void Activate(PlayerSession session, MapInstanceId instance)
    {
        lock (gate)
        {
            if (session.State != PlayerSessionState.WaitingForMap || session.Player is null || instance != map.Id)
                throw new InvalidOperationException("Entrada al mapa inválida.");
            session.State = PlayerSessionState.InWorld;
        }
    }

    public void SubmitMovement(PlayerSession session, InputFrame input)
    {
        lock (gate)
        {
            if (session.State != PlayerSessionState.InWorld || session.Player is null) throw new InvalidOperationException("Jugador fuera del mundo.");
            session.Player.Inputs.Enqueue(input);
        }
    }

    public void SetTarget(PlayerSession session, EntityId target)
    {
        lock (gate)
        {
            EnsureInWorld(session);
            if (target.Value <= 0)
            {
                session.Player!.TargetId = null;
                return;
            }

            if (!map.Entities.TryGet(target, out var entity) || entity is null || entity.MapInstanceId != map.Id)
                throw new InvalidOperationException("Objetivo inexistente.");
            session.Player!.TargetId = target;
        }
    }

    public InteractionOutcome Interact(PlayerSession session, EntityId targetId)
    {
        if (systems is null) throw new InvalidOperationException("GameSystems no está configurado en este WorldRuntime.");
        lock (gate)
        {
            EnsureInWorld(session);
            var player = session.Player!;
            var now = checked(tick * options.TickMilliseconds);
            Entity? target = null;
            if (targetId.Value > 0)
            {
                if (!map.Entities.TryGet(targetId, out target) || target is null)
                    return InteractionOutcome.Fail("El objetivo ya no existe.");
            }
            else
                target = NearestInteractable(player);

            if (target is not null && !systems.Interactions.CanReach(player, target))
                return InteractionOutcome.Fail("Fuera de alcance.");

            if (target is WorldItem worldItem)
            {
                var picked = systems.Interactions.TryPickup(player, worldItem, now);
                if (picked.Success) map.Remove(worldItem.Id, out _);
                return picked.Success
                    ? InteractionOutcome.Ok(string.IsNullOrWhiteSpace(picked.Message) ? "Recogido." : picked.Message)
                    : InteractionOutcome.Fail(picked.Message);
            }

            if (target is ResourceEntity resource)
            {
                var harvest = systems.Interactions.TryHarvest(player, resource, 1f, now);
                return harvest.Success ? InteractionOutcome.Ok(harvest.Message) : InteractionOutcome.Fail(harvest.Message);
            }

            if (target is Npc)
            {
                var evt = systems.Events.MapEvents(map.Definition.Id)
                    .FirstOrDefault(definition => definition.Placement is { } placement
                        && DistanceSquared(placement.Position, target.Position) <= 4f * 4f);
                if (evt is not null)
                    return ToOutcome(systems.Events.TryTrigger(player, evt, EventTrigger.Action, now));
                return InteractionOutcome.Ok($"{target.DisplayName} no tiene diálogo.");
            }

            foreach (var evt in systems.Events.MapEvents(map.Definition.Id))
            {
                if (evt.Placement is not { } placement) continue;
                var page = systems.Events.ActivePage(evt, player);
                var radius = page?.InteractionRadius ?? 32f;
                if (DistanceSquared(player.Position, placement.Position) > radius * radius) continue;
                return ToOutcome(systems.Events.TryTrigger(player, evt, EventTrigger.Action, now));
            }

            return InteractionOutcome.Fail("Nada que interactuar aquí.");
        }
    }

    public TechniqueUseResult BasicAttack(PlayerSession session, EntityId targetId)
    {
        if (systems is null) throw new InvalidOperationException("GameSystems no está configurado.");
        lock (gate)
        {
            EnsureInWorld(session);
            var player = session.Player!;
            if (!map.Entities.TryGet(targetId, out var entity) || entity is not LivingEntity living)
                return TechniqueUseResult.Fail(TechniqueUseFailure.InvalidTarget, "Objetivo inválido.", CanonicalCombatContent.BasicAttackId);
            player.TargetId = targetId;
            if (!player.Techniques.Knows(CanonicalCombatContent.BasicAttackId))
                player.Techniques.Learn(CanonicalCombatContent.BasicAttackId);
            var now = checked(tick * options.TickMilliseconds);
            var result = systems.Techniques.BeginUse(player, CanonicalCombatContent.BasicAttackId, living, null, now);
            if (result.Success && !living.IsAlive)
                systems.Events.NotifyEntityDefeated(player, living, now);
            return result;
        }
    }

    public TechniqueUseResult UseTechnique(PlayerSession session, DefinitionId techniqueId, EntityId targetId, Vector2Data point)
    {
        if (systems is null) throw new InvalidOperationException("GameSystems no está configurado.");
        lock (gate)
        {
            EnsureInWorld(session);
            var player = session.Player!;
            LivingEntity? living = null;
            if (targetId.Value > 0)
            {
                if (!map.Entities.TryGet(targetId, out var entity) || entity is not LivingEntity target)
                    return TechniqueUseResult.Fail(TechniqueUseFailure.InvalidTarget, "Objetivo inválido.", techniqueId);
                living = target;
                player.TargetId = targetId;
            }

            var now = checked(tick * options.TickMilliseconds);
            var result = systems.Techniques.BeginUse(player, techniqueId, living, point, now);
            if (result.Success && living is { IsAlive: false })
                systems.Events.NotifyEntityDefeated(player, living, now);
            return result;
        }
    }

    private Entity? NearestInteractable(Player player)
    {
        Entity? best = null;
        var bestDistance = 64f * 64f;
        foreach (var entity in map.Entities.All)
        {
            if (entity.Id == player.Id) continue;
            if (entity is not (Npc or ResourceEntity or WorldItem)) continue;
            var distance = DistanceSquared(player.Position, entity.Position);
            if (distance > bestDistance) continue;
            best = entity;
            bestDistance = distance;
        }

        return best;
    }

    private static InteractionOutcome ToOutcome(EventExecutionResult result)
        => result.Success ? InteractionOutcome.Ok(string.Join('\n', result.Log)) : InteractionOutcome.Fail(result.Message);

    private static float DistanceSquared(Vector2Data left, Vector2Data right)
    {
        var x = left.X - right.X;
        var y = left.Y - right.Y;
        return x * x + y * y;
    }

    /// <summary>Inserta entidades runtime creadas por sistemas autorizados (loot, proyectiles, recursos, summons).</summary>
    public void AddEntity(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        lock (gate)
        {
            if (entity.MapInstanceId != map.Id) throw new InvalidOperationException("La entidad pertenece a otra instancia de mapa.");
            map.Add(entity);
        }
    }

    public InteractionResult TryPickup(PlayerSession session, EntityId worldItemId, long nowMilliseconds)
    {
        if (systems is null) throw new InvalidOperationException("GameSystems no está configurado en este WorldRuntime.");
        return TryPickup(session, worldItemId, systems.Interactions, nowMilliseconds);
    }

    public InteractionResult TryPickup(PlayerSession session, EntityId worldItemId, InteractionSystem interactions, long nowMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(interactions);
        lock (gate)
        {
            EnsureInWorld(session);
            if (!map.Entities.TryGet(worldItemId, out var entity) || entity is not WorldItem worldItem)
                return InteractionResult.Fail(InteractionFailure.TargetUnavailable, "El item ya no existe en el mundo.");

            var result = interactions.TryPickup(session.Player!, worldItem, nowMilliseconds);
            if (result.Success) map.Remove(worldItem.Id, out _);
            return result;
        }
    }

    public HarvestInteractionResult TryHarvest(PlayerSession session, EntityId resourceId, float workPower,
        long nowMilliseconds, float? range = null)
    {
        if (systems is null) throw new InvalidOperationException("GameSystems no está configurado en este WorldRuntime.");
        return TryHarvest(session, resourceId, systems.Interactions, workPower, nowMilliseconds, range);
    }

    public HarvestInteractionResult TryHarvest(PlayerSession session, EntityId resourceId, InteractionSystem interactions,
        float workPower, long nowMilliseconds, float? range = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(interactions);
        lock (gate)
        {
            EnsureInWorld(session);
            if (!map.Entities.TryGet(resourceId, out var entity) || entity is not ResourceEntity resource)
                return HarvestInteractionResult.Fail(InteractionFailure.TargetUnavailable, "El recurso ya no existe en el mundo.");
            return interactions.TryHarvest(session.Player!, resource, workPower, nowMilliseconds, range);
        }
    }

    public IReadOnlyDictionary<ConnectionId, EntityStatePacket> Step()
    {
        lock (gate)
        {
            tick++;
            var nowMilliseconds = checked(tick * options.TickMilliseconds);
            var despawn = new List<EntityId>();

            foreach (var entity in map.Entities.All.ToArray())
            {
                switch (entity)
                {
                    case Player player:
                        movement.Step(player, map.Definition);
                        break;
                    case Mob mob:
                        mobMovement.Step(mob, map.Definition, tick);
                        break;
                    case Projectile projectile:
                        projectiles.Step(projectile, map.Definition, options.TickMilliseconds);
                        ResolveProjectileImpacts(projectile, nowMilliseconds);
                        if (projectile.IsExpired) despawn.Add(projectile.Id);
                        break;
                    case AreaEffectEntity area:
                        var ticked = area.Advance(options.TickMilliseconds);
                        if (ticked) ApplyAreaTick(area, nowMilliseconds);
                        if (area.IsExpired) despawn.Add(area.Id);
                        break;
                    case ResourceEntity resource:
                        resource.TryRespawn(nowMilliseconds);
                        break;
                    case WorldItem worldItem when worldItem.PickedUp || worldItem.IsExpired(nowMilliseconds):
                        despawn.Add(worldItem.Id);
                        break;
                }

                if (!despawn.Contains(entity.Id)) map.Refresh(entity);
            }

            foreach (var id in despawn.Distinct())
            {
                if (map.Remove(id, out var removed) && removed is not null)
                    systems?.OnEntityRemoved(removed);
            }
            systems?.Advance(map, nowMilliseconds, options.TickMilliseconds);
            if (systems is not null)
            {
                foreach (var session in sessions.Values)
                {
                    if (session.State != PlayerSessionState.InWorld || session.Player is null) continue;
                    systems.Events.Pulse(session.Player, map.Definition, nowMilliseconds);
                }
            }

            return sessions.Values.Where(session => session.State == PlayerSessionState.InWorld)
                .ToDictionary(session => session.Connection, Project);
        }
    }

    private void ResolveProjectileImpacts(Projectile projectile, long nowMilliseconds)
    {
        if (systems is null || projectile.IsExpired || projectile.SourceId is not { } sourceId) return;
        if (!map.Entities.TryGet(sourceId, out var sourceEntity) || sourceEntity is not LivingEntity source) return;
        foreach (var entity in map.Entities.All.OfType<LivingEntity>())
        {
            if (!systems.Projectiles.TryRegisterImpact(projectile, entity, 16f)) continue;
            systems.Combat.ApplyDamage(source, entity, 8, Element.Neutral);
            if (!entity.IsAlive && source is Player player)
                systems.Events.NotifyEntityDefeated(player, entity, nowMilliseconds);
        }
    }

    private void ApplyAreaTick(AreaEffectEntity area, long nowMilliseconds)
    {
        if (systems is null) return;
        area.MarkTickScheduled();
        map.Entities.TryGet(area.SourceId, out var sourceEntity);
        var source = sourceEntity as LivingEntity;
        foreach (var living in map.Entities.All.OfType<LivingEntity>())
        {
            if (!living.IsAlive || living.Id == area.SourceId || !area.Contains(living.Position)) continue;
            if (area.Action.Kind == TechniqueActionKind.Damage)
            {
                var stats = source?.Stats ?? new StatBlock();
                var raw = CombatSystem.CalculateScaledAmount(area.Action.Amount, area.Action.Scaling, stats);
                systems.Combat.ApplyDamage(source, living, raw, area.Action.Element);
                if (!living.IsAlive && source is Player player)
                    systems.Events.NotifyEntityDefeated(player, living, nowMilliseconds);
            }
        }
    }

    private EntityStatePacket Project(PlayerSession session)
    {
        var player = session.Player!;
        var current = interest.Relevant(player, map.Entities).Select(entity => entity.ToState()).ToDictionary(entity => entity.Id);
        var upserts = current.Where(pair => !session.Baseline.TryGetValue(pair.Key, out var previous) || previous != pair.Value).Select(pair => pair.Value).ToArray();
        var despawns = session.Baseline.Keys.Where(id => !current.ContainsKey(id)).ToArray();
        var full = !session.HasSnapshot;
        session.Baseline.Clear();
        foreach (var pair in current) session.Baseline.Add(pair.Key, pair.Value);
        session.HasSnapshot = true;
        player.Interest.Clear();
        foreach (var id in current.Keys) player.Interest.Add(id);
        return new(tick, full, new(player.Id, player.Position, player.Inputs.LastProcessed), upserts, despawns);
    }

    public Player? Disconnect(ConnectionId connection)
    {
        lock (gate)
        {
            if (!sessions.Remove(connection, out var session)) return null;
            session.State = PlayerSessionState.Disconnected;
            if (session.Player is null) return null;
            map.Remove(session.Player.Id, out _);
            systems?.OnEntityRemoved(session.Player);
            return session.Player;
        }
    }

    public IReadOnlyList<Player> DirtyPlayers()
    {
        lock (gate) return sessions.Values.Select(value => value.Player).Where(value => value?.DirtyPosition == true).Cast<Player>().ToArray();
    }

    public IReadOnlyList<string> OnlineNames()
    {
        lock (gate) return sessions.Values.Where(session => session.Player is not null).Select(session => session.Player!.DisplayName).ToArray();
    }

    public bool IsValidPosition(Vector2Data position) => position.IsFinite && map.Definition.Bounds.Clamp(position) == position && !MovementSystem.IsBlocked(map.Definition, position);

    public MapProjection Projection(string contentVersion) => new(map.Definition.Id, map.Id, map.Definition.VisualKey,
        map.Definition.Bounds, options.MovementSpeed, options.TickMilliseconds, contentVersion);

    private static void EnsureInWorld(PlayerSession session)
    {
        if (session.State != PlayerSessionState.InWorld || session.Player is null)
            throw new InvalidOperationException("Jugador fuera del mundo.");
    }

    private sealed class TechniqueMapAccess(WorldRuntime world) : ITechniqueWorldAccess
    {
        public IEnumerable<LivingEntity> LivingOn(MapInstanceId mapId)
            => world.map.Id == mapId ? world.map.Entities.All.OfType<LivingEntity>() : [];

        public EntityId AllocateId() => world.spawns.NextId();

        public void Spawn(Entity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            if (entity.MapInstanceId != world.map.Id)
                throw new InvalidOperationException("La entidad pertenece a otra instancia de mapa.");
            world.map.Add(entity);
        }

        public Vector2Data Clamp(Vector2Data position) => world.map.Definition.Bounds.Clamp(position);
    }
}
