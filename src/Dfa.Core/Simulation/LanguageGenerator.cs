namespace Dfa.Core.Simulation;

using Dfa.Core.Model;

public static class LanguageGenerator
{
    // The first n accepted words, by length; for a class, its first character is used.
    public static IReadOnlyList<string> FirstAccepted(Dfa dfa, int count, int maxLength = 12) =>
        throw new NotImplementedException();
}
