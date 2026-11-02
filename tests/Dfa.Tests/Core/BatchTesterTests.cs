namespace Dfa.Tests.Core;

using Dfa.Core.Documents;
using Dfa.Core.Simulation;
using Shouldly;

public class BatchTesterTests
{
    [Fact]
    public void Run_ComparesEachWordWithItsExpectedVerdict()
    {
        TestCase[] tests =
        [
            new("", true), new("aa", true), new("b", true), new("abba", true), new("bb", true),
            new("a", false), new("bab", false), new("aaa", false), new("ab", false),
            new("a", true), // wrong expectation: the one failure
        ];

        var result = BatchTester.Run(TestAutomata.EvenA(), tests);

        result.Summary.ShouldBe("9 of 10 correct");
        result.Results.Select(r => r.Case).ShouldBe(tests);
        var failure = result.Results.Single(r => !r.Passed);
        failure.Actual.ShouldBeFalse();
        failure.Explanation.ShouldBe("REJECTED: the word ended in q1, which is not final.");
    }

    [Fact]
    public void Run_ExplanationIsTheFinalVerdict()
    {
        var result = BatchTester.Run(TestAutomata.EvenA(), [new TestCase("abc", false), new TestCase("aa", true)]);

        result.Results.Select(r => r.Explanation).ShouldBe([
            "REJECTED: symbol 'c' is not in the alphabet (position 3).",
            "ACCEPTED: the word ended in the final state q0.",
        ]);
        result.Summary.ShouldBe("2 of 2 correct");
    }

    [Fact]
    public void Run_NoTests_IsZeroOfZero()
    {
        BatchTester.Run(TestAutomata.EvenA(), []).Summary.ShouldBe("0 of 0 correct");
    }
}
