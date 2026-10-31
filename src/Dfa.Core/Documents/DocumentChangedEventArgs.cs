namespace Dfa.Core.Documents;

public sealed class DocumentChangedEventArgs(DocumentChangeKind kind) : EventArgs
{
    public DocumentChangeKind Kind { get; } = kind;
}
