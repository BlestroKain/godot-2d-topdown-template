using NuevoMMO.Core;
using NuevoMMO.Server.Entities;
using NuevoMMO.Server.World;

namespace NuevoMMO.Server.Database;

public sealed class PersistenceService(ICharacterRepository characters, MapDefinition map)
{
    public async Task<CharacterSpawn> LoadCharacterAsync(AccountId account, CharacterRecord record, CancellationToken cancellationToken = default)
    {
        var stored = await characters.GetAsync(record.Id, cancellationToken) ?? record;
        return new(account, stored.Id, stored.Name, stored.MapDefinition.Value == Guid.Empty ? map.Id : stored.MapDefinition, stored.Position);
    }

    public Task SaveCharacterAsync(Player player, CancellationToken cancellationToken = default)
        => characters.SavePositionAsync(player.CharacterId, map.Id, player.Position, cancellationToken);

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
