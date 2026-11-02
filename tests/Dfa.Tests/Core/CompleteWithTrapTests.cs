namespace Dfa.Tests.Core;

using Dfa.Core.Documents;
using Dfa.Core.Model;
using Dfa.Core.Simulation;
using Dfa.Core.Validation;
using Shouldly;

public class CompleteWithTrapTests
{
    private static readonly string[] _words = ["", "a", "b", "ab", "ba", "aab", "bba", "abab"];

    private readonly DfaDocument _doc = DfaDocument.CreateEmpty();
    private readonly List<DocumentChangeKind> _changes = [];

    // "starts with a", without its trap: q0 has no transition on 'b'.
    public CompleteWithTrapTests()
    {
        _doc.AddState(new Point2(0, 0));
        _doc.AddState(new Point2(100, 0));
        _doc.AddSymbol("a");
        _doc.AddSymbol("b");
        _doc.AddTransitions("q0", ["a"], "q1");
        _doc.AddTransitions("q1", ["a", "b"], "q1");
        _doc.SetStart("q0");
        _doc.SetFinal("q1", true);
        _doc.Changed += (_, e) => _changes.Add(e.Kind);
    }

    [Fact]
    public void CompleteWithTrap_FillsEveryMissingTransition_IncludingItsOwnLoops()
    {
        _doc.CompleteWithTrap(new Point2(50, 80)).Ok.ShouldBeTrue();

        _doc.Dfa.Next("q0", "b").ShouldBe("trap");
        _doc.Dfa.Next("trap", "a").ShouldBe("trap");
        _doc.Dfa.Next("trap", "b").ShouldBe("trap");
        _doc.Dfa.FinalStates.ShouldNotContain("trap");
        _doc.Layout["trap"].ShouldBe(new Point2(50, 80));
        DfaValidator.Validate(_doc.Dfa).ShouldNotContain(i => i.Code == "MISSING_TRANSITION");
        _changes.ShouldBe([DocumentChangeKind.Structure]);
    }

    [Fact]
    public void CompleteWithTrap_KeepsTheLanguage()
    {
        var before = _words.Select(w => _doc.Dfa.Accepts(w)).ToList();

        _doc.CompleteWithTrap(new Point2(50, 80), "sink").Ok.ShouldBeTrue();

        _words.Select(w => _doc.Dfa.Accepts(w)).ShouldBe(before);
        _doc.Dfa.States.ShouldContain("sink");
    }

    [Fact]
    public void CompleteWithTrap_OnCompleteAutomaton_Fails()
    {
        _doc.CompleteWithTrap(new Point2(50, 80));
        _changes.Clear();

        var result = _doc.CompleteWithTrap(new Point2(0, 80), "other");

        result.Error.ShouldBe("The automaton is already complete.");
        _doc.Dfa.States.ShouldNotContain("other");
        _changes.ShouldBeEmpty();
    }

    [Fact]
    public void CompleteWithTrap_TakenName_FailsAndChangesNothing()
    {
        var result = _doc.CompleteWithTrap(new Point2(50, 80), "q1");

        result.Error.ShouldBe("A state named 'q1' already exists.");
        _doc.Dfa.Next("q0", "b").ShouldBeNull();
        _changes.ShouldBeEmpty();
    }
}
