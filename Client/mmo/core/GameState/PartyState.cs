using NuevoMMO.Core;

namespace NuevoMMO.Client;

public sealed record PartyMemberState(EntityId EntityId, string Name, int Level, bool Online);

/// <summary>
/// Estado local de party. Sin protocolo propio todavía: el cliente conserva
/// la membresía que reciba, sin inventar invite/kick/loot-rules.
/// </summary>
public sealed class PartyState
{
    private readonly List<PartyMemberState> members = [];

    public PartyId? Id { get; private set; }
    public EntityId Leader { get; private set; }
    public IReadOnlyList<PartyMemberState> Members => members;
    public bool HasParty => Id is not null && members.Count > 0;

    public void Set(PartyId id, EntityId leader, IEnumerable<PartyMemberState> partyMembers)
    {
        if (id.Value == Guid.Empty) throw new ArgumentException("PartyId vacío.", nameof(id));
        if (leader.Value <= 0) throw new ArgumentException("Leader inválido.", nameof(leader));
        ArgumentNullException.ThrowIfNull(partyMembers);

        var copy = partyMembers.ToArray();
        if (copy.Length == 0) throw new ArgumentException("Una party requiere al menos un miembro.", nameof(partyMembers));
        if (copy.Any(static member => member.EntityId.Value <= 0 || string.IsNullOrWhiteSpace(member.Name) || member.Level < 1))
            throw new ArgumentException("Miembro de party inválido.", nameof(partyMembers));
        if (copy.All(member => member.EntityId != leader))
            throw new ArgumentException("El líder debe pertenecer a la party.", nameof(leader));

        Id = id;
        Leader = leader;
        members.Clear();
        members.AddRange(copy);
    }

    public void Clear()
    {
        Id = null;
        Leader = default;
        members.Clear();
    }
}
