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
        var health = definition.Harvest.HealthRange is { } range ? Math.Max(1f, range.Maximum) : 1f;
        return new(NextId(), definition, instance, position, health);
    }
}

public sealed class MapInstance(MapInstanceId id, MapDefinition definition, float interestRadius)
{
    public MapInstanceId Id { get; } = id.Value > 0 ? id : throw new ArgumentException("Instancia inválida.");
    public MapDefinition Definition { get; } = definition ?? throw new ArgumentNullException(nameof(definition));
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

    public void Add(MapInstance map)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (!maps.TryAdd(map.Id, map)) throw new InvalidOperationException("Instancia duplicada.");
    }

    public MapInstance Get(MapInstanceId id)
        => maps.TryGetValue(id, out var map) ? map : throw new KeyNotFoundException("Instancia inexistente.");

    public bool TryGet(MapInstanceId id, out MapInstance? map) => maps.TryGetValue(id, out map);

    public MapInstance GetShared(DefinitionId definitionId)
        => TryGetShared(definitionId, out var map) && map is not null
            ? map
            : throw new KeyNotFoundException($"No existe una instancia para MapDefinition {definitionId}.");

    public bool TryGetShared(DefinitionId definitionId, out MapInstance? map)
    {
        map = maps.Values.FirstOrDefault(candidate => candidate.Definition.Id == definitionId);
        return map is not null;
    }
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
    public MapInstanceId? PendingMapInstance { get; set; }
    public bool MapLoadDispatched { get; set; }
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
    private readonly WorldManager worlds = new();
    private readonly MovementSystem movement;
    private readonly MobMovementSystem mobMovement;
    private readonly ProjectileSystem projectiles;
    private readonly GameSystems? systems;
    private readonly SpawnManager spawns = new();
    private readonly Dictionary<ConnectionId, PlayerSession> sessions = [];
    private long nextMapInstance;
    private long tick;

    public int TickMilliseconds => options.TickMilliseconds;
    public int PlayerCount { get { lock (gate) return sessions.Values.Count(value => value.State == PlayerSessionState.InWorld); } }
    public int EntityCount { get { lock (gate) return worlds.All.Sum(value => value.Entities.Count); } }
    public long Tick { get { lock (gate) return tick; } }
    public MapDefinition Map => map.Definition;
    public MapInstanceId Instance => map.Id;
    public MapInstance MapInstance => map;
    public IEnumerable<MapInstance> MapInstances => worlds.All;
    public GameSystems? Systems => systems;

    public WorldRuntime(MapDefinition definition, WorldOptions options, IMobMovementPolicy mobPolicy, GameSystems? systems = null)
    {
        if (options.MaxPlayers is < 1 or > 4096 || options.InterestRadius <= 0 || !float.IsFinite(options.InterestRadius))
            throw new ArgumentException("Opciones de mundo inválidas.");
        this.options = options;
        this.systems = systems;
        map = new(options.Instance, definition, options.InterestRadius);
        worlds.Add(map);
        nextMapInstance = options.Instance.Value;
        movement = new(options.MovementSpeed, options.TickMilliseconds);
        mobMovement = new(options.MobSpeed, options.TickMilliseconds, mobPolicy);
        projectiles = systems?.Projectiles ?? new ProjectileSystem();
        systems?.Techniques.BindWorld(new TechniqueMapAccess(this));
    }

    public WorldRuntime(MapDefinition definition, MobDefinition mobDefinition, WorldOptions options, IMobMovementPolicy mobPolicy,
        Vector2Data mobSpawn, GameSystems? systems = null)
        : this(definition, options, mobPolicy, systems)
    {
        map.Add(spawns.Mob(mobDefinition, map.Id, mobSpawn));
    }

    public MapInstance AddMap(MapDefinition definition, MapInstanceId? instance = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        lock (gate)
        {
            if (worlds.TryGetShared(definition.Id, out var existing) && instance is null) return existing!;
            var id = instance ?? AllocateMapInstanceId();
            var created = new MapInstance(id, definition, options.InterestRadius);
            worlds.Add(created);
            nextMapInstance = Math.Max(nextMapInstance, id.Value);
            return created;
        }
    }

    public bool TryGetMap(MapInstanceId instance, out MapInstance? value)
    {
        lock (gate) return worlds.TryGet(instance, out value);
    }

    public MapInstance GetMap(MapInstanceId instance)
    {
        lock (gate) return worlds.Get(instance);
    }

    public DefinitionId MapDefinitionFor(MapInstanceId instance)
    {
        lock (gate) return worlds.Get(instance).Definition.Id;
    }

    public Mob SpawnMob(MobDefinition definition, Vector2Data position) => SpawnMob(map.Id, definition, position);
    public Npc SpawnNpc(NpcDefinition definition, Vector2Data position) => SpawnNpc(map.Id, definition, position);
    public ResourceEntity SpawnResource(ResourceDefinition definition, Vector2Data position) => SpawnResource(map.Id, definition, position);

    public Mob SpawnMob(MapInstanceId instance, MobDefinition definition, Vector2Data position)
    {
        ArgumentNullException.ThrowIfNull(definition);
        lock (gate)
        {
            var targetMap = worlds.Get(instance);
            var mob = spawns.Mob(definition, targetMap.Id, targetMap.Definition.Bounds.Clamp(position));
            targetMap.Add(mob);
            return mob;
        }
    }

    public Npc SpawnNpc(MapInstanceId instance, NpcDefinition definition, Vector2Data position)
    {
        ArgumentNullException.ThrowIfNull(definition);
        lock (gate)
        {
            var targetMap = worlds.Get(instance);
            var npc = spawns.Npc(definition, targetMap.Id, targetMap.Definition.Bounds.Clamp(position));
            targetMap.Add(npc);
            return npc;
        }
    }

    public ResourceEntity SpawnResource(MapInstanceId instance, ResourceDefinition definition, Vector2Data position)
    {
        ArgumentNullException.ThrowIfNull(definition);
        lock (gate)
        {
            var targetMap = worlds.Get(instance);
            var resource = spawns.Resource(definition, targetMap.Id, targetMap.Definition.Bounds.Clamp(position));
            targetMap.Add(resource);
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
            if (session.State != PlayerSessionState.Authenticated) throw new InvalidOperationException("Selección de personaje inválida.");
            var targetMap = worlds.TryGetShared(character.MapDefinition, out var persisted) && persisted is not null ? persisted : map;
            var position = IsValidPosition(targetMap, character.Position) ? character.Position : targetMap.Definition.Spawn;
            var player = spawns.Player(character with { MapDefinition = targetMap.Definition.Id, Position = position }, targetMap.Id);
            player.Techniques.Learn(CanonicalCombatContent.BasicAttackId);
            session.Character = character.Character;
            session.Player = player;
            BeginMapLoad(session, targetMap.Id);
            return player;
        }
    }

    public MapLoadPacket PrepareMapLoad(PlayerSession session, string contentVersion)
    {
        lock (gate)
        {
            if (session.State != PlayerSessionState.WaitingForMap || session.Player is null || session.PendingMapInstance is not { } instance)
                throw new InvalidOperationException("La sesión no tiene una carga de mapa pendiente.");
            session.MapLoadDispatched = true;
            return new MapLoadPacket(Projection(instance, contentVersion), session.Player.Id, session.Character);
        }
    }

    public IReadOnlyDictionary<ConnectionId, MapLoadPacket> PendingMapLoads(string contentVersion)
    {
        lock (gate)
        {
            var result = new Dictionary<ConnectionId, MapLoadPacket>();
            foreach (var session in sessions.Values)
            {
                if (session.State != PlayerSessionState.WaitingForMap || session.MapLoadDispatched ||
                    session.Player is null || session.PendingMapInstance is not { } instance) continue;
                session.MapLoadDispatched = true;
                result[session.Connection] = new MapLoadPacket(ProjectionCore(instance, contentVersion), session.Player.Id, session.Character);
            }
            return result;
        }
    }

    public void Activate(PlayerSession session, MapInstanceId instance)
    {
        lock (gate)
        {
            if (session.State != PlayerSessionState.WaitingForMap || session.Player is null ||
                session.PendingMapInstance != instance || session.Player.MapInstanceId != instance || !worlds.TryGet(instance, out var targetMap) || targetMap is null)
                throw new InvalidOperationException("Entrada al mapa inválida.");
            targetMap.Add(session.Player);
            session.PendingMapInstance = null;
            session.MapLoadDispatched = false;
            session.State = PlayerSessionState.InWorld;
        }
    }

    public bool TryTransition(PlayerSession session, DefinitionId destinationMapId, Vector2Data destination, Direction facing = Direction.Down)
    {
        lock (gate)
        {
            EnsureInWorld(session);
            if (!worlds.TryGetShared(destinationMapId, out var destinationMap) || destinationMap is null) return false;
            var resolved = destinationMap.Definition.Bounds.Clamp(destination);
            if (!IsValidPosition(destinationMap, resolved)) return false;
            return TransitionCore(session, destinationMap, resolved, facing);
        }
    }

    public void SubmitMovement(PlayerSession session, InputFrame input)
    {
        lock (gate)
        {
            EnsureInWorld(session);
            session.Player!.Inputs.Enqueue(input);
        }
    }

    public void SetTarget(PlayerSession session, EntityId target)
    {
        lock (gate)
        {
            EnsureInWorld(session);
            var currentMap = CurrentMap(session.Player!);
            if (target.Value <= 0) { session.Player!.TargetId = null; return; }
            if (!currentMap.Entities.TryGet(target, out var entity) || entity is null)
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
            var currentMap = CurrentMap(player);
            var now = checked(tick * options.TickMilliseconds);
            Entity? target = null;
            if (targetId.Value > 0)
            {
                if (!currentMap.Entities.TryGet(targetId, out target) || target is null)
                    return InteractionOutcome.Fail("El objetivo ya no existe.");
            }
            else target = NearestInteractable(player, currentMap);

            if (target is not null && !systems.Interactions.CanReach(player, target))
                return InteractionOutcome.Fail("Fuera de alcance.");
            if (target is WorldItem worldItem)
            {
                var picked = systems.Interactions.TryPickup(player, worldItem, now);
                if (picked.Success) currentMap.Remove(worldItem.Id, out _);
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
                var evt = systems.Events.MapEvents(currentMap.Definition.Id)
                    .FirstOrDefault(definition => definition.Placement is { } placement && DistanceSquared(placement.Position, target.Position) <= 16f);
                if (evt is not null) return ToOutcome(systems.Events.TryTrigger(player, evt, EventTrigger.Action, now));
                return InteractionOutcome.Ok($"{target.DisplayName} no tiene diálogo.");
            }
            foreach (var evt in systems.Events.MapEvents(currentMap.Definition.Id))
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
            var currentMap = CurrentMap(player);
            if (!currentMap.Entities.TryGet(targetId, out var entity) || entity is not LivingEntity living)
                return TechniqueUseResult.Fail(TechniqueUseFailure.InvalidTarget, "Objetivo inválido.", CanonicalCombatContent.BasicAttackId);
            player.TargetId = targetId;
            if (!player.Techniques.Knows(CanonicalCombatContent.BasicAttackId)) player.Techniques.Learn(CanonicalCombatContent.BasicAttackId);
            var now = checked(tick * options.TickMilliseconds);
            var result = systems.Techniques.BeginUse(player, CanonicalCombatContent.BasicAttackId, living, null, now);
            if (result.Success && !living.IsAlive) systems.Events.NotifyEntityDefeated(player, living, now);
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
            var currentMap = CurrentMap(player);
            LivingEntity? living = null;
            if (targetId.Value > 0)
            {
                if (!currentMap.Entities.TryGet(targetId, out var entity) || entity is not LivingEntity target)
                    return TechniqueUseResult.Fail(TechniqueUseFailure.InvalidTarget, "Objetivo inválido.", techniqueId);
                living = target;
                player.TargetId = targetId;
            }
            var now = checked(tick * options.TickMilliseconds);
            var result = systems.Techniques.BeginUse(player, techniqueId, living, point, now);
            if (result.Success && living is { IsAlive: false }) systems.Events.NotifyEntityDefeated(player, living, now);
            return result;
        }
    }

    public void AddEntity(Entity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        lock (gate) worlds.Get(entity.MapInstanceId).Add(entity);
    }

    public InventorySnapshotPacket InventorySnapshot(PlayerSession session)
    {
        lock (gate)
        {
            if (session.Player is null) throw new InvalidOperationException("Jugador fuera del mundo.");
            return InventoryProjection.Create(session.Player);
        }
    }

    public string EquipItem(PlayerSession session, ItemInstanceId itemId)
    {
        if (systems is null) throw new InvalidOperationException("GameSystems no está configurado.");
        lock (gate)
        {
            EnsureInWorld(session);
            var player = session.Player!;
            if (!player.Inventory.TryGet(itemId, out var item) || item is null)
                throw new InvalidOperationException("El item no está en el inventario.");
            var definition = systems.Definitions.Get<ItemDefinition>(item.DefinitionId);
            if (definition.Equipment is null)
                throw new InvalidOperationException("El item no es equipable.");
            var index = NextEquipmentIndex(player, definition.Equipment.Slot);
            systems.Equipment.Equip(player, itemId, new EquipmentPosition(definition.Equipment.Slot, index));
            systems.Progression.Recalculate(player, preserveVitals: true);
            return $"Equipado: {definition.Name}.";
        }
    }

    public string UnequipItem(PlayerSession session, ItemInstanceId itemId)
    {
        if (systems is null) throw new InvalidOperationException("GameSystems no está configurado.");
        lock (gate)
        {
            EnsureInWorld(session);
            var player = session.Player!;
            if (!systems.Equipment.Unequip(player, itemId, out _))
                throw new InvalidOperationException("Ese item no está equipado.");
            systems.Progression.Recalculate(player, preserveVitals: true);
            return "Item desequipado.";
        }
    }

    public void MoveInventoryItem(PlayerSession session, ItemInstanceId itemId, int targetIndex)
    {
        lock (gate)
        {
            EnsureInWorld(session);
            session.Player!.Inventory.Move(itemId, targetIndex);
        }
    }

    private static int NextEquipmentIndex(Player player, EquipmentSlot slot)
    {
        if (slot is not (EquipmentSlot.Ring or EquipmentSlot.Trophy)) return 0;
        var used = player.Equipment.PositionsFor(slot).Select(static position => position.Index).ToHashSet();
        for (var index = 0; index < 8; index++)
            if (!used.Contains(index)) return index;
        return 0;
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
            var currentMap = CurrentMap(session.Player!);
            if (!currentMap.Entities.TryGet(worldItemId, out var entity) || entity is not WorldItem worldItem)
                return InteractionResult.Fail(InteractionFailure.TargetUnavailable, "El item ya no existe en el mundo.");
            var result = interactions.TryPickup(session.Player!, worldItem, nowMilliseconds);
            if (result.Success) currentMap.Remove(worldItem.Id, out _);
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
            var currentMap = CurrentMap(session.Player!);
            if (!currentMap.Entities.TryGet(resourceId, out var entity) || entity is not ResourceEntity resource)
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
            foreach (var currentMap in worlds.All.ToArray()) StepMap(currentMap, nowMilliseconds);

            if (systems is not null)
            {
                foreach (var currentMap in worlds.All.ToArray())
                {
                    systems.Advance(currentMap, nowMilliseconds, options.TickMilliseconds);
                    foreach (var entity in currentMap.Entities.All) currentMap.Refresh(entity);
                }
                foreach (var session in sessions.Values)
                {
                    if (session.State != PlayerSessionState.InWorld || session.Player is null) continue;
                    var currentMap = CurrentMap(session.Player);
                    systems.Events.Pulse(session.Player, currentMap.Definition, nowMilliseconds);
                }
            }

            return sessions.Values.Where(session => session.State == PlayerSessionState.InWorld)
                .ToDictionary(session => session.Connection, Project);
        }
    }

    private void StepMap(MapInstance currentMap, long nowMilliseconds)
    {
        var despawn = new List<EntityId>();
        foreach (var entity in currentMap.Entities.All.ToArray())
        {
            var transitioned = false;
            switch (entity)
            {
                case Player player:
                    var session = sessions.Values.FirstOrDefault(value => value.Player?.Id == player.Id);
                    if (session?.State != PlayerSessionState.InWorld) break;
                    movement.Step(player, currentMap.Definition);
                    transitioned = TryPortalTransitionCore(session, currentMap);
                    break;
                case Mob mob:
                    mobMovement.Step(mob, currentMap.Definition, tick);
                    break;
                case Projectile projectile:
                    projectiles.Step(projectile, currentMap.Definition, options.TickMilliseconds);
                    ResolveProjectileImpacts(projectile, currentMap, nowMilliseconds);
                    if (projectile.IsExpired) despawn.Add(projectile.Id);
                    break;
                case AreaEffectEntity area:
                    var ticked = area.Advance(options.TickMilliseconds);
                    if (ticked) ApplyAreaTick(area, currentMap, nowMilliseconds);
                    if (area.IsExpired) despawn.Add(area.Id);
                    break;
                case ResourceEntity resource:
                    resource.TryRespawn(nowMilliseconds);
                    break;
                case WorldItem worldItem when worldItem.PickedUp || worldItem.IsExpired(nowMilliseconds):
                    despawn.Add(worldItem.Id);
                    break;
            }
            if (!transitioned && !despawn.Contains(entity.Id) && entity.MapInstanceId == currentMap.Id)
                currentMap.Refresh(entity);
        }

        foreach (var id in despawn.Distinct())
            if (currentMap.Remove(id, out var removed) && removed is not null) systems?.OnEntityRemoved(removed);
    }

    private bool TryPortalTransitionCore(PlayerSession session, MapInstance currentMap)
    {
        if (systems is null || session.Player is null) return false;
        var player = session.Player;
        foreach (var portal in currentMap.Definition.Content.Portals)
        {
            if (!ShapeContains(portal.TriggerArea, player.Position)) continue;
            var context = new ConditionEvaluationContext(
                currentMap.Definition.Id,
                Variables: player.Events.Variables,
                Switches: player.Events.Switches);
            if (!systems.Conditions.Evaluate(player, portal.Requirements, context)) continue;
            if (!worlds.TryGetShared(portal.DestinationMapId, out var destinationMap) || destinationMap is null) return false;
            var destination = destinationMap.Definition.Bounds.Clamp(portal.Destination);
            if (!IsValidPosition(destinationMap, destination)) return false;
            return TransitionCore(session, destinationMap, destination, portal.DestinationDirection);
        }
        return false;
    }

    private bool TransitionCore(PlayerSession session, MapInstance destinationMap, Vector2Data destination, Direction facing)
    {
        var player = session.Player!;
        var sourceMap = CurrentMap(player);
        systems?.Techniques.Cancel(player.Id);
        sourceMap.Remove(player.Id, out _);
        player.TransferTo(destinationMap.Id, destination, facing);
        BeginMapLoad(session, destinationMap.Id);
        return true;
    }

    private void BeginMapLoad(PlayerSession session, MapInstanceId instance)
    {
        session.Baseline.Clear();
        session.HasSnapshot = false;
        session.PendingMapInstance = instance;
        session.MapLoadDispatched = false;
        session.State = PlayerSessionState.WaitingForMap;
    }

    private void ResolveProjectileImpacts(Projectile projectile, MapInstance currentMap, long nowMilliseconds)
    {
        if (systems is null || projectile.IsExpired || projectile.SourceId is not { } sourceId) return;
        if (!currentMap.Entities.TryGet(sourceId, out var sourceEntity) || sourceEntity is not LivingEntity source) return;
        foreach (var entity in currentMap.Entities.All.OfType<LivingEntity>())
        {
            if (!entity.IsAlive || !systems.Projectiles.TryRegisterImpact(projectile, entity, 16f)) continue;
            if (projectile.ImpactActions.Length > 0)
                systems.Techniques.ExecuteEffectActions(source, entity, projectile.ImpactActions, nowMilliseconds);
            if (!entity.IsAlive && source is Player player) systems.Events.NotifyEntityDefeated(player, entity, nowMilliseconds);
            if (projectile.IsExpired) break;
        }
    }

    private void ApplyAreaTick(AreaEffectEntity area, MapInstance currentMap, long nowMilliseconds)
    {
        if (systems is null) return;
        area.MarkTickScheduled();
        currentMap.Entities.TryGet(area.SourceId, out var sourceEntity);
        var source = sourceEntity as LivingEntity;
        foreach (var living in currentMap.Entities.All.OfType<LivingEntity>())
        {
            if (!living.IsAlive || living.Id == area.SourceId || !area.Contains(living.Position)) continue;
            if (area.Action.PayloadActions.Length > 0)
                systems.Techniques.ExecuteEffectActions(source, living, area.Action.PayloadActions, nowMilliseconds);
            if (!living.IsAlive && source is Player player) systems.Events.NotifyEntityDefeated(player, living, nowMilliseconds);
        }
    }

    private EntityStatePacket Project(PlayerSession session)
    {
        var player = session.Player!;
        var currentMap = CurrentMap(player);
        var interest = new InterestManager(currentMap.Spatial, options.InterestRadius);
        var current = interest.Relevant(player, currentMap.Entities).Select(entity => entity.ToState()).ToDictionary(entity => entity.Id);
        var upserts = current.Where(pair => !session.Baseline.TryGetValue(pair.Key, out var previous) || previous != pair.Value)
            .Select(pair => pair.Value).ToArray();
        var despawns = session.Baseline.Keys.Where(id => !current.ContainsKey(id)).ToArray();
        var full = !session.HasSnapshot;
        session.Baseline.Clear();
        foreach (var pair in current) session.Baseline.Add(pair.Key, pair.Value);
        session.HasSnapshot = true;
        player.Interest.Clear();
        foreach (var id in current.Keys) player.Interest.Add(id);
        return new(tick, full, new(player.Id, player.Position, player.Inputs.LastProcessed), upserts, despawns);
    }

    private Entity? NearestInteractable(Player player, MapInstance currentMap)
    {
        Entity? best = null;
        var bestDistance = 64f * 64f;
        foreach (var entity in currentMap.Entities.All)
        {
            if (entity.Id == player.Id || entity is not (Npc or ResourceEntity or WorldItem)) continue;
            var distance = DistanceSquared(player.Position, entity.Position);
            if (distance > bestDistance) continue;
            best = entity;
            bestDistance = distance;
        }
        return best;
    }

    public Player? Disconnect(ConnectionId connection)
    {
        lock (gate)
        {
            if (!sessions.Remove(connection, out var session)) return null;
            session.State = PlayerSessionState.Disconnected;
            if (session.Player is null) return null;
            if (worlds.TryGet(session.Player.MapInstanceId, out var currentMap) && currentMap is not null)
                currentMap.Remove(session.Player.Id, out _);
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

    public bool IsValidPosition(Vector2Data position) => IsValidPosition(map, position);

    public bool IsValidPosition(MapInstanceId instance, Vector2Data position)
    {
        lock (gate) return IsValidPosition(worlds.Get(instance), position);
    }

    private static bool IsValidPosition(MapInstance targetMap, Vector2Data position)
        => position.IsFinite && targetMap.Definition.Bounds.Clamp(position) == position && !MovementSystem.IsBlocked(targetMap.Definition, position);

    public MapProjection Projection(string contentVersion) => Projection(map.Id, contentVersion);

    public MapProjection Projection(MapInstanceId instance, string contentVersion)
    {
        lock (gate) return ProjectionCore(instance, contentVersion);
    }

    private MapProjection ProjectionCore(MapInstanceId instance, string contentVersion)
    {
        var targetMap = worlds.Get(instance);
        return new(targetMap.Definition.Id, targetMap.Id, targetMap.Definition.VisualKey,
            targetMap.Definition.Bounds, options.MovementSpeed, options.TickMilliseconds, contentVersion);
    }

    private MapInstance CurrentMap(Player player) => worlds.Get(player.MapInstanceId);

    private MapInstanceId AllocateMapInstanceId()
    {
        do { nextMapInstance = checked(nextMapInstance + 1); }
        while (worlds.TryGet(new MapInstanceId(nextMapInstance), out _));
        return new MapInstanceId(nextMapInstance);
    }

    private static InteractionOutcome ToOutcome(EventExecutionResult result)
        => result.Success ? InteractionOutcome.Ok(string.Join('\n', result.Log)) : InteractionOutcome.Fail(result.Message);

    private static float DistanceSquared(Vector2Data left, Vector2Data right)
    {
        var x = left.X - right.X;
        var y = left.Y - right.Y;
        return x * x + y * y;
    }

    private static bool ShapeContains(MapShapeDefinition shape, Vector2Data point)
    {
        return shape.Kind switch
        {
            MapShapeKind.Rectangle => MathF.Abs(point.X - shape.Center.X) <= shape.Size.X * .5f &&
                                      MathF.Abs(point.Y - shape.Center.Y) <= shape.Size.Y * .5f,
            MapShapeKind.Circle => DistanceSquared(point, shape.Center) <= shape.Radius * shape.Radius,
            MapShapeKind.Polygon => PolygonContains(shape.Points, point),
            _ => false
        };
    }

    private static bool PolygonContains(IReadOnlyList<Vector2Data> points, Vector2Data point)
    {
        if (points.Count < 3) return false;
        var inside = false;
        for (var i = 0; i < points.Count; i++)
        {
            var j = i == 0 ? points.Count - 1 : i - 1;
            var a = points[i];
            var b = points[j];
            var crosses = (a.Y > point.Y) != (b.Y > point.Y) &&
                          point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X;
            if (crosses) inside = !inside;
        }
        return inside;
    }

    private static void EnsureInWorld(PlayerSession session)
    {
        if (session.State != PlayerSessionState.InWorld || session.Player is null)
            throw new InvalidOperationException("Jugador fuera del mundo.");
    }

    private sealed class TechniqueMapAccess(WorldRuntime world) : ITechniqueWorldAccess
    {
        public IEnumerable<LivingEntity> LivingOn(MapInstanceId mapId)
            => world.worlds.TryGet(mapId, out var targetMap) && targetMap is not null
                ? targetMap.Entities.All.OfType<LivingEntity>()
                : [];

        public EntityId AllocateId() => world.spawns.NextId();

        public void Spawn(Entity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            world.worlds.Get(entity.MapInstanceId).Add(entity);
        }

        public Vector2Data Clamp(MapInstanceId mapId, Vector2Data position)
            => world.worlds.Get(mapId).Definition.Bounds.Clamp(position);
    }
}
