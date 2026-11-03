namespace Dfa.App.Services;

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Dfa.Core.Documents;

public sealed partial class DocumentSession : ObservableObject
{
    [ObservableProperty]
    private string? _filePath;

    [ObservableProperty]
    private bool _isDirty;

    public DocumentSession()
    {
        History = new DocumentHistory(Document);
        Document.Changed += OnDocumentChanged;
    }

    // One instance for the whole session: Replace copies into it, so History and every subscriber stay attached.
    public DfaDocument Document { get; } = DfaDocument.CreateEmpty();

    public DocumentHistory History { get; }

    public event EventHandler? DocumentReplaced;

    public event EventHandler<DocumentChangedEventArgs>? DocumentEdited;

    public void Replace(DfaDocument document, string? filePath)
    {
        // A replacement is not an edit: no DocumentEdited, no dirty flag.
        Document.Changed -= OnDocumentChanged;
        Document.RestoreFrom(document);
        Document.Changed += OnDocumentChanged;

        History.Clear();
        FilePath = filePath;
        IsDirty = false;
        DocumentReplaced?.Invoke(this, EventArgs.Empty);
    }

    private void OnDocumentChanged(object? sender, DocumentChangedEventArgs e)
    {
        IsDirty = true;
        DocumentEdited?.Invoke(this, e);
    }
}
