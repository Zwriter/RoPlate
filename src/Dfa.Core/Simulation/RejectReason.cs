namespace Dfa.Core.Simulation;

public enum RejectReason
{
    None,
    NoStartState,
    EndedInNonFinal,
    NoTransition,
    NotInAlphabet,
}
