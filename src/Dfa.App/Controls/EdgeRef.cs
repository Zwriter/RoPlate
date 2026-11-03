namespace Dfa.App.Controls;

// one arrow = one (From, To) pair, like Dfa.Edges
public sealed record EdgeRef(string From, string To);
