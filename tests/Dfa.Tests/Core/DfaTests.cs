namespace Dfa.Tests.Core;

using Dfa.Core.Model;
using Shouldly;

public class DfaTests
{
    // q0 --a--> q1, q1 --a,b--> q0; start q0, final q1
    private static Dfa Sample()
    {
        var dfa = new Dfa();
        dfa.AddState("q0");
        dfa.AddState("q1");
        dfa.AddSymbol("a");
        dfa.AddSymbol("b");
        dfa.AddTransition("q0", "a", "q1");
        dfa.AddTransitions("q1", ["a", "b"], "q0");
        dfa.SetStart("q0");
        dfa.SetFinal("q1", true);
        return dfa;
    }

    [Fact]
    public void AddTransition_Duplicate_IsRefusedWithEd04Message()
    {
        var dfa = Sample();

        var result = dfa.AddTransition("q0", "a", "q0");

        result.Error.ShouldBe("From q0 there is already a transition on 'a' to q1.");
        dfa.Next("q0", "a").ShouldBe("q1");
    }

    [Fact]
    public void AddTransition_SymbolOutsideAlphabet_IsRefused()
    {
        var dfa = Sample();

        dfa.AddTransition("q0", "c", "q1").Error.ShouldBe("Symbol 'c' is not in the alphabet.");
    }

    [Fact]
    public void AddTransition_UnknownState_IsRefused()
    {
        Sample().AddTransition("q0", "b", "q9").Error.ShouldBe("State 'q9' does not exist.");
    }

    [Fact]
    public void AddTransitions_OneBadSymbol_AddsNothing()
    {
        var dfa = Sample();

        var result = dfa.AddTransitions("q0", ["b", "c"], "q0");

        result.Ok.ShouldBeFalse();
        dfa.Next("q0", "b").ShouldBeNull();
    }

    [Fact]
    public void AddTransitions_RepeatedSymbol_IsRefused()
    {
        var dfa = Sample();

        dfa.AddTransitions("q0", ["b", "b"], "q1").Error.ShouldBe("From q0 there is already a transition on 'b' to q1.");
        dfa.Next("q0", "b").ShouldBeNull();
    }

    [Fact]
    public void RemoveState_DeletesTouchingTransitionsStartAndFinal()
    {
        var dfa = Sample();

        dfa.RemoveState("q1").Ok.ShouldBeTrue();

        dfa.States.ShouldBe(["q0"]);
        dfa.Transitions.ShouldBeEmpty();
        dfa.FinalStates.ShouldBeEmpty();
        dfa.RemoveState("q0");
        dfa.StartState.ShouldBeNull();
    }

    [Fact]
    public void RenameState_UpdatesTransitionsStartAndFinals()
    {
        var dfa = Sample();

        dfa.RenameState("q0", "p").Ok.ShouldBeTrue();
        dfa.RenameState("q1", "r").Ok.ShouldBeTrue();

        dfa.States.ShouldBe(["p", "r"]);
        dfa.StartState.ShouldBe("p");
        dfa.FinalStates.ShouldBe(["r"]);
        dfa.Next("p", "a").ShouldBe("r");
        dfa.Next("r", "b").ShouldBe("p");
        dfa.Transitions.Count.ShouldBe(3);
    }

    [Theory]
    [InlineData("q1", "A state named 'q1' already exists.")]
    [InlineData(" ", "The state name cannot be empty.")]
    public void RenameState_InvalidNewName_IsRefused(string newName, string error)
    {
        Sample().RenameState("q0", newName).Error.ShouldBe(error);
    }

    [Theory]
    [InlineData("q0", "A state named 'q0' already exists.")]
    [InlineData("", "The state name cannot be empty.")]
    public void AddState_InvalidName_IsRefused(string name, string error)
    {
        Sample().AddState(name).Error.ShouldBe(error);
    }

    [Theory]
    [InlineData("a", "Symbol 'a' is already in the alphabet.")]
    [InlineData("ab", "A simple symbol must have exactly one character. Use a symbol class for groups.")]
    public void AddSymbol_Invalid_IsRefused(string symbol, string error)
    {
        Sample().AddSymbol(symbol).Error.ShouldBe(error);
    }

    [Fact]
    public void RemoveSymbol_UsedInTransitions_IsRefused()
    {
        var dfa = Sample();

        dfa.RemoveSymbol("a").Error.ShouldBe("Symbol 'a' is used in 2 transitions. Remove them first.");
        dfa.RemoveTransition("q1", "b").Ok.ShouldBeTrue();
        dfa.RemoveSymbol("b").Ok.ShouldBeTrue();
        dfa.Alphabet.ShouldBe(["a"]);
    }

    [Fact]
    public void RemoveTransition_Missing_IsRefused()
    {
        Sample().RemoveTransition("q0", "b").Error.ShouldBe("From q0 there is no transition on 'b'.");
    }

    [Fact]
    public void Edges_GroupSymbolsByFromAndTo()
    {
        Sample().Edges.Select(e => $"{e.From}>{e.To}:{string.Join(",", e.Symbols)}")
            .ShouldBe(["q0>q1:a", "q1>q0:a,b"]);
    }

    [Fact]
    public void Clone_IsIndependent()
    {
        var dfa = Sample();
        var copy = dfa.Clone();

        copy.RemoveState("q1");

        dfa.States.Count.ShouldBe(2);
        dfa.Transitions.Count.ShouldBe(3);
        copy.StartState.ShouldBe("q0");
    }
}
