namespace Dfa.Tests.Core;

using Dfa.Core.Examples;
using Dfa.Core.Model;
using Dfa.Core.Simulation;
using Shouldly;

public class LanguageGeneratorTests
{
    [Fact]
    public void EvenA_FirstFour_AreShortestFirstInSigmaOrder()
    {
        var dfa = ExampleCatalog.Load("even-a").Dfa;

        LanguageGenerator.FirstAccepted(dfa, 4).ShouldBe(["", "b", "aa", "bb"]);
    }

    [Fact]
    public void EveryGeneratedWord_IsAccepted()
    {
        var dfa = ExampleCatalog.Load("contains-aba").Dfa;

        var words = LanguageGenerator.FirstAccepted(dfa, 20);

        words.Count.ShouldBe(20);
        words.ShouldAllBe(w => dfa.Accepts(w));
        words.Select(w => w.Length).ShouldBeInOrder();
    }

    [Fact]
    public void MaxLength_StopsTheSearch()
    {
        var words = LanguageGenerator.FirstAccepted(TestAutomata.EvenA(), 100, maxLength: 2);

        words.ShouldBe(["", "b", "aa", "bb"]);
    }

    [Fact]
    public void TrapState_IsPruned_AndEmptyLanguageGivesNothing()
    {
        // "starts with a": everything after a leading b is dead.
        var startsA = TestAutomata.Build("q0", ["q1"], ("q0", "a", "q1"), ("q0", "b", "t"), ("q1", "a,b", "q1"), ("t", "a,b", "t"));

        LanguageGenerator.FirstAccepted(startsA, 4).ShouldBe(["a", "aa", "ab", "aaa"]);
        LanguageGenerator.FirstAccepted(TestAutomata.Build("q0", [], ("q0", "a", "q0")), 4).ShouldBeEmpty();
        LanguageGenerator.FirstAccepted(TestAutomata.Build(null, ["q0"], ("q0", "a", "q0")), 4).ShouldBeEmpty();
    }

    [Fact]
    public void SymbolClass_UsesItsFirstCharacter()
    {
        var dfa = TestAutomata.Build("q0", ["q1"], ("q0", "-", "q1"));
        dfa.AddSymbolClass(SymbolClass.FromRanges("C", "0-9"));
        dfa.AddTransition("q0", "C", "q1");

        LanguageGenerator.FirstAccepted(dfa, 5).ShouldBe(["-", "0"]);
    }

    [Fact]
    public void NegativeCount_IsAProgrammingError()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => LanguageGenerator.FirstAccepted(TestAutomata.EvenA(), -1));
    }
}
