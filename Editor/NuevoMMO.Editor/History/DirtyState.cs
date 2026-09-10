namespace NuevoMMO.Editor;

public sealed class DirtyState
{
    public bool IsDirty { get; private set; }
    public void Mark() => IsDirty = true;
    public void Clear() => IsDirty = false;
}
