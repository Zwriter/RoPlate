namespace Dfa.Core.Simulation;

using Dfa.Core.Documents;
using Dfa.Core.Model;

public static class BatchTester
{
    public static BatchResult Run(Dfa dfa, IEnumerable<TestCase> tests) =>
        new([.. tests.Select(test => Evaluate(dfa, test))]);

    private static TestResult Evaluate(Dfa dfa, TestCase test)
    {
        var run = new DfaRun(dfa, test.Word);
        run.RunToEnd();
        return new TestResult(test, run.Verdict == Verdict.Accepted, run.Explanation);
    }
}
