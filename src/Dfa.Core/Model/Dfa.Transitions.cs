namespace Dfa.Core.Model;

public sealed partial class Dfa
{
    public IReadOnlyList<Edge> Edges =>
        _transitions
            .GroupBy(t => (From: t.Key.State, To: t.Value))
            .OrderBy(g => _states.IndexOf(g.Key.From))
            .ThenBy(g => _states.IndexOf(g.Key.To))
            .Select(g => new Edge(
                g.Key.From,
                g.Key.To,
                g.Select(t => t.Key.Symbol).OrderBy(_alphabet.IndexOf).ToList()))
            .ToList();

    public string? Next(string state, string symbol) =>
        _transitions.GetValueOrDefault(new TransitionKey(state, symbol));

    public OpResult AddTransition(string from, string symbol, string to) => AddTransitions(from, [symbol], to);

    public OpResult AddTransitions(string from, IEnumerable<string> symbols, string to)
    {
        if (!_states.Contains(from))
        {
            return StateNotFound(from);
        }

        if (!_states.Contains(to))
        {
            return StateNotFound(to);
        }

        // Validate everything first so a failing symbol leaves δ untouched ("a,b" is all or nothing).
        var pending = new List<string>();
        foreach (var symbol in symbols)
        {
            if (!_alphabet.Contains(symbol))
            {
                return SymbolNotInAlphabet(symbol);
            }

            var existing = pending.Contains(symbol) ? to : Next(from, symbol);
            if (existing is not null)
            {
                return OpResult.Fail($"From {from} there is already a transition on '{symbol}' to {existing}.");
            }

            pending.Add(symbol);
        }

        foreach (var symbol in pending)
        {
            _transitions[new TransitionKey(from, symbol)] = to;
        }

        return OpResult.Success;
    }

    public OpResult RemoveTransition(string from, string symbol)
    {
        if (!_states.Contains(from))
        {
            return StateNotFound(from);
        }

        if (!_alphabet.Contains(symbol))
        {
            return SymbolNotInAlphabet(symbol);
        }

        return _transitions.Remove(new TransitionKey(from, symbol))
            ? OpResult.Success
            : OpResult.Fail($"From {from} there is no transition on '{symbol}'.");
    }
}
