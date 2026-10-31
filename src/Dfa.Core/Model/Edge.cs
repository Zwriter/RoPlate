namespace Dfa.Core.Model;

// O săgeată de pe desen: toate simbolurile care duc din From în To
public sealed record Edge(string From, string To, IReadOnlyList<string> Symbols);
