namespace Dfa.Tests.App;

using Dfa.App.Services;
using Dfa.App.Views.TransitionTable;
using Dfa.Core.Documents;
using Dfa.Core.Model;
using Shouldly;

public class TransitionTableViewModelTests
{
    private readonly DocumentSession _session = new();
    private readonly TransitionTableViewModel _table;

    public TransitionTableViewModelTests()
    {
        _table = new TransitionTableViewModel(_session);
        var doc = _session.Document;
        doc.AddState(new Point2(0, 0));
        doc.AddState(new Point2(100, 0));
        doc.AddSymbol("a");
        doc.AddSymbol("b");
        doc.AddTransitions("q0", ["a"], "q1");
        doc.SetStart("q0");
        doc.SetFinal("q1", true);
    }

    [Fact]
    public void Rows_AreStatesWithMarks_ColumnsAreSigma_MissingCellsAreNull()
    {
        _table.Symbols.ShouldBe(["a", "b"]);
        _table.Rows.Select(r => r.Label).ShouldBe(["→q0", "*q1"]);
        _table.Rows[0].Targets.ShouldBe(["q1", null]);
        _table.Rows[1].Targets.ShouldBe([null, null]);
    }

    [Fact]
    public void StartAndFinal_OnTheSameState_ShowBothMarks()
    {
        _session.Document.SetFinal("q0", true);

        _table.Rows[0].Label.ShouldBe("→*q0");
    }

    [Fact]
    public void Edit_AndUndo_UpdateTheTable()
    {
        _session.Document.AddTransitions("q1", ["a", "b"], "q1");
        _table.Rows[1].Targets.ShouldBe(["q1", "q1"]);

        _session.History.Undo();

        _table.Rows[1].Targets.ShouldBe([null, null]);
    }

    [Fact]
    public void Replace_RebuildsTheTable()
    {
        _session.Replace(DfaDocument.CreateEmpty(), null);

        _table.Symbols.ShouldBeEmpty();
        _table.Rows.ShouldBeEmpty();
    }

    [Fact]
    public void Move_DoesNotRebuildTheTable()
    {
        var rows = _table.Rows;

        _session.Document.MoveState("q0", new Point2(5, 5));

        _table.Rows.ShouldBeSameAs(rows);
    }
}
