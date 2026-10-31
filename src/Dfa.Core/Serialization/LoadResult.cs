namespace Dfa.Core.Serialization;

using Dfa.Core.Documents;

public sealed record LoadResult(DfaDocument? Document, IReadOnlyList<string> Errors)
{
    public bool Ok => Document is not null;
}
