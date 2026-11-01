namespace Dfa.Core.Model;

// One arrow in the drawing: all symbols leading from From to To
public sealed record Edge(string From, string To, IReadOnlyList<string> Symbols);
