namespace NuevoMMO.Editor;

public sealed class EditorHistory
{
    private readonly Stack<ChangeSet> undo = [];
    private readonly Stack<ChangeSet> redo = [];
    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;
    public void Push(ChangeSet change) { change.Apply(); undo.Push(change); redo.Clear(); }
    public void Undo() { if (!CanUndo) return; var change = undo.Pop(); change.Revert(); redo.Push(change); }
    public void Redo() { if (!CanRedo) return; var change = redo.Pop(); change.Apply(); undo.Push(change); }
}
