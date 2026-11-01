namespace Dfa.Core.Model;

public sealed record SymbolClass(string Name, string Characters)
{
    public bool Contains(char c) => throw new NotImplementedException();

    // Builds from a range description: FromRanges("L", "A-Z"), FromRanges("C", "0-9")
    public static SymbolClass FromRanges(string name, string ranges) => throw new NotImplementedException();
}
