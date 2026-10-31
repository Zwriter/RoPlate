namespace Dfa.Core.Simulation;

public sealed record BatchResult(IReadOnlyList<TestResult> Results)
{
    public int Passed => Results.Count(r => r.Passed);

    public int Total => Results.Count;

    public string Summary => $"{Passed} din {Total} corecte";
}
