# Server

Autoridad, simulación, persistencia y mundo runtime.

- Entity → LivingEntity → Player / Mob / Npc. Resource, Projectile, WorldItem, InteractiveObject.
- WorldRuntime coordina MapInstance, EntityRegistry, InterestManager y MovementSystem.
- Handlers delegan en AuthService / CharacterService / MovementSystem. Nunca SQL directo.
- Persistencia inicial: Account, Character y CharacterPosition en memoria; schema PostgreSQL preparado.
