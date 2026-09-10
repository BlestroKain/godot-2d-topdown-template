namespace NuevoMMO.Editor;

public sealed class ChangeSet(string description, Action apply, Action revert)
{
    public string Description { get; } = description;
    public void Apply() => apply();
    public void Revert() => revert();
}
