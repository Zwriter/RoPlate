namespace Dfa.App.Services;

using Dfa.Core.Documents;

public interface INavigationService
{
    void ShowTab(MainTab tab);

    // "Show automaton" from RoPlate: loads a COPY of the document and starts the simulation on input
    void OpenInSimulator(DfaDocument document, string input);

    void ShowRoPlate();
}
