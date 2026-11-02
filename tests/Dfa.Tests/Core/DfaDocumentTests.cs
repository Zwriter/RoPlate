namespace Dfa.Tests.Core;

using Dfa.Core.Documents;
using Dfa.Core.Model;
using Shouldly;

public class DfaDocumentTests
{
    private readonly DfaDocument _doc = DfaDocument.CreateEmpty();
    private readonly List<DocumentChangeKind> _changes = [];

    public DfaDocumentTests()
    {
        _doc.AddState(new Point2(10, 20));
        _doc.AddState(new Point2(30, 40));
        _doc.AddSymbol("a");
        _doc.AddTransitions("q0", ["a"], "q1");
        _doc.SetDescription("q0", "start here");
        _doc.Changed += (_, e) => _changes.Add(e.Kind);
    }

    [Fact]
    public void CreateEmpty_UsesDefaultName()
    {
        DfaDocument.CreateEmpty().Name.ShouldBe("New automaton");
    }

    [Fact]
    public void AddState_WithoutName_TakesFirstFreeQName()
    {
        _doc.RemoveState("q0");

        _doc.AddState(new Point2(0, 0)).Ok.ShouldBeTrue();
        _doc.AddState(new Point2(0, 0)).Ok.ShouldBeTrue();

        _doc.Dfa.States.ShouldBe(["q1", "q0", "q2"]);
        _changes.ShouldBe([DocumentChangeKind.Structure, DocumentChangeKind.Structure, DocumentChangeKind.Structure]);
    }

    [Fact]
    public void RenameState_MovesPositionAndDescription()
    {
        _doc.RenameState("q0", "start").Ok.ShouldBeTrue();

        _doc.Layout.Keys.ShouldBe(["q1", "start"], ignoreOrder: true);
        _doc.Layout["start"].ShouldBe(new Point2(10, 20));
        _doc.Descriptions["start"].ShouldBe("start here");
        _doc.Descriptions.ShouldNotContainKey("q0");
        _changes.ShouldBe([DocumentChangeKind.Structure]);
    }

    [Fact]
    public void RemoveState_DropsPositionAndDescription()
    {
        _doc.RemoveState("q0").Ok.ShouldBeTrue();

        _doc.Layout.ShouldNotContainKey("q0");
        _doc.Descriptions.ShouldBeEmpty();
    }

    [Fact]
    public void MoveState_RaisesLayoutNotStructure()
    {
        _doc.MoveState("q1", new Point2(5, 5)).Ok.ShouldBeTrue();

        _doc.Layout["q1"].ShouldBe(new Point2(5, 5));
        _changes.ShouldBe([DocumentChangeKind.Layout]);
    }

    [Fact]
    public void SetDescription_EmptyText_RemovesIt()
    {
        _doc.SetDescription("q0", " ").Ok.ShouldBeTrue();

        _doc.Descriptions.ShouldBeEmpty();
        _changes.ShouldBe([DocumentChangeKind.Description]);
    }

    [Fact]
    public void FailedOperations_ChangeNothingAndRaiseNothing()
    {
        _doc.AddState("q0", new Point2(99, 99)).Error.ShouldBe("A state named 'q0' already exists.");
        _doc.MoveState("x", new Point2(0, 0)).Error.ShouldBe("State 'x' does not exist.");
        _doc.SetDescription("x", "?").Error.ShouldBe("State 'x' does not exist.");
        _doc.AddTransitions("q1", ["b"], "q0").Ok.ShouldBeFalse();
        _doc.RemoveEdge("q1", "q0").Error.ShouldBe("From q1 there is no transition to q0.");
        _doc.RemoveEdge("q1", "x").Error.ShouldBe("State 'x' does not exist.");

        _doc.Layout["q0"].ShouldBe(new Point2(10, 20));
        _changes.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveEdge_RemovesAllItsSymbolsInOneChange()
    {
        _doc.AddSymbol("b");
        _doc.AddTransitions("q1", ["a", "b"], "q0");
        _changes.Clear();

        _doc.RemoveEdge("q1", "q0").Ok.ShouldBeTrue();

        _doc.Dfa.Next("q1", "a").ShouldBeNull();
        _doc.Dfa.Next("q1", "b").ShouldBeNull();
        _doc.Dfa.Next("q0", "a").ShouldBe("q1");
        _changes.ShouldBe([DocumentChangeKind.Structure]);
    }

    [Fact]
    public void SetTests_ReplacesTheList()
    {
        _doc.SetTests([new TestCase("a", true)]);
        _doc.SetTests([new TestCase("", false), new TestCase("aa", true)]);

        _doc.Tests.ShouldBe([new TestCase("", false), new TestCase("aa", true)]);
        _changes.ShouldBe([DocumentChangeKind.Tests, DocumentChangeKind.Tests]);
    }

    [Fact]
    public void Clone_IsIndependentAndSilent()
    {
        var copy = _doc.Clone();

        copy.RemoveState("q0");
        copy.SetTests([new TestCase("a", true)]);

        _doc.Dfa.States.ShouldBe(["q0", "q1"]);
        _doc.Layout.Count.ShouldBe(2);
        _doc.Tests.ShouldBeEmpty();
        _changes.ShouldBeEmpty();
    }

    [Fact]
    public void RestoreFrom_CopiesSnapshotAndRaisesReset()
    {
        var snapshot = _doc.Clone();
        _doc.Name = "edited";
        _doc.RemoveState("q1");
        _changes.Clear();

        _doc.RestoreFrom(snapshot);
        snapshot.RemoveState("q0");

        _doc.Name.ShouldBe("New automaton");
        _doc.Dfa.States.ShouldBe(["q0", "q1"]);
        _doc.Dfa.Next("q0", "a").ShouldBe("q1");
        _doc.Layout["q1"].ShouldBe(new Point2(30, 40));
        _doc.Descriptions["q0"].ShouldBe("start here");
        _changes.ShouldBe([DocumentChangeKind.Reset]);
    }
}
