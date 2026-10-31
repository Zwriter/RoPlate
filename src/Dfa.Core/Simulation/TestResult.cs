namespace Dfa.Core.Simulation;

using Dfa.Core.Documents;

public sealed record TestResult(TestCase Case, bool Actual, string Explanation)
{
    public bool Passed => Actual == Case.Expected;
}
