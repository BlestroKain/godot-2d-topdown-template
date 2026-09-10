namespace NuevoMMO.Editor;

public sealed class EditorNetworking
{
    public bool Connected { get; private set; }
    public void Connect() => Connected = true;
    public void Disconnect() => Connected = false;
}
