namespace Dfa.Core.Simulation;

public sealed record DfaStep(
    int Number,
    int Position,
    char Character,
    string? Symbol,
    string From,
    string? To,
    StepOutcome Outcome);
