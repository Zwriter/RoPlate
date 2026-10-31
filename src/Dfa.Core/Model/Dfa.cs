namespace Dfa.Core.Model;

public sealed class Dfa
{
    public IReadOnlyList<string> States => throw new NotImplementedException();

    public IReadOnlyList<string> Alphabet => throw new NotImplementedException();

    public IReadOnlyList<SymbolClass> SymbolClasses => throw new NotImplementedException();

    public string? StartState => throw new NotImplementedException();

    public IReadOnlySet<string> FinalStates => throw new NotImplementedException();

    public IReadOnlyDictionary<TransitionKey, string> Transitions => throw new NotImplementedException();

    public IReadOnlyList<Edge> Edges => throw new NotImplementedException();

    public string? Next(string state, string symbol) => throw new NotImplementedException();

    public string? SymbolOf(char c) => throw new NotImplementedException();

    public bool IsFinal(string state) => throw new NotImplementedException();

    public OpResult AddState(string name) => throw new NotImplementedException();

    public OpResult RemoveState(string name) => throw new NotImplementedException();

    public OpResult RenameState(string oldName, string newName) => throw new NotImplementedException();

    public OpResult SetStart(string? name) => throw new NotImplementedException();

    public OpResult SetFinal(string name, bool isFinal) => throw new NotImplementedException();

    public OpResult AddSymbol(string symbol) => throw new NotImplementedException();

    public OpResult AddSymbolClass(SymbolClass symbolClass) => throw new NotImplementedException();

    public OpResult RemoveSymbol(string symbol) => throw new NotImplementedException();

    public OpResult AddTransition(string from, string symbol, string to) => throw new NotImplementedException();

    public OpResult AddTransitions(string from, IEnumerable<string> symbols, string to) =>
        throw new NotImplementedException();

    public OpResult RemoveTransition(string from, string symbol) => throw new NotImplementedException();

    public Dfa Clone() => throw new NotImplementedException();
}
