using NuevoMMO.Core;

namespace NuevoMMO.Client;

public sealed class ChatBubbleState
{
    public ChatBubbleState(EntityId owner, string text, long expiresAtMilliseconds)
    {
        if (owner.Value <= 0) throw new ArgumentException("Owner inválido.", nameof(owner));
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Texto vacío.", nameof(text));
        Owner = owner;
        Text = text.Trim();
        ExpiresAtMilliseconds = expiresAtMilliseconds;
    }

    public EntityId Owner { get; }
    public string Text { get; }
    public long ExpiresAtMilliseconds { get; }
}

public sealed class DashState
{
    public DashState(Vector2Data from, Vector2Data to, long durationMilliseconds)
    {
        if (!from.IsFinite || !to.IsFinite) throw new ArgumentException("Dash con coordenadas no finitas.");
        if (durationMilliseconds <= 0) throw new ArgumentOutOfRangeException(nameof(durationMilliseconds));
        From = from;
        To = to;
        DurationMilliseconds = durationMilliseconds;
    }

    public Vector2Data From { get; }
    public Vector2Data To { get; }
    public long DurationMilliseconds { get; }
}

public sealed class FriendInstance
{
    public FriendInstance(EntityId entityId, string name, bool online)
    {
        if (entityId.Value <= 0) throw new ArgumentException("EntityId inválido.", nameof(entityId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Nombre vacío.", nameof(name));
        EntityId = entityId;
        Name = name.Trim();
        Online = online;
    }

    public EntityId EntityId { get; }
    public string Name { get; }
    public bool Online { get; }
}

public sealed class ActionMessage : IActionMessage
{
    public ActionMessage(Vector2Data position, string text)
    {
        if (!position.IsFinite) throw new ArgumentException("Position no finita.", nameof(position));
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Texto vacío.", nameof(text));
        Position = position;
        Text = text.Trim();
    }

    public Vector2Data Position { get; }
    public string Text { get; }
}
