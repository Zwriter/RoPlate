namespace Dfa.Tests.Core;

using Dfa.Core.Documents;
using Dfa.Core.Model;
using Dfa.Core.Serialization;
using Shouldly;

public class DfaJsonTests
{
    private const string Header = "\"format\": \"dfa-simulator\", \"version\": 1";

    // The example from contract 2, section 1.
    private const string EvenA = """
        {
          "format": "dfa-simulator",
          "version": 1,
          "name": "Even number of a",
          "alphabet": ["a", "b"],
          "symbolClasses": [],
          "states": ["q0", "q1"],
          "start": "q0",
          "finals": ["q0"],
          "transitions": [
            { "from": "q0", "symbol": "a", "to": "q1" },
            { "from": "q0", "symbol": "b", "to": "q0" },
            { "from": "q1", "symbol": "a", "to": "q0" },
            { "from": "q1", "symbol": "b", "to": "q1" }
          ],
          "layout": { "q0": { "x": 210, "y": 120 }, "q1": { "x": 430, "y": 120 } },
          "descriptions": { "q0": "seen an even number of a", "q1": "seen an odd number of a" },
          "tests": [ { "word": "", "expected": true }, { "word": "bab", "expected": false } ],
          "someFutureField": 42
        }
        """;

    [Fact]
    public void Deserialize_ContractExample_LoadsEverything()
    {
        var result = DfaJson.Deserialize(EvenA);

        result.Errors.ShouldBeEmpty();
        var doc = result.Document.ShouldNotBeNull();
        doc.Name.ShouldBe("Even number of a");
        doc.Dfa.States.ShouldBe(["q0", "q1"]);
        doc.Dfa.StartState.ShouldBe("q0");
        doc.Dfa.FinalStates.ShouldBe(["q0"]);
        doc.Dfa.Next("q1", "a").ShouldBe("q0");
        doc.Layout["q1"].ShouldBe(new Point2(430, 120));
        doc.Descriptions["q1"].ShouldBe("seen an odd number of a");
        doc.Tests.ShouldBe([new TestCase("", true), new TestCase("bab", false)]);
    }

    [Fact]
    public void RoundTrip_GivesAnEquivalentDocument()
    {
        var doc = DfaJson.Deserialize(EvenA).Document!;
        doc.AddSymbolClass(SymbolClass.FromRanges("C", "0-9"));
        doc.AddTransitions("q0", ["C"], "q0");
        doc.MoveState("q0", new Point2(10.6, 20.2));

        var json = DfaJson.Serialize(doc);
        var copy = DfaJson.Deserialize(json).Document.ShouldNotBeNull();

        DfaJson.Serialize(copy).ShouldBe(json);
        copy.Dfa.SymbolOf('7').ShouldBe("C");
        copy.Layout["q0"].ShouldBe(new Point2(11, 20));
    }

    [Fact]
    public void Serialize_UsesContractFieldOrderAndTwoSpaces()
    {
        var json = DfaJson.Serialize(DfaJson.Deserialize(EvenA).Document!);

        string[] fields = ["format", "version", "name", "alphabet", "symbolClasses", "states", "start", "finals",
            "transitions", "layout", "descriptions", "tests"];
        fields.Select(f => json.IndexOf($"\"{f}\"", StringComparison.Ordinal)).ShouldBeInOrder();
        json.ShouldContain("\n  \"version\": 1");
        json.ShouldNotContain("someFutureField");
    }

    [Fact]
    public void StatesWithoutLayout_GetDistinctAutomaticPositions()
    {
        var doc = DfaJson.Deserialize($$"""{ {{Header}}, "alphabet": ["a"], "states": ["a1", "a2", "a3"] }""")
            .Document.ShouldNotBeNull();

        doc.Layout.Count.ShouldBe(3);
        doc.Layout.Values.Distinct().Count().ShouldBe(3);
        doc.Name.ShouldBe("New automaton");
    }

    [Fact]
    public void LayoutAndDescriptionsForMissingStates_AreIgnored()
    {
        var result = DfaJson.Deserialize($$"""
            { {{Header}}, "alphabet": ["a"], "states": ["q0"],
              "layout": { "ghost": { "x": 1, "y": 2 } }, "descriptions": { "ghost": "boo" } }
            """);

        result.Errors.ShouldBeEmpty();
        result.Document!.Descriptions.ShouldBeEmpty();
        result.Document.Layout.Keys.ShouldBe(["q0"]);
    }

    [Theory]
    [InlineData("{ \"format\": ", "The file is not valid JSON (line 1).")]
    [InlineData("", "The file is not valid JSON (line 1).")]
    [InlineData("{\n\"format\": \"dfa-simulator\",\n\"version\": 1,,\n}", "The file is not valid JSON (line 3).")]
    [InlineData("{ \"format\": \"dfa-simulator\", \"format\": \"x\" }", "The file is not valid JSON (line 1).")]
    [InlineData("[1, 2]", "The file is not an automaton (missing \"format\": \"dfa-simulator\").")]
    [InlineData("{ \"version\": 1 }", "The file is not an automaton (missing \"format\": \"dfa-simulator\").")]
    [InlineData("{ \"format\": \"other\" }", "The file is not an automaton (missing \"format\": \"dfa-simulator\").")]
    [InlineData("{ \"format\": \"dfa-simulator\" }", "Field 'version' is missing or has the wrong type.")]
    [InlineData("{ \"format\": \"dfa-simulator\", \"version\": 2 }", "The file was created with a newer version (2).")]
    public void Deserialize_BrokenFile_ReportsSingleError(string json, string error)
    {
        var result = DfaJson.Deserialize(json);

        result.Ok.ShouldBeFalse();
        result.Document.ShouldBeNull();
        result.Errors.ShouldBe([error]);
    }

