using NuevoMMO.Core;
using NuevoMMO.Server.Entities;
using NuevoMMO.Server.World;

namespace NuevoMMO.Server.Database;

public sealed record LoadedCharacter(
    CharacterSpawn Spawn,
    PlayerProgressionState Progression,
    int? CurrentHealth,
    int? CurrentMana);

public sealed class PersistenceService(ICharacterRepository characters, MapDefinition map)
{
    public async Task<LoadedCharacter> LoadCharacterAsync(AccountId account, CharacterRecord record, CancellationToken cancellationToken = default)
    {
        var stored = await characters.GetAsync(record.Id, cancellationToken) ?? record;
        var spawn = new CharacterSpawn(
            account,
            stored.Id,
            stored.Name,
            stored.MapDefinition.Value == Guid.Empty ? map.Id : stored.MapDefinition,
            stored.Position);
        return new LoadedCharacter(
            spawn,
            stored.ToProgressionState(),
            stored.CurrentHealth,
            stored.CurrentMana);
    }

    public Task SaveCharacterAsync(Player player, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(player);
        return characters.SaveCheckpointAsync(
            player.CharacterId,
            map.Id,
            player.Position,
            player.Progression,
            player.Health,
            player.Mana,
            cancellationToken);
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
