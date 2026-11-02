namespace Dfa.Tests.Core;

using Dfa.Core.Model;
using Dfa.Core.Validation;
using Shouldly;

public class DfaValidatorTests
{
    [Fact]
    public void CompleteExample_HasNoIssues()
    {
        var issues = DfaValidator.Validate(TestAutomata.EvenA());

        issues.ShouldBeEmpty();
        DfaValidator.CanSimulate(issues).ShouldBeTrue();
    }

    [Fact]
    public void EmptyAutomaton_ReportsNoStatesAndEmptyAlphabetOnly()
    {
        var issues = DfaValidator.Validate(new Dfa());

        issues.Select(i => i.Code).ShouldBe(["NO_STATES", "EMPTY_ALPHABET"]);
        issues[0].Message.ShouldBe("The automaton has no states.");
        issues[1].Message.ShouldBe("The alphabet is empty.");
        DfaValidator.CanSimulate(issues).ShouldBeFalse();
    }

    [Fact]
    public void NoStart_IsAnError()
    {
        var dfa = TestAutomata.EvenA();
        dfa.SetStart(null);

        var issue = DfaValidator.Validate(dfa).ShouldHaveSingleItem();

        issue.ShouldBe(new ValidationIssue(Severity.Error, "NO_START", "The automaton has no start state."));
    }

    [Fact]
    public void Unreachable_IsAWarningPerState()
    {
        var dfa = TestAutomata.EvenA();
        dfa.AddState("x");
        dfa.AddTransitions("x", ["a", "b"], "q0");

        var issues = DfaValidator.Validate(dfa);

        issues.ShouldBe([new ValidationIssue(
            Severity.Warning, "UNREACHABLE", "State x cannot be reached from the start state.", State: "x")]);
        DfaValidator.CanSimulate(issues).ShouldBeTrue();
    }

    [Fact]
    public void NoFinal_IsAWarning()
    {
        var dfa = TestAutomata.EvenA();
        dfa.SetFinal("q0", false);

        DfaValidator.Validate(dfa).ShouldBe([new ValidationIssue(
            Severity.Warning, "NO_FINAL", "The automaton has no final states: it will reject every word.")]);
    }

    [Fact]
    public void MissingTransition_IsInfoPerStateAndSymbol()
    {
        var dfa = TestAutomata.EvenA();
        dfa.RemoveTransition("q1", "b");

        DfaValidator.Validate(dfa).ShouldBe([new ValidationIssue(
            Severity.Info, "MISSING_TRANSITION", "From q1 the transition on 'b' is missing.", "q1", "b")]);
    }
}
