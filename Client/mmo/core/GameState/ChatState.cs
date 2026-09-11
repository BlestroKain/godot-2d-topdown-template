namespace NuevoMMO.Client;

public enum ChatChannel : byte
{
    System,
    Local,
    Combat
}

public sealed record ChatMessage(ChatChannel Channel, string Text, long TimestampMilliseconds);

public sealed class ChatState
{
    public const int Capacity = 100;
    private readonly List<ChatMessage> messages = [];

    public IReadOnlyList<ChatMessage> Messages => messages;

    public void Append(ChatChannel channel, string text, long timestampMilliseconds = 0)
    {
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("Mensaje vacío.", nameof(text));
        messages.Add(new ChatMessage(channel, text.Trim(), timestampMilliseconds));
        if (messages.Count > Capacity)
            messages.RemoveRange(0, messages.Count - Capacity);
    }

    public void Clear() => messages.Clear();
}
