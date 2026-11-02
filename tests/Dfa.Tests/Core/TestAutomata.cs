namespace Dfa.Tests.Core;

using Dfa.Core.Model;
using Shouldly;

// Small automata built in code for engine tests; the examples 1–6 themselves live in Dfa.Core/Examples.
public static class TestAutomata
{
    // Same language as the even-a example; kept in code so engine tests don't depend on the JSON loader.
    public static Dfa EvenA() => Build("q0", ["q0"],
        ("q0", "a", "q1"), ("q0", "b", "q0"), ("q1", "a", "q0"), ("q1", "b", "q1"));

    // States and simple symbols are added in order of first appearance.
    public static Dfa Build(string? start, string[] finals, params (string From, string Symbols, string To)[] transitions)
    {
        var dfa = new Dfa();
        foreach (var (from, symbols, to) in transitions)
        {
            dfa.AddState(from);
            dfa.AddState(to);
            foreach (var symbol in symbols.Split(','))
            {
                dfa.AddSymbol(symbol);
            }
        }

        foreach (var (from, symbols, to) in transitions)
        {
            dfa.AddTransitions(from, symbols.Split(','), to).Ok.ShouldBeTrue($"{from} -{symbols}-> {to}");
        }

        dfa.SetStart(start);
        foreach (var final in finals)
        {
            dfa.SetFinal(final, true);
        }

        return dfa;
    }
}
