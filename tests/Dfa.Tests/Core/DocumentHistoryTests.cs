namespace Dfa.Tests.Core;

using Dfa.Core.Documents;
using Dfa.Core.Model;
using Shouldly;

public class DocumentHistoryTests
{
    private static readonly Point2 _origin = new(0, 0);

    private readonly DfaDocument _doc = DfaDocument.CreateEmpty();

    [Fact]
    public void ThreeStates_UndoThrice_ThenRedoThrice()
    {
        var history = new DocumentHistory(_doc);
        _doc.AddState(_origin);
        _doc.AddState(_origin);
        _doc.AddState(_origin);

        history.Undo();
        history.Undo();
        history.Undo();

        _doc.Dfa.States.ShouldBeEmpty();
        _doc.Layout.ShouldBeEmpty();
        history.CanUndo.ShouldBeFalse();
        history.CanRedo.ShouldBeTrue();

        history.Redo();
        history.Redo();
        history.Redo();

        _doc.Dfa.States.ShouldBe(["q0", "q1", "q2"]);
        history.CanRedo.ShouldBeFalse();
    }

    [Fact]
    public void NewEditAfterUndo_ClearsRedo()
    {
        var history = new DocumentHistory(_doc);
        _doc.AddState(_origin);
        _doc.AddState(_origin);
        history.Undo();

        _doc.AddState("x", _origin);

        history.CanRedo.ShouldBeFalse();
        _doc.Dfa.States.ShouldBe(["q0", "x"]);
        history.Undo();
        _doc.Dfa.States.ShouldBe(["q0"]);
    }

    [Fact]
    public void Capacity_KeepsOnlyTheNewestSteps()
    {
        var history = new DocumentHistory(_doc, capacity: 2);
        _doc.AddState(_origin);
        _doc.AddState(_origin);
        _doc.AddState(_origin);

        history.Undo();
        history.Undo();
        history.CanUndo.ShouldBeFalse();
        history.Undo();

        _doc.Dfa.States.ShouldBe(["q0"]);
    }

    [Fact]
    public void EveryKindOfEdit_IsUndoable()
    {
        _doc.AddState(_origin);
        var history = new DocumentHistory(_doc);
        _doc.MoveState("q0", new Point2(5, 5));
        _doc.SetDescription("q0", "hello");
        _doc.SetTests([new TestCase("a", true)]);

        history.Undo();
        _doc.Tests.ShouldBeEmpty();
        history.Undo();
        _doc.Descriptions.ShouldBeEmpty();
        history.Undo();
        _doc.Layout["q0"].ShouldBe(_origin);
    }

    [Fact]
    public void FailedEdit_AddsNoStep()
    {
        var history = new DocumentHistory(_doc);

        _doc.RemoveState("missing").Ok.ShouldBeFalse();

        history.CanUndo.ShouldBeFalse();
    }

    [Fact]
    public void Clear_ForgetsEverythingAndKeepsTheDocument()
    {
        var history = new DocumentHistory(_doc);
        _doc.AddState(_origin);
        _doc.AddState(_origin);
        history.Undo();

        history.Clear();

        history.CanUndo.ShouldBeFalse();
        history.CanRedo.ShouldBeFalse();
        _doc.Dfa.States.ShouldBe(["q0"]);
        _doc.AddState(_origin);
        history.Undo();
        _doc.Dfa.States.ShouldBe(["q0"]);
    }

    [Fact]
    public void Changed_IsRaisedForEveryStep_AndUndoWithNothingIsSilent()
    {
        var history = new DocumentHistory(_doc);
        var changes = 0;
        history.Changed += (_, _) => changes++;

        history.Undo();
        history.Redo();
        _doc.AddState(_origin);
        history.Undo();
        history.Redo();
        history.Clear();

        changes.ShouldBe(4);
    }

    [Fact]
    public void Capacity_MustBePositive()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new DocumentHistory(_doc, capacity: 0));
    }
}
