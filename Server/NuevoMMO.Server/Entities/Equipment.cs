using NuevoMMO.Core;

namespace NuevoMMO.Server.Entities;

public sealed class Equipment
{
    private readonly Dictionary<string, ItemInstanceId> slots = [];
    public IReadOnlyDictionary<string, ItemInstanceId> Slots => slots;
}
