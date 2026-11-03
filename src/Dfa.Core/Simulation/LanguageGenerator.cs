namespace Dfa.Core.Simulation;

using Dfa.Core.Model;

public static class LanguageGenerator
{
    // The first n accepted words, by length; for a class, its first character is used.
    // Breadth-first, one length at a time, symbols in Σ order: the words come out shortest first, then in Σ order.
    // shortcut: a level can hold up to |Σ|^length words when most states can still accept; fine for maxLength 12
    // on the examples, upgrade to a counting search if larger alphabets or lengths are needed.
    public static IReadOnlyList<string> FirstAccepted(Dfa dfa, int count, int maxLength = 12)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        var words = new List<string>();
        var live = LiveStates(dfa);
        if (dfa.StartState is not { } start || !live.Contains(start))
        {
            return words;
        }

        var letters = dfa.Alphabet.Select(symbol => (Symbol: symbol, Letter: LetterOf(dfa, symbol)))
            .Where(s => s.Letter is not null)
            .ToList();
        List<(string State, string Word)> level = [(start, "")];
        for (var length = 0; level.Count > 0 && words.Count < count; length++)
        {
            words.AddRange(level.Where(n => dfa.FinalStates.Contains(n.State))
                .Select(n => n.Word)
                .Take(count - words.Count));
            if (length == maxLength)
            {
                break;
            }

            level = [.. level.SelectMany(n => letters.Select(s => (State: dfa.Next(n.State, s.Symbol), Word: n.Word + s.Letter)))
                .Where(n => n.State is not null && live.Contains(n.State))
                .Select(n => (n.State!, n.Word))];
        }

        return words;
    }

    // States from which a final state can still be reached; words that leave them can never be accepted.
    private static HashSet<string> LiveStates(Dfa dfa)
    {
        var live = dfa.FinalStates.ToHashSet();
        while (dfa.Edges.Count(e => live.Contains(e.To) && live.Add(e.From)) > 0)
        {
        }

        return live;
    }

    // A simple symbol is its own character; an empty class has none, so it never appears in a word.
    private static char? LetterOf(Dfa dfa, string symbol)
    {
        var symbolClass = dfa.SymbolClasses.FirstOrDefault(c => c.Name == symbol);
        return symbolClass is null ? symbol[0] : symbolClass.Characters.Length > 0 ? symbolClass.Characters[0] : null;
    }
}
