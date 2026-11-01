namespace Dfa.Core.Model;

public sealed partial class Dfa
{
    private readonly List<string> _states = [];
    private readonly List<string> _alphabet = [];
    private readonly List<SymbolClass> _symbolClasses = [];
    private readonly HashSet<string> _finalStates = [];
    private readonly Dictionary<TransitionKey, string> _transitions = [];

    public IReadOnlyList<string> States => _states;

    public IReadOnlyList<string> Alphabet => _alphabet;

    public IReadOnlyList<SymbolClass> SymbolClasses => _symbolClasses;

    public string? StartState { get; private set; }

    public IReadOnlySet<string> FinalStates => _finalStates;

    public IReadOnlyDictionary<TransitionKey, string> Transitions => _transitions;

    public bool IsFinal(string state) => _finalStates.Contains(state);

    public OpResult AddState(string name)
    {
        var check = CheckNewStateName(name);
        if (!check.Ok)
        {
            return check;
        }

        _states.Add(name);
        return OpResult.Success;
    }

    public OpResult RemoveState(string name)
    {
        if (!_states.Remove(name))
        {
            return StateNotFound(name);
        }

        foreach (var key in _transitions.Where(t => t.Key.State == name || t.Value == name).Select(t => t.Key).ToList())
        {
            _transitions.Remove(key);
        }

        _finalStates.Remove(name);
        if (StartState == name)
        {
            StartState = null;
        }

        return OpResult.Success;
    }

    public OpResult RenameState(string oldName, string newName)
    {
        var index = _states.IndexOf(oldName);
        if (index < 0)
        {
            return StateNotFound(oldName);
        }

        if (oldName == newName)
        {
            return OpResult.Success;
        }

        var check = CheckNewStateName(newName);
        if (!check.Ok)
        {
            return check;
        }

        _states[index] = newName;
        foreach (var (key, to) in _transitions.ToList())
        {
            if (key.State == oldName || to == oldName)
            {
                _transitions.Remove(key);
                _transitions[key with { State = Rename(key.State) }] = Rename(to);
            }
        }

        if (_finalStates.Remove(oldName))
        {
            _finalStates.Add(newName);
        }

        StartState = StartState == oldName ? newName : StartState;
        return OpResult.Success;

        string Rename(string state) => state == oldName ? newName : state;
    }

    public OpResult SetStart(string? name)
    {
        if (name is not null && !_states.Contains(name))
        {
            return StateNotFound(name);
        }

        StartState = name;
        return OpResult.Success;
    }

    public OpResult SetFinal(string name, bool isFinal)
    {
        if (!_states.Contains(name))
        {
            return StateNotFound(name);
        }

        if (isFinal)
        {
            _finalStates.Add(name);
        }
        else
        {
            _finalStates.Remove(name);
        }

        return OpResult.Success;
    }

    public Dfa Clone()
    {
        var copy = new Dfa { StartState = StartState };
        copy._states.AddRange(_states);
        copy._alphabet.AddRange(_alphabet);
        copy._symbolClasses.AddRange(_symbolClasses);
        copy._finalStates.UnionWith(_finalStates);
        foreach (var (key, to) in _transitions)
        {
            copy._transitions[key] = to;
        }

        return copy;
    }

    private OpResult CheckNewStateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return OpResult.Fail("The state name cannot be empty.");
        }

        return _states.Contains(name)
            ? OpResult.Fail($"A state named '{name}' already exists.")
            : OpResult.Success;
    }

    private static OpResult StateNotFound(string name) => OpResult.Fail($"State '{name}' does not exist.");
}
