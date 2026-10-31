namespace Dfa.Core.Simulation;

using Dfa.Core.Model;

public sealed class DfaRun
{
    public DfaRun(Dfa dfa, string input) => throw new NotImplementedException();

    public Dfa Dfa => throw new NotImplementedException();

    public string Input => throw new NotImplementedException();

    public IReadOnlyList<DfaStep> Steps => throw new NotImplementedException();

    public int Position => throw new NotImplementedException();

    public bool IsAtStart => throw new NotImplementedException();

    public bool IsAtEnd => throw new NotImplementedException();

    public bool IsBlocked => throw new NotImplementedException();

    public string? CurrentState => throw new NotImplementedException();

    public DfaStep? LastStep => throw new NotImplementedException();

    public string RemainingInput => throw new NotImplementedException();

    public Verdict Verdict => throw new NotImplementedException();

    public RejectReason Reason => throw new NotImplementedException();

    public string Explanation => throw new NotImplementedException();

    public Verdict FinalVerdict => throw new NotImplementedException();

#pragma warning disable CS0067 // stub: raised once DfaRun is implemented (C-03)
    public event EventHandler? Changed;
#pragma warning restore CS0067

    public bool StepForward() => throw new NotImplementedException();

    public bool StepBack() => throw new NotImplementedException();

    public void Reset() => throw new NotImplementedException();

    public void RunToEnd() => throw new NotImplementedException();

    public void Append(char c) => throw new NotImplementedException();

    public void RemoveLast() => throw new NotImplementedException();
}
