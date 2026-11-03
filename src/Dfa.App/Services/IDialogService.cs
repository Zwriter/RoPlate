namespace Dfa.App.Services;

using System.Collections.Generic;
using System.Threading.Tasks;

public interface IDialogService
{
    Task ShowErrorAsync(string message);

    Task ShowErrorsAsync(string title, IEnumerable<string> messages);

    Task ShowInfoAsync(string message);

    // Yes / No
    Task<bool> ConfirmAsync(string message);

    // null = Cancel
    Task<string?> AskTextAsync(string title, string prompt, string initial = "");

    // *.dfa.json filter
    Task<string?> PickOpenFileAsync();

    Task<string?> PickSaveFileAsync(string suggestedName);
}
