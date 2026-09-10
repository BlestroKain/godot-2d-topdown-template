namespace NuevoMMO.Server.Notifications;

public sealed class NotificationService
{
    public event Action<string>? Broadcast;
    public void Send(string message) => Broadcast?.Invoke(message);
}
