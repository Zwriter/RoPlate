namespace Dfa.Core.Model;

public sealed record SymbolClass(string Name, string Characters)
{
    public bool Contains(char c) => Characters.Contains(c);

    // Builds from a range description: FromRanges("L", "A-Z"), FromRanges("C", "0-9")
    public static SymbolClass FromRanges(string name, string ranges)
    {
        var characters = new List<char>();
        for (var i = 0; i < ranges.Length; i++)
        {
            if (i + 2 < ranges.Length && ranges[i + 1] == '-')
            {
                var (low, high) = ranges[i] <= ranges[i + 2] ? (ranges[i], ranges[i + 2]) : (ranges[i + 2], ranges[i]);
                for (var c = low; c <= high; c++)
                {
                    characters.Add(c);
                }

                i += 2;
            }
            else
            {
                characters.Add(ranges[i]);
            }
        }

        return new SymbolClass(name, new string(characters.Distinct().ToArray()));
    }
}
