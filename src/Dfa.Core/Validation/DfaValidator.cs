namespace Dfa.Core.Validation;

using Dfa.Core.Model;

public static class DfaValidator
{
    public static IReadOnlyList<ValidationIssue> Validate(Dfa dfa)
    {
        var issues = new List<ValidationIssue>();

        if (dfa.States.Count == 0)
        {
            issues.Add(new(Severity.Error, "NO_STATES", "The automaton has no states."));
        }
        else if (dfa.StartState is null)
        {
            issues.Add(new(Severity.Error, "NO_START", "The automaton has no start state."));
        }

        if (dfa.Alphabet.Count == 0)
        {
            issues.Add(new(Severity.Error, "EMPTY_ALPHABET", "The alphabet is empty."));
        }

        if (dfa.StartState is not null)
        {
            var reachable = Reachable(dfa, dfa.StartState);
            issues.AddRange(dfa.States.Where(s => !reachable.Contains(s)).Select(s => new ValidationIssue(
                Severity.Warning, "UNREACHABLE", $"State {s} cannot be reached from the start state.", State: s)));
        }

        if (dfa.States.Count > 0 && dfa.FinalStates.Count == 0)
        {
            issues.Add(new(
                Severity.Warning, "NO_FINAL", "The automaton has no final states: it will reject every word."));
        }

        foreach (var state in dfa.States)
        {
            issues.AddRange(dfa.Alphabet.Where(symbol => dfa.Next(state, symbol) is null).Select(symbol =>
                new ValidationIssue(
                    Severity.Info,
                    "MISSING_TRANSITION",
                    $"From {state} the transition on '{symbol}' is missing.",
                    state,
                    symbol)));
        }

        return issues;
    }

    public static bool CanSimulate(IReadOnlyList<ValidationIssue> issues) =>
        issues.All(i => i.Severity != Severity.Error);

    private static HashSet<string> Reachable(Dfa dfa, string start)
    {
        var reached = new HashSet<string> { start };
        var queue = new Queue<string>([start]);
        while (queue.TryDequeue(out var state))
        {
            foreach (var edge in dfa.Edges.Where(e => e.From == state && reached.Add(e.To)))
            {
                queue.Enqueue(edge.To);
            }
        }

        return reached;
    }
}
