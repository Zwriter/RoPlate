namespace Dfa.Core.Examples;

using Dfa.Core.Documents;
using Dfa.Core.Serialization;

public static class ExampleCatalog
{
    // The contract's table; an example is listed only once its embedded {id}.dfa.json exists.
    private static readonly ExampleInfo[] _known =
    [
        new("even-a", "Even number of a"),
        new("starts-a", "Starts with a"),
        new("ends-ab", "Ends in ab"),
        new("contains-aba", "Contains aba"),
        new("div3", "Binary numbers divisible by 3"),
        new("length-3", "Length a multiple of 3"),
        new("license-plate", "License plate (RoPlate)"),
    ];

    public static IReadOnlyList<ExampleInfo> All { get; } =
        [.. _known.Where(e => typeof(ExampleCatalog).Assembly.GetManifestResourceInfo(ResourceName(e.Id)) is not null)];

    public static DfaDocument Load(string id)
    {
        using var stream = typeof(ExampleCatalog).Assembly.GetManifestResourceStream(ResourceName(id))
            ?? throw new ArgumentException($"There is no example with id '{id}'.", nameof(id));
        using var reader = new StreamReader(stream);

        var result = DfaJson.Deserialize(reader.ReadToEnd());
        return result.Document
            ?? throw new InvalidOperationException($"Example '{id}' is broken: {string.Join(" ", result.Errors)}");
    }

    private static string ResourceName(string id) => $"{id}.dfa.json";
}
