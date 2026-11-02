namespace Dfa.Tests.Core;

using Dfa.Core.Model;
using Dfa.Core.Simulation;
using Shouldly;

public class DfaRunTests
{
    [Fact]
    public void EmptyWord_HasNoStepsAndDependsOnStartState()
    {
        var run = new DfaRun(TestAutomata.EvenA(), "");

        run.Steps.ShouldBeEmpty();
        run.IsAtEnd.ShouldBeTrue();
        run.Verdict.ShouldBe(Verdict.Accepted);
        run.Explanation.ShouldBe("ACCEPTED: the word ended in the final state q0.");
    }

    [Fact]
    public void EndInNonFinal_IsRejectedWithReason()
    {
        var run = new DfaRun(TestAutomata.EvenA(), "ab");
        run.RunToEnd();

        run.Reason.ShouldBe(RejectReason.EndedInNonFinal);
        run.Explanation.ShouldBe("REJECTED: the word ended in q1, which is not final.");
    }

    [Fact]
    public void MissingTransition_BlocksOnLastStep()
    {
        var dfa = TestAutomata.Build("q0", ["q1"], ("q0", "a", "q1"), ("q1", "b", "q1"));

        var run = new DfaRun(dfa, "aaba");
        run.RunToEnd();

        run.Steps.Count.ShouldBe(2);
        run.Steps[^1].Outcome.ShouldBe(StepOutcome.NoTransition);
        run.Steps[^1].To.ShouldBeNull();
        run.IsBlocked.ShouldBeTrue();
        run.CurrentState.ShouldBe("q1");
        run.Reason.ShouldBe(RejectReason.NoTransition);
        run.Explanation.ShouldBe("REJECTED: from q1 there is no transition on 'a' (position 2).");
    }

    [Fact]
    public void MissingTransition_OnClass_NamesTheClass()
    {
        var dfa = new Dfa();
        dfa.AddState("q0");
        dfa.AddSymbolClass(SymbolClass.FromRanges("L", "A-Z"));
        dfa.AddSymbolClass(SymbolClass.FromRanges("C", "0-9"));
        dfa.AddTransition("q0", "L", "q0");
        dfa.SetStart("q0");

        var run = new DfaRun(dfa, "AB7");
        run.RunToEnd();

        run.Steps[^1].Symbol.ShouldBe("C");
        run.Explanation.ShouldBe("REJECTED: from q0 there is no transition on '7' (class C, position 3).");
    }

    [Fact]
    public void ForeignSymbol_StopsTheRun()
    {
        var run = new DfaRun(TestAutomata.EvenA(), "abcab");
        run.RunToEnd();

        run.Steps.Count.ShouldBe(3);
        run.Steps[^1].Outcome.ShouldBe(StepOutcome.NotInAlphabet);
        run.Steps[^1].Symbol.ShouldBeNull();
        run.Reason.ShouldBe(RejectReason.NotInAlphabet);
        run.Explanation.ShouldBe("REJECTED: symbol 'c' is not in the alphabet (position 3).");
    }

    [Fact]
    public void NoStartState_IsRejectedWithoutSteps()
    {
        var dfa = TestAutomata.EvenA();
        dfa.SetStart(null);

        var run = new DfaRun(dfa, "aa");

        run.Steps.ShouldBeEmpty();
        run.CurrentState.ShouldBeNull();
        run.Verdict.ShouldBe(Verdict.Rejected);
        run.Reason.ShouldBe(RejectReason.NoStartState);
        run.Explanation.ShouldBe("REJECTED: the automaton has no start state.");
    }

    [Fact]
    public void Navigation_MovesThroughPrecomputedSteps()
    {
        var run = new DfaRun(TestAutomata.EvenA(), "aba");
        var changes = 0;
        run.Changed += (_, _) => changes++;

        run.IsAtStart.ShouldBeTrue();
        run.StepBack().ShouldBeFalse();
        run.Explanation.ShouldBe("Step 0 of 3.");

        run.StepForward().ShouldBeTrue();
        run.CurrentState.ShouldBe("q1");
        run.RemainingInput.ShouldBe("ba");
        run.Verdict.ShouldBe(Verdict.InProgress);
        run.FinalVerdict.ShouldBe(Verdict.Accepted);
        run.Explanation.ShouldBe("Step 1 of 3.");

        run.RunToEnd();
        run.IsAtEnd.ShouldBeTrue();
        run.StepForward().ShouldBeFalse();
        run.RemainingInput.ShouldBe("");
        run.Verdict.ShouldBe(Verdict.Accepted);

        run.StepBack().ShouldBeTrue();
        run.LastStep!.Number.ShouldBe(2);
        run.Reset();
        run.Position.ShouldBe(0);
        run.Reset();

        changes.ShouldBe(4);
    }

    [Theory]
    [InlineData("abba")]
    [InlineData("abcab")]
    [InlineData("")]
    public void Append_IsEquivalentToRunningTheWholeWord(string word)
    {
        var dfa = TestAutomata.EvenA();
        var incremental = new DfaRun(dfa, "");
        foreach (var c in word)
        {
            incremental.Append(c);
        }

        var whole = new DfaRun(dfa, word);

        incremental.Steps.ShouldBe(whole.Steps);
        incremental.Input.ShouldBe(word);
        incremental.IsAtEnd.ShouldBeTrue();
        incremental.FinalVerdict.ShouldBe(whole.FinalVerdict);
    }

    [Fact]
    public void RemoveLast_UndoesAppend_EvenPastABlock()
    {
        var dfa = TestAutomata.EvenA();
        var run = new DfaRun(dfa, "ab#ab");

        run.RemoveLast();
        run.RemoveLast();
        run.Steps.Count.ShouldBe(3);
        run.RemoveLast();
        run.Steps.ShouldBe(new DfaRun(dfa, "ab").Steps);

        run.Append('a');
        run.Steps.ShouldBe(new DfaRun(dfa, "aba").Steps);
        run.IsAtEnd.ShouldBeTrue();
    }

    [Fact]
    public void RemoveLast_OnEmptyInput_DoesNothing()
    {
        var run = new DfaRun(TestAutomata.EvenA(), "");
        var changes = 0;
        run.Changed += (_, _) => changes++;

        run.RemoveLast();

        changes.ShouldBe(0);
    }
}
