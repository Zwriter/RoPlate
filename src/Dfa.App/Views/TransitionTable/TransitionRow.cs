namespace Dfa.App.Views.TransitionTable;

using System.Collections.Generic;

// One row of the δ table: the state label (with → and *) and one target per symbol of Σ, null when missing.
public sealed record TransitionRow(string Label, IReadOnlyList<string?> Targets);
