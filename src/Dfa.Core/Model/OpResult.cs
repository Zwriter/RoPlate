namespace Dfa.Core.Model;

public sealed record OpResult(bool Ok, string? Error = null)
{
    public static OpResult Success { get; } = new(true);

    public static OpResult Fail(string message) => new(false, message);
}
