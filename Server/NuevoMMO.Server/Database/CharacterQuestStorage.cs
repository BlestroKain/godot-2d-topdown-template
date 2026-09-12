using System.Text.Json;
using NuevoMMO.Core;
using NuevoMMO.Server.Entities;

namespace NuevoMMO.Server.Database;

public sealed class CharacterQuestStorage
{
    public const string EmptyJson = """{"quests":[]}""";

    public StoredQuestProgress[] Quests { get; set; } = [];

    public static CharacterQuestStorage FromPlayer(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        return new CharacterQuestStorage
        {
            Quests = player.Quests.Entries.Select(entry => new StoredQuestProgress(
                entry.QuestId.Value,
                (byte)entry.State,
                entry.CompletionCount,
                entry.TaskProgress.Select(pair => new StoredQuestTaskProgress(pair.Key, pair.Value)).ToArray())).ToArray()
        };
    }

    public void ApplyTo(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        player.Quests.Clear();
        foreach (var quest in Quests)
        {
            if (quest.QuestId == Guid.Empty || !Enum.IsDefined((QuestRuntimeState)quest.State) || quest.CompletionCount < 0)
                continue;
            var tasks = (quest.Tasks ?? [])
                .Where(static task => task.TaskId != Guid.Empty && task.Progress >= 0)
                .Select(static task => new KeyValuePair<Guid, int>(task.TaskId, task.Progress));
            player.Quests.Restore(new DefinitionId(quest.QuestId), (QuestRuntimeState)quest.State, quest.CompletionCount, tasks);
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this);

    public static CharacterQuestStorage Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new CharacterQuestStorage();
        try
        {
            return JsonSerializer.Deserialize<CharacterQuestStorage>(json) ?? new CharacterQuestStorage();
        }
        catch (JsonException)
        {
            return new CharacterQuestStorage();
        }
    }
}

public sealed record StoredQuestProgress(Guid QuestId, byte State, int CompletionCount, StoredQuestTaskProgress[] Tasks);
public sealed record StoredQuestTaskProgress(Guid TaskId, int Progress);
