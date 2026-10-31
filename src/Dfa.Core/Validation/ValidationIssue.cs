namespace Dfa.Core.Validation;

public sealed record ValidationIssue(
    Severity Severity,
    string Code,
    string Message,
    string? State = null,
    string? Symbol = null);
