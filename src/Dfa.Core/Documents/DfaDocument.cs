namespace Dfa.Core.Documents;

using Dfa.Core.Model;

public sealed class DfaDocument
{
    private readonly Dictionary<string, Point2> _layout = [];
    private readonly Dictionary<string, string> _descriptions = [];
    private readonly List<TestCase> _tests = [];
    private Dfa _dfa = new();

    private DfaDocument(string name) => Name = name;

    public string Name { get; set; }

    public Dfa Dfa => _dfa;

    public IReadOnlyDictionary<string, Point2> Layout => _layout;

    public IReadOnlyDictionary<string, string> Descriptions => _descriptions;

    public IReadOnlyList<TestCase> Tests => _tests;

    public event EventHandler<DocumentChangedEventArgs>? Changed;

    public OpResult AddState(Point2 position)
    {
        var i = 0;
        while (_dfa.States.Contains($"q{i}"))
        {
            i++;
        }

        return AddState($"q{i}", position);
    }

    public OpResult AddState(string name, Point2 position)
    {
        var result = _dfa.AddState(name);
        if (result.Ok)
        {
            _layout[name] = position;
        }

        return Raise(result, DocumentChangeKind.Structure);
    }

    public OpResult RemoveState(string name)
    {
        var result = _dfa.RemoveState(name);
        if (result.Ok)
        {
            _layout.Remove(name);
            _descriptions.Remove(name);
        }

        return Raise(result, DocumentChangeKind.Structure);
    }

    public OpResult RenameState(string oldName, string newName)
    {
        var result = _dfa.RenameState(oldName, newName);
        if (result.Ok && oldName != newName)
        {
            Move(_layout, oldName, newName);
            Move(_descriptions, oldName, newName);
        }

        return Raise(result, DocumentChangeKind.Structure);
    }

    public OpResult MoveState(string name, Point2 position)
    {
        if (!_dfa.States.Contains(name))
        {
            return Dfa.StateNotFound(name);
        }

        _layout[name] = position;
        return Raise(OpResult.Success, DocumentChangeKind.Layout);
    }

    public OpResult SetStart(string? name) => Raise(_dfa.SetStart(name), DocumentChangeKind.Structure);

    public OpResult SetFinal(string name, bool isFinal) =>
        Raise(_dfa.SetFinal(name, isFinal), DocumentChangeKind.Structure);

    public OpResult SetDescription(string state, string? text)
    {
        if (!_dfa.States.Contains(state))
        {
            return Dfa.StateNotFound(state);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            _descriptions.Remove(state);
        }
        else
        {
            _descriptions[state] = text;
        }

        return Raise(OpResult.Success, DocumentChangeKind.Description);
    }

    public OpResult AddSymbol(string symbol) => Raise(_dfa.AddSymbol(symbol), DocumentChangeKind.Structure);

    public OpResult AddSymbolClass(SymbolClass symbolClass) =>
        Raise(_dfa.AddSymbolClass(symbolClass), DocumentChangeKind.Structure);

    public OpResult RemoveSymbol(string symbol) => Raise(_dfa.RemoveSymbol(symbol), DocumentChangeKind.Structure);

    public OpResult AddTransitions(string from, IEnumerable<string> symbols, string to) =>
        Raise(_dfa.AddTransitions(from, symbols, to), DocumentChangeKind.Structure);

    public OpResult RemoveTransition(string from, string symbol) =>
        Raise(_dfa.RemoveTransition(from, symbol), DocumentChangeKind.Structure);

    public OpResult RemoveEdge(string from, string to)
    {
        if (!_dfa.States.Contains(from))
        {
            return Dfa.StateNotFound(from);
        }

        if (!_dfa.States.Contains(to))
        {
            return Dfa.StateNotFound(to);
        }

        var edge = _dfa.Edges.FirstOrDefault(e => e.From == from && e.To == to);
        if (edge is null)
        {
            return OpResult.Fail($"From {from} there is no transition to {to}.");
        }

        foreach (var symbol in edge.Symbols)
        {
            _dfa.RemoveTransition(from, symbol);
        }

        return Raise(OpResult.Success, DocumentChangeKind.Structure);
    }

    public OpResult CompleteWithTrap(Point2 position, string name = "trap") =>
        throw new NotImplementedException();

    public void SetTests(IEnumerable<TestCase> tests)
    {
        _tests.Clear();
        _tests.AddRange(tests);
        Raise(OpResult.Success, DocumentChangeKind.Tests);
    }

    public DfaDocument Clone()
    {
        var copy = new DfaDocument(Name);
        copy.CopyFrom(this);
        return copy;
    }

    public void RestoreFrom(DfaDocument snapshot)
    {
        Name = snapshot.Name;
        CopyFrom(snapshot);
        Raise(OpResult.Success, DocumentChangeKind.Reset);
    }

    public static DfaDocument CreateEmpty(string name = "New automaton") => new(name);

    private void CopyFrom(DfaDocument source)
    {
        _dfa = source._dfa.Clone();
        Replace(_layout, source._layout);
        Replace(_descriptions, source._descriptions);
        _tests.Clear();
        _tests.AddRange(source._tests);
    }

    private OpResult Raise(OpResult result, DocumentChangeKind kind)
    {
        if (result.Ok)
        {
            Changed?.Invoke(this, new DocumentChangedEventArgs(kind));
        }

        return result;
    }

    private static void Move<T>(Dictionary<string, T> map, string oldKey, string newKey)
    {
        if (map.Remove(oldKey, out var value))
        {
            map[newKey] = value;
        }
    }

    private static void Replace<T>(Dictionary<string, T> target, Dictionary<string, T> source)
    {
        target.Clear();
        foreach (var (key, value) in source)
        {
            target[key] = value;
        }
    }
}
