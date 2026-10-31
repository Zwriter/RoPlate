namespace Dfa.Core.Documents;

public sealed class DocumentHistory
{
    public DocumentHistory(DfaDocument document, int capacity = 100) => throw new NotImplementedException();

    public bool CanUndo => throw new NotImplementedException();

    public bool CanRedo => throw new NotImplementedException();

#pragma warning disable CS0067 // stub: raised once DocumentHistory is implemented (C-09)
    public event EventHandler? Changed;
#pragma warning restore CS0067

    public void Undo() => throw new NotImplementedException();

    public void Redo() => throw new NotImplementedException();

    public void Clear() => throw new NotImplementedException();
}
