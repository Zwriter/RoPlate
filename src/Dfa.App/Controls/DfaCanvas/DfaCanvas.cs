namespace Dfa.App.Controls;

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Dfa.Core.Documents;

// Stub (E-00): all properties and events from contract 3; drawing and editing come in E-02 / E-03.
public sealed class DfaCanvas : UserControl
{
    public static readonly StyledProperty<DfaDocument?> DocumentProperty =
        AvaloniaProperty.Register<DfaCanvas, DfaDocument?>(nameof(Document));

    public static readonly StyledProperty<CanvasMode> ModeProperty =
        AvaloniaProperty.Register<DfaCanvas, CanvasMode>(nameof(Mode));

    public static readonly StyledProperty<string?> HighlightedStateProperty =
        AvaloniaProperty.Register<DfaCanvas, string?>(nameof(HighlightedState));

    public static readonly StyledProperty<HighlightKind> HighlightKindProperty =
        AvaloniaProperty.Register<DfaCanvas, HighlightKind>(nameof(HighlightKind));

    public static readonly StyledProperty<EdgeRef?> HighlightedEdgeProperty =
        AvaloniaProperty.Register<DfaCanvas, EdgeRef?>(nameof(HighlightedEdge));

    public static readonly StyledProperty<string?> SelectedStateProperty =
        AvaloniaProperty.Register<DfaCanvas, string?>(nameof(SelectedState), defaultBindingMode: BindingMode.TwoWay);

    public event EventHandler<string>? StateClicked;

    public event EventHandler<string>? StatusMessage;

    public DfaDocument? Document
    {
        get => GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public CanvasMode Mode
    {
        get => GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public string? HighlightedState
    {
        get => GetValue(HighlightedStateProperty);
        set => SetValue(HighlightedStateProperty, value);
    }

    public HighlightKind HighlightKind
    {
        get => GetValue(HighlightKindProperty);
        set => SetValue(HighlightKindProperty, value);
    }

    public EdgeRef? HighlightedEdge
    {
        get => GetValue(HighlightedEdgeProperty);
        set => SetValue(HighlightedEdgeProperty, value);
    }

    public string? SelectedState
    {
        get => GetValue(SelectedStateProperty);
        set => SetValue(SelectedStateProperty, value);
    }

    private void OnStateClicked(string state) => StateClicked?.Invoke(this, state);

    private void OnStatusMessage(string text) => StatusMessage?.Invoke(this, text);
}
