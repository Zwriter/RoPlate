namespace Dfa.Core.Serialization;

using System.Text.Json;
using System.Text.Json.Nodes;
using Dfa.Core.Documents;
using Dfa.Core.Model;


internal sealed class DfaJsonReader
{
    private const string NotAnAutomaton = "The file is not an automaton (missing \"format\": \"dfa-simulator\").";
    private const int KnownVersion = 1;

    private readonly List<string> _errors = [];
    private readonly JsonObject _root;
    private readonly DfaDocument _doc;

    private DfaJsonReader(JsonObject root, string name)
    {
        _root = root;
        _doc = DfaDocument.CreateEmpty(name);
    }

    public static LoadResult Read(string json, string defaultName)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions { AllowDuplicateProperties = false });
        }
        catch (JsonException e)
        {
            return Failed($"The file is not valid JSON (line {(e.LineNumber ?? 0) + 1}).");
        }

        if (node is not JsonObject root
            || !(root["format"] is JsonValue format && format.TryGetValue(out string? f) && f == "dfa-simulator"))
        {
            return Failed(NotAnAutomaton);
        }

        if (!(root["version"] is JsonValue v && v.TryGetValue(out int version)))
        {
            return Failed(WrongField("version"));
        }

        if (version > KnownVersion)
        {
            return Failed($"The file was created with a newer version ({version}).");
        }

        var reader = new DfaJsonReader(root, defaultName);
        var name = reader.Text(root["name"], "name");
        if (name is not null)
        {
            reader._doc.Name = name;
        }

        return reader.ReadBody();
    }

    private LoadResult ReadBody()
    {
        ReadAlphabet();
        var states = ReadStates();
        if (Text(_root["start"], "start") is { } start)
        {
            Forward(_doc.SetStart(start));
        }

        foreach (var final in Strings("finals", required: false))
        {
            Forward(_doc.SetFinal(final, true));
        }

        ReadTransitions();
        ReadDescriptions(states);
        ReadTests();

        return _errors.Count > 0 ? new LoadResult(null, _errors) : new LoadResult(_doc, []);
    }

    private void ReadAlphabet()
    {
        var classes = new Dictionary<string, SymbolClass>();
        foreach (var (i, node) in Items("symbolClasses", required: false))
        {
            if (Object(node, $"symbolClasses[{i}]") is { } obj
                && Text(obj["name"], $"symbolClasses[{i}].name", required: true) is { } name
                && Text(obj["chars"], $"symbolClasses[{i}].chars", required: true) is { } chars)
            {
                classes[name] = new SymbolClass(name, chars);
            }
        }

        var alphabet = Strings("alphabet", required: true);
        foreach (var symbol in alphabet)
        {
            Forward(classes.TryGetValue(symbol, out var symbolClass)
                ? _doc.AddSymbolClass(symbolClass)
                : _doc.AddSymbol(symbol));
        }

        foreach (var name in classes.Keys.Where(n => !alphabet.Contains(n)))
        {
            _errors.Add($"Symbol '{name}' is not in the alphabet.");
        }
    }

    private HashSet<string> ReadStates()
    {
        var layout = new Dictionary<string, Point2>();
        foreach (var (state, node) in Object(_root["layout"], "layout") ?? [])
        {
            if (Object(node, $"layout.{state}") is { } point
                && Number(point["x"], $"layout.{state}.x") is { } x
                && Number(point["y"], $"layout.{state}.y") is { } y)
            {
                layout[state] = new Point2(x, y);
            }
        }

        var states = Strings("states", required: true);
        var seen = new HashSet<string>();
        for (var i = 0; i < states.Count; i++)
        {
            if (!seen.Add(states[i]))
            {
                _errors.Add($"State '{states[i]}' appears twice.");
                continue;
            }

            Forward(_doc.AddState(states[i], layout.GetValueOrDefault(states[i], OnCircle(i, states.Count))));
        }

        return seen;
    }

    private void ReadTransitions()
    {
        var dfa = _doc.Dfa;
        foreach (var (i, node) in Items("transitions", required: false))
        {
            if (Object(node, $"transitions[{i}]") is not { } obj
                || Text(obj["from"], $"transitions[{i}].from", required: true) is not { } from
                || Text(obj["symbol"], $"transitions[{i}].symbol", required: true) is not { } symbol
                || Text(obj["to"], $"transitions[{i}].to", required: true) is not { } to)
            {
                continue;
            }

            if (!dfa.States.Contains(from) || !dfa.States.Contains(to))
            {
                _errors.Add($"Transition {from} –{symbol}→ {to} uses a state that does not exist.");
            }
            else if (!dfa.Alphabet.Contains(symbol))
            {
                _errors.Add($"Symbol '{symbol}' is not in the alphabet.");
            }
            else if (dfa.Next(from, symbol) is not null)
            {
                _errors.Add($"From {from} there is already a transition on '{symbol}'.");
            }
            else
            {
                Forward(_doc.AddTransitions(from, [symbol], to));
            }
        }
    }

    private void ReadDescriptions(HashSet<string> states)
    {
        foreach (var (state, node) in Object(_root["descriptions"], "descriptions") ?? [])
        {
            if (states.Contains(state) && Text(node, $"descriptions.{state}") is { } text)
            {
                Forward(_doc.SetDescription(state, text));
            }
        }
    }

    private void ReadTests()
    {
        var tests = new List<TestCase>();
        foreach (var (i, node) in Items("tests", required: false))
        {
            if (Object(node, $"tests[{i}]") is { } obj
                && Text(obj["word"], $"tests[{i}].word", required: true) is { } word
                && Bool(obj["expected"], $"tests[{i}].expected") is { } expected)
            {
                tests.Add(new TestCase(word, expected));
            }
        }

        _doc.SetTests(tests);
    }

    private static Point2 OnCircle(int index, int count)
    {
        var angle = 2 * Math.PI * index / count;
        return new Point2(400 + (200 * Math.Cos(angle)), 300 + (200 * Math.Sin(angle)));
    }

    private IEnumerable<(int Index, JsonNode? Node)> Items(string field, bool required)
    {
        var node = _root[field];
        if (node is JsonArray array)
        {
            return array.Select((item, i) => (i, item));
        }

        if (node is not null || required)
        {
            _errors.Add(WrongField(field));
        }

        return [];
    }

    private List<string> Strings(string field, bool required) =>
        [.. Items(field, required).Select(item => Text(item.Node, $"{field}[{item.Index}]", required: true))
            .OfType<string>()];

    private string? Text(JsonNode? node, string field, bool required = false)
    {
        if (node is JsonValue v && v.TryGetValue(out string? text))
        {
            return text;
        }

        if (node is not null || required)
        {
            _errors.Add(WrongField(field));
        }

        return null;
    }

    private double? Number(JsonNode? node, string field) =>
        node is JsonValue v && v.TryGetValue(out double d) ? d : Wrong<double>(field);

    private bool? Bool(JsonNode? node, string field) =>
        node is JsonValue v && v.TryGetValue(out bool b) ? b : Wrong<bool>(field);

    private JsonObject? Object(JsonNode? node, string field)
    {
        if (node is null or JsonObject)
        {
            return node as JsonObject;
        }

        _errors.Add(WrongField(field));
        return null;
    }

    private T? Wrong<T>(string field)
        where T : struct
    {
        _errors.Add(WrongField(field));
        return null;
    }

    private void Forward(OpResult result)
    {
        if (!result.Ok)
        {
            _errors.Add(result.Error!);
        }
    }

    private static string WrongField(string field) => $"Field '{field}' is missing or has the wrong type.";

    private static LoadResult Failed(string error) => new(null, [error]);
}
