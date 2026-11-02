namespace Dfa.Core.Serialization;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dfa.Core.Documents;
using Dfa.Core.Model;

public static class DfaJson
{
    // Relaxed escaping keeps non-ASCII characters (accents, '–', 'δ') readable in hand-edited files.
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string Serialize(DfaDocument document)
    {
        var dfa = document.Dfa;
        var root = new JsonObject
        {
            ["format"] = "dfa-simulator",
            ["version"] = 1,
            ["name"] = document.Name,
            ["alphabet"] = Array(dfa.Alphabet.Select(s => (JsonNode?)s)),
            ["symbolClasses"] = Array(dfa.SymbolClasses.Select(c =>
                (JsonNode?)new JsonObject { ["name"] = c.Name, ["chars"] = c.Characters })),
            ["states"] = Array(dfa.States.Select(s => (JsonNode?)s)),
            ["start"] = dfa.StartState,
            ["finals"] = Array(dfa.States.Where(dfa.IsFinal).Select(s => (JsonNode?)s)),
            ["transitions"] = Array(
                from state in dfa.States
                from symbol in dfa.Alphabet
                let to = dfa.Next(state, symbol)
                where to is not null
                select (JsonNode?)new JsonObject { ["from"] = state, ["symbol"] = symbol, ["to"] = to }),
            ["layout"] = Map(document.Layout, dfa.States, p =>
                new JsonObject { ["x"] = (int)Math.Round(p.X), ["y"] = (int)Math.Round(p.Y) }),
            ["descriptions"] = Map(document.Descriptions, dfa.States, d => JsonValue.Create(d)),
            ["tests"] = Array(document.Tests.Select(t =>
                (JsonNode?)new JsonObject { ["word"] = t.Word, ["expected"] = t.Expected })),
        };

        return root.ToJsonString(_options);
    }

    public static LoadResult Deserialize(string json) => DfaJsonReader.Read(json, DfaDocument.CreateEmpty().Name);

    public static LoadResult LoadFile(string path)
    {
        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return new LoadResult(null, [$"Could not open the file: {e.Message}"]);
        }

        var name = Path.GetFileName(path);
        name = name.EndsWith(".dfa.json", StringComparison.OrdinalIgnoreCase) ? name[..^".dfa.json".Length] : name;
        return DfaJsonReader.Read(json, name);
    }

    public static OpResult SaveFile(DfaDocument document, string path)
    {
        // Write next to the target, then swap, so a failed save never truncates the existing file.
        var temp = path + ".tmp";
        try
        {
            File.WriteAllText(temp, Serialize(document));
            File.Move(temp, path, overwrite: true);
            return OpResult.Success;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            if (File.Exists(temp))
            {
                File.Delete(temp);
            }

            return OpResult.Fail($"Could not save the file: {e.Message}");
        }
    }

    private static JsonArray Array(IEnumerable<JsonNode?> items) => new([.. items]);

    private static JsonObject Map<T>(
        IReadOnlyDictionary<string, T> values, IEnumerable<string> order, Func<T, JsonNode> toNode)
    {
        var map = new JsonObject();
        foreach (var state in order.Where(values.ContainsKey))
        {
            map[state] = toNode(values[state]);
        }

        return map;
    }
}
