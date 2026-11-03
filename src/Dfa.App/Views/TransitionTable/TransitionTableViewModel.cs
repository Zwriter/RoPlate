namespace Dfa.App.Views.TransitionTable;

using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Dfa.App.Services;
using Dfa.Core.Documents;

public sealed partial class TransitionTableViewModel : ObservableObject
{
    private readonly DocumentSession _session;

    [ObservableProperty]
    private IReadOnlyList<string> _symbols = [];

    [ObservableProperty]
    private IReadOnlyList<TransitionRow> _rows = [];

    public TransitionTableViewModel(DocumentSession session)
    {
        _session = session;
        session.DocumentReplaced += (_, _) => Refresh();
        session.DocumentEdited += (_, e) =>
        {
            // Only δ, Σ, the states and start/final show in the table; moves and descriptions do not.
            if (e.Kind is DocumentChangeKind.Structure or DocumentChangeKind.Reset)
            {
                Refresh();
            }
        };
        Refresh();
    }

    private void Refresh()
    {
        var dfa = _session.Document.Dfa;
        Symbols = [.. dfa.Alphabet];
        Rows = [.. dfa.States.Select(state => new TransitionRow(
            $"{(dfa.StartState == state ? "→" : "")}{(dfa.FinalStates.Contains(state) ? "*" : "")}{state}",
            [.. dfa.Alphabet.Select(symbol => dfa.Next(state, symbol))]))];
    }
}
