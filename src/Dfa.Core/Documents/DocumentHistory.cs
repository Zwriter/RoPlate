namespace Dfa.Core.Documents;

public sealed class DocumentHistory
{
    private readonly DfaDocument _document;
    private readonly int _capacity;
    private readonly List<DfaDocument> _undo = [];
    private readonly List<DfaDocument> _redo = [];

    // A copy of the document as it is now: the "before" snapshot of the next change.
    private DfaDocument _current;

    public DocumentHistory(DfaDocument document, int capacity = 100)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _document = document;
        _capacity = capacity;
        _current = document.Clone();
        document.Changed += OnDocumentChanged;
    }

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public event EventHandler? Changed;

    public void Undo() => Step(_undo, _redo);

    public void Redo() => Step(_redo, _undo);

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        _current = _document.Clone();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        // Reset comes from our own Undo/Redo (RestoreFrom), which is not a new edit.
        if (e.Kind == DocumentChangeKind.Reset)
        {
            return;
        }

        _undo.Add(_current);
        if (_undo.Count > _capacity)
        {
            _undo.RemoveAt(0);
        }

        _redo.Clear();
        _current = _document.Clone();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void Step(List<DfaDocument> from, List<DfaDocument> to)
    {
        if (from.Count == 0)
        {
            return;
        }

        to.Add(_current);
        _current = from[^1];
        from.RemoveAt(from.Count - 1);
        _document.RestoreFrom(_current);
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
