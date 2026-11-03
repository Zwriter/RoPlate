namespace Dfa.App.Services;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

// Avalonia has no built-in MessageBox, so the dialogs are small windows built here.
public sealed class DialogService(Window owner) : IDialogService
{
    private static readonly FilePickerFileType _dfaFileType = new("DFA automaton") { Patterns = ["*.dfa.json"] };

    public Task ShowErrorAsync(string message) => ShowAsync("Error", Text(message), "OK");

    public Task ShowErrorsAsync(string title, IEnumerable<string> messages)
    {
        var list = new StackPanel { Spacing = 4 };
        list.Children.AddRange(messages.Select(m => Text("• " + m)));
        return ShowAsync(title, new ScrollViewer { MaxHeight = 320, Content = list }, "OK");
    }

    public Task ShowInfoAsync(string message) => ShowAsync("Information", Text(message), "OK");

    public async Task<bool> ConfirmAsync(string message) =>
        await ShowAsync("Confirm", Text(message), "Yes", "No") == 0;

    public async Task<string?> AskTextAsync(string title, string prompt, string initial = "")
    {
        var input = new TextBox { Text = initial };
        var body = new StackPanel { Spacing = 8, Children = { Text(prompt), input } };
        return await ShowAsync(title, body, input, "OK", "Cancel") == 0 ? input.Text ?? "" : null;
    }

    public async Task<string?> PickOpenFileAsync()
    {
        var files = await owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open automaton",
            FileTypeFilter = [_dfaFileType],
        });
        return files.Count == 0 ? null : files[0].TryGetLocalPath();
    }

    public async Task<string?> PickSaveFileAsync(string suggestedName)
    {
        var file = await owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save automaton",
            SuggestedFileName = suggestedName,
            DefaultExtension = "dfa.json",
            FileTypeChoices = [_dfaFileType],
        });
        return file?.TryGetLocalPath();
    }

    private static TextBlock Text(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };

    private Task<int?> ShowAsync(string title, Control body, params string[] buttons) =>
        ShowAsync(title, body, null, buttons);

    // Returns the index of the pressed button, or null when the window is closed without one.
    private Task<int?> ShowAsync(string title, Control body, Control? focus, params string[] buttons)
    {
        var window = new Window
        {
            Title = title,
            SizeToContent = SizeToContent.WidthAndHeight,
            MinWidth = 320,
            MaxWidth = 560,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        for (var i = 0; i < buttons.Length; i++)
        {
            var index = i;
            var button = new Button { Content = buttons[i], IsDefault = i == 0, IsCancel = i == buttons.Length - 1 };
            button.Click += (_, _) => window.Close(index);
            row.Children.Add(button);
        }

        window.Content = new StackPanel { Margin = new Thickness(16), Spacing = 16, Children = { body, row } };
        window.Opened += (_, _) => (focus ?? row.Children[0]).Focus();
        return window.ShowDialog<int?>(owner);
    }
}
