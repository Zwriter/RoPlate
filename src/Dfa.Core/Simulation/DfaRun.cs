namespace Dfa.Core.Simulation;

using Dfa.Core.Model;

public sealed class DfaRun
{
    private readonly List<DfaStep> _steps = [];
    private int _position;

    public DfaRun(Dfa dfa, string input)
    {
        Dfa = dfa;
        Input = input;

        var i = 0;
        while (i < input.Length && TryAddStep(i))
        {
            i++;
        }
    }

    public Dfa Dfa { get; }

    public string Input { get; private set; }

    public IReadOnlyList<DfaStep> Steps => _steps;

    public int Position => _position;

    public bool IsAtStart => _position == 0;

    public bool IsAtEnd => _position == _steps.Count;

    public bool IsBlocked => LastStep is { Outcome: not StepOutcome.Moved };

    public string? CurrentState => StateAt(_position);

    public DfaStep? LastStep => _position == 0 ? null : _steps[_position - 1];

    public string RemainingInput => LastStep is { } step ? Input[(step.Position + 1)..] : Input;

    public Verdict Verdict => Evaluate(_position).Verdict;

    public RejectReason Reason => Evaluate(_position).Reason;

    public string Explanation => Explain(_position);

    public Verdict FinalVerdict => Evaluate(_steps.Count).Verdict;

    public event EventHandler? Changed;

    public bool StepForward() => !IsAtEnd && MoveTo(_position + 1);

    public bool StepBack() => !IsAtStart && MoveTo(_position - 1);

    public void Reset() => MoveTo(0);

    public void RunToEnd() => MoveTo(_steps.Count);

    public void Append(char c)
    {
        Input += c;
        TryAddStep(Input.Length - 1);
        _position = _steps.Count;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveLast()
    {
        if (Input.Length == 0)
        {
            return;
        }

        Input = Input[..^1];

        // Characters typed after a block have no step, so only the character that made the last step removes it.
        if (_steps.Count > 0 && _steps[^1].Position == Input.Length)
        {
            _steps.RemoveAt(_steps.Count - 1);
        }

        _position = _steps.Count;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private bool TryAddStep(int position)
    {
        if (Dfa.StartState is null || _steps is [.., { Outcome: not StepOutcome.Moved }])
        {
            return false;
        }

        var from = _steps.Count == 0 ? Dfa.StartState : _steps[^1].To!;
        var c = Input[position];
        var symbol = Dfa.SymbolOf(c);
        var to = symbol is null ? null : Dfa.Next(from, symbol);
        var outcome = symbol is null ? StepOutcome.NotInAlphabet
            : to is null ? StepOutcome.NoTransition
            : StepOutcome.Moved;

        _steps.Add(new DfaStep(_steps.Count + 1, position, c, symbol, from, to, outcome));
        return true;
    }

    private bool MoveTo(int position)
    {
        if (position != _position)
        {
            _position = position;
            Changed?.Invoke(this, EventArgs.Empty);
        }

        return true;
    }

    private string? StateAt(int position)
    {
        if (position == 0)
        {
            return Dfa.StartState;
        }

        var step = _steps[position - 1];
        return step.To ?? step.From;
    }

    private (Verdict Verdict, RejectReason Reason) Evaluate(int position)
    {
        if (Dfa.StartState is null)
        {
            return (Verdict.Rejected, RejectReason.NoStartState);
        }

        var outcome = position == 0 ? StepOutcome.Moved : _steps[position - 1].Outcome;
        if (outcome == StepOutcome.NoTransition)
        {
            return (Verdict.Rejected, RejectReason.NoTransition);
        }

        if (outcome == StepOutcome.NotInAlphabet)
        {
            return (Verdict.Rejected, RejectReason.NotInAlphabet);
        }

        if (position < _steps.Count)
        {
            return (Verdict.InProgress, RejectReason.None);
        }

        return Dfa.IsFinal(StateAt(position)!)
            ? (Verdict.Accepted, RejectReason.None)
            : (Verdict.Rejected, RejectReason.EndedInNonFinal);
    }

    private string Explain(int position)
    {
        var (verdict, reason) = Evaluate(position);
        var state = StateAt(position);
        var step = position == 0 ? null : _steps[position - 1];

        return (verdict, reason) switch
        {
            (Verdict.InProgress, _) => $"Step {position} of {_steps.Count}.",
            (Verdict.Accepted, _) => $"ACCEPTED: the word ended in the final state {state}.",
            (_, RejectReason.NoStartState) => "REJECTED: the automaton has no start state.",
            (_, RejectReason.EndedInNonFinal) => $"REJECTED: the word ended in {state}, which is not final.",
            (_, RejectReason.NotInAlphabet) =>
                $"REJECTED: symbol '{step!.Character}' is not in the alphabet (position {step.Position + 1}).",
            _ when step!.Symbol == step.Character.ToString() =>
                $"REJECTED: from {step.From} there is no transition on '{step.Character}' (position {step.Position + 1}).",
            _ => $"REJECTED: from {step.From} there is no transition on '{step.Character}' " +
                $"(class {step.Symbol}, position {step.Position + 1}).",
        };
    }
}