    [Theory]
    [InlineData("\"alphabet\": [\"a\"], \"states\": [\"q0\", \"q0\"]", "State 'q0' appears twice.")]
    [InlineData("\"alphabet\": [\"a\"], \"states\": [\"q0\"], \"transitions\": [{ \"from\": \"q0\", \"symbol\": \"a\", \"to\": \"x\" }]",
        "Transition q0 –a→ x uses a state that does not exist.")]
    [InlineData("\"alphabet\": [\"a\"], \"states\": [\"q0\"], \"transitions\": [{ \"from\": \"q0\", \"symbol\": \"b\", \"to\": \"q0\" }]",
        "Symbol 'b' is not in the alphabet.")]
    [InlineData("\"alphabet\": [\"a\"], \"states\": [\"q0\"], \"transitions\": [{ \"from\": \"q0\", \"symbol\": \"a\", \"to\": \"q0\" }, { \"from\": \"q0\", \"symbol\": \"a\", \"to\": \"q0\" }]",
        "From q0 there is already a transition on 'a'.")]
    [InlineData("\"alphabet\": [\"a\"], \"states\": [\"q0\"], \"start\": \"x\"", "State 'x' does not exist.")]
    [InlineData("\"alphabet\": [\"a\"], \"states\": [\"q0\"], \"finals\": [\"x\"]", "State 'x' does not exist.")]
    [InlineData("\"alphabet\": [\"ab\"], \"states\": []",
        "A simple symbol must have exactly one character. Use a symbol class for groups.")]
    [InlineData("\"alphabet\": [\"L\", \"V\"], \"symbolClasses\": [{ \"name\": \"L\", \"chars\": \"AB\" }, { \"name\": \"V\", \"chars\": \"A\" }], \"states\": []",
        "Character 'A' already belongs to class 'L'.")]
    [InlineData("\"alphabet\": [], \"symbolClasses\": [{ \"name\": \"L\", \"chars\": \"AB\" }], \"states\": []",
        "Symbol 'L' is not in the alphabet.")]
    [InlineData("\"states\": []", "Field 'alphabet' is missing or has the wrong type.")]
    [InlineData("\"alphabet\": [\"a\"], \"states\": \"q0\"", "Field 'states' is missing or has the wrong type.")]
    [InlineData("\"alphabet\": [\"a\"], \"states\": [\"q0\", 7]", "Field 'states[1]' is missing or has the wrong type.")]
    [InlineData("\"alphabet\": [\"a\"], \"states\": [\"q0\"], \"layout\": { \"q0\": { \"x\": \"left\", \"y\": 0 } }",
        "Field 'layout.q0.x' is missing or has the wrong type.")]
    [InlineData("\"alphabet\": [\"a\"], \"states\": [\"q0\"], \"tests\": [{ \"word\": \"a\" }]",
        "Field 'tests[0].expected' is missing or has the wrong type.")]
    public void Deserialize_InvalidContent_ReportsError(string body, string error)
    {
        var result = DfaJson.Deserialize($"{{ {Header}, {body} }}");

        result.Document.ShouldBeNull();
        result.Errors.ShouldBe([error]);
    }

    [Fact]
    public void Deserialize_ReportsAllErrorsAtOnce()
    {
        var result = DfaJson.Deserialize($$"""
            { {{Header}}, "alphabet": ["a"], "states": ["q0", "q0"], "start": "x", "finals": ["y"] }
            """);

        result.Errors.ShouldBe(["State 'q0' appears twice.", "State 'x' does not exist.", "State 'y' does not exist."]);
    }

    [Fact]
    public void Deserialize_EveryTruncationOfAValidFile_NeverThrows()
    {
        for (var length = 0; length < EvenA.Length; length++)
        {
            Should.NotThrow(() => DfaJson.Deserialize(EvenA[..length]));
        }
    }

    [Fact]
    public void SaveFileAndLoadFile_RoundTripThroughDisk()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(dir.FullName, "my-automaton.dfa.json");
            var doc = DfaJson.Deserialize(EvenA).Document!;
            doc.Name = "";

            DfaJson.SaveFile(doc, path).Ok.ShouldBeTrue();
            File.ReadAllText(path).ShouldBe(DfaJson.Serialize(doc));
            Directory.GetFiles(dir.FullName).Length.ShouldBe(1);

            var withoutName = File.ReadAllText(path).Replace("\"name\": \"\",", "");
            File.WriteAllText(path, withoutName);
            DfaJson.LoadFile(path).Document!.Name.ShouldBe("my-automaton");
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public void LoadFile_MissingFile_ReportsErrorInsteadOfThrowing()
    {
        var result = DfaJson.LoadFile(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".dfa.json"));

        result.Ok.ShouldBeFalse();
        result.Errors.ShouldHaveSingleItem().ShouldStartWith("Could not open the file: ");
    }

    [Fact]
    public void SaveFile_BadPath_FailsWithMessage()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "missing-dir", "a.dfa.json");

        DfaJson.SaveFile(DfaDocument.CreateEmpty(), path).Error.ShouldNotBeNull().ShouldStartWith("Could not save the file: ");
    }
}
