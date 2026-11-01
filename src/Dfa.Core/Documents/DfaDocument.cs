namespace Dfa.Core.Documents;

using Dfa.Core.Model;

public sealed class DfaDocument
{
    public string Name
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public Dfa Dfa => throw new NotImplementedException();

    public IReadOnlyDictionary<string, Point2> Layout => throw new NotImplementedException();

    public IReadOnlyDictionary<string, string> Descriptions => throw new NotImplementedException();

    public IReadOnlyList<TestCase> Tests => throw new NotImplementedException();

#pragma warning disable CS0067 // stub: raised once DfaDocument is implemented (C-05)
    public event EventHandler<DocumentChangedEventArgs>? Changed;
#pragma warning restore CS0067

    public OpResult AddState(Point2 position) => throw new NotImplementedException();

    public OpResult AddState(string name, Point2 position) => throw new NotImplementedException();

    public OpResult RemoveState(string name) => throw new NotImplementedException();

    public OpResult RenameState(string oldName, string newName) => throw new NotImplementedException();

    public OpResult MoveState(string name, Point2 position) => throw new NotImplementedException();

    public OpResult SetStart(string? name) => throw new NotImplementedException();

    public OpResult SetFinal(string name, bool isFinal) => throw new NotImplementedException();

    public OpResult SetDescription(string state, string? text) => throw new NotImplementedException();

    public OpResult AddSymbol(string symbol) => throw new NotImplementedException();

    public OpResult AddSymbolClass(SymbolClass symbolClass) => throw new NotImplementedException();

    public OpResult RemoveSymbol(string symbol) => throw new NotImplementedException();

    public OpResult AddTransitions(string from, IEnumerable<string> symbols, string to) =>
        throw new NotImplementedException();

    public OpResult RemoveTransition(string from, string symbol) => throw new NotImplementedException();

    public OpResult RemoveEdge(string from, string to) => throw new NotImplementedException();

    public OpResult CompleteWithTrap(Point2 position, string name = "trap") =>
        throw new NotImplementedException();

    public void SetTests(IEnumerable<TestCase> tests) => throw new NotImplementedException();

    public DfaDocument Clone() => throw new NotImplementedException();

    public void RestoreFrom(DfaDocument snapshot) => throw new NotImplementedException();

    public static DfaDocument CreateEmpty(string name = "New automaton") => throw new NotImplementedException();
}
