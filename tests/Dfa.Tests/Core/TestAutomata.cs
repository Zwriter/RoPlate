namespace Dfa.Tests.Core;

using Dfa.Core.Model;
using Shouldly;

// Examples 1–6 built in code until the JSON examples exist (C-07).
public static class TestAutomata
{
    public static Dfa EvenA() => Build("q0", ["q0"],
        ("q0", "a", "q1"), ("q0", "b", "q0"), ("q1", "a", "q0"), ("q1", "b", "q1"));

    public static Dfa StartsA() => Build("q0", ["q1"],
        ("q0", "a", "q1"), ("q0", "b", "trap"), ("q1", "a,b", "q1"), ("trap", "a,b", "trap"));

    public static Dfa EndsAb() => Build("s0", ["s2"],
        ("s0", "a", "s1"), ("s0", "b", "s0"), ("s1", "a", "s1"), ("s1", "b", "s2"), ("s2", "a", "s1"), ("s2", "b", "s0"));

    public static Dfa ContainsAba() => Build("c0", ["c3"],
        ("c0", "a", "c1"), ("c0", "b", "c0"), ("c1", "a", "c1"), ("c1", "b", "c2"),
        ("c2", "a", "c3"), ("c2", "b", "c0"), ("c3", "a,b", "c3"));

    // r = value mod 3; reading digit d moves r to (2r + d) mod 3
    public static Dfa Div3() => Build("r0", ["r0"],
        ("r0", "0", "r0"), ("r0", "1", "r1"), ("r1", "0", "r2"), ("r1", "1", "r0"), ("r2", "0", "r1"), ("r2", "1", "r2"));

    public static Dfa Length3() => Build("l0", ["l0"],
        ("l0", "a,b", "l1"), ("l1", "a,b", "l2"), ("l2", "a,b", "l0"));

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
