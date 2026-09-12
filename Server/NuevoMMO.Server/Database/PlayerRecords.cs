using NuevoMMO.Core;

namespace NuevoMMO.Server.Database;

public sealed class AccountRecord
{
    public AccountId Id { get; init; }
    public string Username { get; init; } = "";
    public string PasswordHash { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CharacterRecord
{
    public CharacterId Id { get; init; }
    public AccountId AccountId { get; init; }
    public string Name { get; init; } = "";
    public DefinitionId MapDefinition { get; set; }
    public Vector2Data Position { get; set; }
    public DefinitionId TraditionId { get; set; } = DefinitionId.Empty;
    public CharacterAppearance Appearance { get; set; } = CanonicalCharacterAppearance.Default;
    public int Level { get; set; } = 1;
    public long Experience { get; set; }
    public int AvailableAttributePoints { get; set; }
    public int Strength { get; set; } = ProgressionRules.BaseNaturalAttribute;
    public int Intelligence { get; set; } = ProgressionRules.BaseNaturalAttribute;
    public int Agility { get; set; } = ProgressionRules.BaseNaturalAttribute;
    public int Spirit { get; set; } = ProgressionRules.BaseNaturalAttribute;
    public int Vitality { get; set; } = ProgressionRules.BaseNaturalAttribute;
    public int? CurrentHealth { get; set; }
    public int? CurrentMana { get; set; }
    public string InventoryData { get; set; } = CharacterInventoryStorage.EmptyJson;
    public string QuestData { get; set; } = CharacterQuestStorage.EmptyJson;

    public PlayerProgressionState ToProgressionState()
    {
        var result = new PlayerProgressionState(
            Level,
            Experience,
            AvailableAttributePoints,
            new NaturalPrimaryStats(Strength, Intelligence, Agility, Spirit, Vitality));
        ProgressionRules.Validate(result);
        return result;
    }

    public void ApplyProgression(PlayerProgressionState state)
    {
        ProgressionRules.Validate(state);
        Level = state.Level;
        Experience = state.Experience;
        AvailableAttributePoints = state.AvailableAttributePoints;
        Strength = state.NaturalAttributes.Strength;
        Intelligence = state.NaturalAttributes.Intelligence;
        Agility = state.NaturalAttributes.Agility;
        Spirit = state.NaturalAttributes.Spirit;
        Vitality = state.NaturalAttributes.Vitality;
    }
}

public sealed class SessionRecord
{
    public SessionId Id { get; init; }
    public AccountId AccountId { get; init; }
    public string Token { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? RevokedAt { get; set; }
    public bool IsActive => RevokedAt is null;
}
