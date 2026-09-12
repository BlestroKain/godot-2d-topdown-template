using NuevoMMO.Core;
using NuevoMMO.Server.Entities;
using NuevoMMO.Server.World;

namespace NuevoMMO.Server.Database;

public sealed record LoadedCharacter(
    CharacterSpawn Spawn,
    PlayerProgressionState Progression,
    int? CurrentHealth,
    int? CurrentMana);

public sealed class PersistenceService
{
    private readonly ICharacterRepository characters;
    private readonly MapDefinition fallbackMap;
    private readonly Func<MapInstanceId, DefinitionId>? mapDefinitionResolver;

    public PersistenceService(
        ICharacterRepository characters,
        MapDefinition fallbackMap,
        Func<MapInstanceId, DefinitionId>? mapDefinitionResolver = null)
    {
        this.characters = characters ?? throw new ArgumentNullException(nameof(characters));
        this.fallbackMap = fallbackMap ?? throw new ArgumentNullException(nameof(fallbackMap));
        this.mapDefinitionResolver = mapDefinitionResolver;
    }

    public async Task<LoadedCharacter> LoadCharacterAsync(AccountId account, CharacterRecord record, CancellationToken cancellationToken = default)
    {
        var stored = await characters.GetAsync(record.Id, cancellationToken) ?? record;
        var spawn = new CharacterSpawn(
            account,
            stored.Id,
            stored.Name,
            stored.MapDefinition.Value == Guid.Empty ? fallbackMap.Id : stored.MapDefinition,
            stored.Position);
        return new LoadedCharacter(
            spawn,
            stored.ToProgressionState(),
            stored.CurrentHealth,
            stored.CurrentMana);
    }

    public async Task SaveCharacterAsync(Player player, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(player);
        var mapDefinition = mapDefinitionResolver?.Invoke(player.MapInstanceId) ?? fallbackMap.Id;
        if (mapDefinition.IsEmpty)
            throw new InvalidOperationException("No se pudo resolver la MapDefinition actual del jugador.");
        await characters.SaveCheckpointAsync(
            player.CharacterId,
            mapDefinition,
            player.Position,
            player.Progression,
            player.Health,
            player.Mana,
            cancellationToken);
        await characters.SaveInventoryAsync(
            player.CharacterId,
            CharacterInventoryStorage.FromPlayer(player).ToJson(),
            cancellationToken);
        await characters.SaveQuestDataAsync(
            player.CharacterId,
            CharacterQuestStorage.FromPlayer(player).ToJson(),
            cancellationToken);
    }

    public void RestoreInventory(Player player, CharacterRecord record)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(record);
        CharacterInventoryStorage.Parse(record.InventoryData).ApplyTo(player);
    }

    public void RestoreQuests(Player player, CharacterRecord record)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(record);
        CharacterQuestStorage.Parse(record.QuestData).ApplyTo(player);
    }

    public async Task SaveDirtyAsync(IEnumerable<Player> players, CancellationToken cancellationToken = default)
    {
        foreach (var player in players)
        {
            await SaveCharacterAsync(player, cancellationToken);
            player.MarkSaved();
        }
    }
}

public sealed class TransactionManager
{
    public async Task ExecuteAsync(Func<CancellationToken, Task> work, CancellationToken cancellationToken = default)
        => await work(cancellationToken);
}

public sealed class GameDataLoader
{
    public DefinitionRegistry Load(ContentPackage package)
    {
        var registry = new DefinitionRegistry();
        package.LoadInto(registry);
        return registry;
    }
}
