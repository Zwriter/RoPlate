namespace Dfa.Tests.Core;

using Dfa.Core.Documents;
using Dfa.Core.Model;
using Dfa.Core.Simulation;
using Shouldly;

public class RecordTests
{
    [Fact]
    public void BatchResult_Summary_CountsPassedCases()
    {
        var results = new[]
        {
            new TestResult(new TestCase("a", true), true, ""),
            new TestResult(new TestCase("b", false), true, ""),
        };

        new BatchResult(results).Summary.ShouldBe("1 of 2 correct");
    }

    [Fact]
    public void OpResult_Fail_CarriesMessage()
    {
        var result = OpResult.Fail("x");

        result.Ok.ShouldBeFalse();
        result.Error.ShouldBe("x");
    }
}
