# Core

Contratos y datos compartidos. Sin Godot, SQL ni simulación.

- Ids: DefinitionId, EntityId, AccountId, CharacterId, MapId, MapInstanceId, ItemInstanceId, SessionId, GuildId, PartyId
- Definitions: plantillas. Referencian otras definitions por DefinitionId.
- States: proyección serializable.
- Stats: STR/INT/AGI/SPI/VIT, Luck, resistencias elementales.
- Items: ItemDefinition vs ItemInstance.
- Math: Vector2Data, Vector2IntData, BoundsData, Direction.
