using NuevoMMO.Core;
using NuevoMMO.Network;

namespace NuevoMMO.Client;

public sealed class ClientMap : IClientMap
{
    private readonly List<ActionMessage> actionMessages = [];

    public ClientMap(MapProjection projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        Projection = projection;
    }

    public MapProjection Projection { get; }
    public DefinitionId Definition => Projection.Definition;
    public MapInstanceId Instance => Projection.Instance;
    public BoundsData Bounds => Projection.Bounds;
    public IReadOnlyList<IActionMessage> ActionMessages => actionMessages;

    public void AddAction(Vector2Data position, string text)
        => actionMessages.Add(new ActionMessage(position, text));

    public void ClearActions() => actionMessages.Clear();
}
