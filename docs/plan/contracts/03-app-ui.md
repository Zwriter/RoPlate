# Contract 3: the user interface (`Dfa.App`)

**Owners:** P2 (session, navigation, dialogs, window, `DfaCanvas`), P1 (the δ table panel), P3 (the simulation panel, the tape, RoPlate).

This contract defines **how the panels connect to each other**, so that everyone can build their part separately and everything fits at the end.

---

## 1. Shared technical decisions

| Aspect | Decision |
|---|---|
| Platform | .NET 10, **Avalonia** (Windows, Linux, macOS). UI files are `.axaml`; custom control properties are `StyledProperty` |
| Bindings | compiled: every view sets `x:DataType` to its ViewModel |
| Dialogs | asynchronous (in Avalonia, modal windows and file pickers return `Task`); commands that use them are `[RelayCommand]` on `async Task` methods |
| MVVM | `CommunityToolkit.Mvvm`: `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]` |
| No DI container | objects are created by hand in `MainViewModel` (section 7) |
| Language | all visible text is in English |
| Code-behind | only for drawing and mouse handling (in `DfaCanvas`, `InputTape`); everything else in the ViewModel |

---

## 2. Main window layout

```
┌──────────────────────────────────────────────────────────────────────┐
│ File ▾  Examples ▾  Edit ▾   [↶] [↷]   [Complete with trap state]    │  ← menu (P2)
├───────────────────────────────────────────┬──────────────────────────┤
│                                           │ [ Edit | Simulate |      │
│                                           │   Tests ]                │
│            DfaCanvas (P2)                 │                          │
│     (the drawing of the automaton; in     │  Edit:     alphabet,     │
│      Simulate mode shows the current      │            classes (P2)  │
│      state)                               │            δ table (P1)  │
│                                           │  Simulate: SimulatorPanel│
│                                           │            (P3)          │
│                                           │  Tests:    BatchTestPanel│
│                                           │            (P3)          │
├───────────────────────────────────────────┴──────────────────────────┤
│ status bar: messages / validation errors (P2)                        │
└──────────────────────────────────────────────────────────────────────┘
Separate window:  RoPlateWindow (P3), opened from the "Apps ▸ RoPlate" menu
```

There is **only one** canvas, used both for editing and for simulation.

---

## 3. `DocumentSession`: the open document (P2)

A single object, shared by all panels. Every panel reads the automaton from here.

```csharp
namespace Dfa.App.Services;

public sealed partial class DocumentSession : ObservableObject
{
    [ObservableProperty] private DfaDocument document = DfaDocument.CreateEmpty();
    [ObservableProperty] private string? filePath;          // null = not saved yet
    [ObservableProperty] private bool isDirty;              // unsaved changes (shown with * in the title)

    public DocumentHistory History { get; }                 // undo/redo for the current document

    // Raised when the document was REPLACED (New / Open / Examples)
    public event EventHandler? DocumentReplaced;

    // Forwards Document.Changed for the current document, so you don't resubscribe on every replacement
    public event EventHandler<DocumentChangedEventArgs>? DocumentEdited;

    public void Replace(DfaDocument document, string? filePath);   // clears History, raises DocumentReplaced
}
```

**Rule SI-07:** the simulation panel listens to `DocumentEdited` and **resets** when `Kind` is `Structure` or `Reset`, showing the message `The automaton was modified: the simulation was reset.` Moving states (`Layout`) does **not** reset the simulation.

---

## 4. `INavigationService` and `IDialogService` (P2)

```csharp
namespace Dfa.App.Services;

public enum MainTab { Editor, Simulator, Tests }

public interface INavigationService
{
    void ShowTab(MainTab tab);
    // "Show automaton" from RoPlate: loads a COPY of the document and starts the simulation on input
    void OpenInSimulator(DfaDocument document, string input);
    void ShowRoPlate();
}

public interface IDialogService
{
    Task ShowErrorAsync(string message);
    Task ShowErrorsAsync(string title, IEnumerable<string> messages);
    Task ShowInfoAsync(string message);
    Task<bool> ConfirmAsync(string message);                                   // Yes / No
    Task<string?> AskTextAsync(string title, string prompt, string initial = ""); // null = Cancel
    Task<string?> PickOpenFileAsync();                                          // *.dfa.json filter
    Task<string?> PickSaveFileAsync(string suggestedName);
}
```

For tests and for working in parallel, P2 provides simple implementations from day one (a custom message window, because Avalonia has no built-in `MessageBox`; files are picked with `TopLevel.StorageProvider`), so P3 can use them right away.

---

## 5. `DfaCanvas`: the drawing (P2, used by P3)

A `UserControl` in `Dfa.App.Controls`. It draws `Document` and redraws itself on `Document.Changed`.

### Properties (StyledProperty, bindable from AXAML)

| Property | Type | Role |
|---|---|---|
| `Document` | `DfaDocument?` | what is drawn |
| `Mode` | `CanvasMode` | `Edit`, `ReadOnly`, `PickState` |
| `HighlightedState` | `string?` | the current state in the simulation |
| `HighlightKind` | `HighlightKind` | `None`, `Current`, `Accepted`, `Rejected` (the highlighted state's color) |
| `HighlightedEdge` | `EdgeRef?` | the arrow just taken |
| `SelectedState` | `string?` | the state selected in the editor (two-way: `defaultBindingMode: BindingMode.TwoWay`) |

```csharp
namespace Dfa.App.Controls;

public enum CanvasMode { Edit, ReadOnly, PickState }
public enum HighlightKind { None, Current, Accepted, Rejected }
public sealed record EdgeRef(string From, string To);      // one arrow = one (From, To) pair, like Dfa.Edges
```

### Events

| Event | When | Used by |
|---|---|---|
| `StateClicked(string state)` | click on a state, in any mode | P3: "Guess" mode (SI-09), with `Mode = PickState` |
| `StatusMessage(string text)` | error/info while editing (e.g. the ED-04 message) | P2: the status bar |

### Behavior per mode

| Mode | What can be done |
|---|---|
| `Edit` | everything ED-01…ED-04 require (add, move, delete, transitions, context menu). A simulation may be active: editing resets it (SI-07) |
| `ReadOnly` | view only (RoPlate: "Show automaton", export) |
| `PickState` | clicking a state only raises `StateClicked`; no editing |

### Visual conventions (the clarity requirement: **not color alone**)

| Element | Drawing |
|---|---|
| state | circle with radius 28 px, name in the center |
| final state | double circle (second circle, radius 22 px) |
| start state | short arrow from the left, labeled "start" |
| current state (`Current`) | **thick (4 px)** outline + the `Brush.StateCurrent` color |
| accepted / rejected | thick outline + `Brush.Accepted` / `Brush.Rejected` + a ✓ / ✗ symbol next to the state |
| last arrow | **thick** line + `Brush.EdgeLast` |
| state description (SI-10) | small gray text below the circle |

### Shared styles (`Themes/Styles.axaml`, P2)

Resource keys used by everyone: `Brush.StateCurrent`, `Brush.Accepted`, `Brush.Rejected`, `Brush.EdgeLast`, `Brush.MissingCell` (empty cells in the δ table), `Brush.TapeRead` (symbols already read, dimmed), `Brush.TapeCursor`.

---

## 6. The panels

Each panel = a `UserControl` + its ViewModel. The ViewModel constructor receives only what it needs.

| Panel | Owner | ViewModel | Constructor |
|---|---|---|---|
| `EditorPanel` (alphabet, classes, descriptions) | P2 | `EditorViewModel` | `(DocumentSession, IDialogService)` |
| `TransitionTablePanel` (δ table, ED-06) | P1 | `TransitionTableViewModel` | `(DocumentSession)` |
| `SimulatorPanel` (SI-01…SI-07, SI-09) | P3 | `SimulatorViewModel` | `(DocumentSession, IDialogService)` |
| `BatchTestPanel` (SI-08) | P3 | `BatchTestViewModel` | `(DocumentSession)` |
| `RoPlateWindow` (A1) | P3 | `RoPlateViewModel` | `(INavigationService)` |

### What `SimulatorViewModel` exposes for the canvas (P3 → P2)

The main window (P2) binds the canvas to these properties:

```csharp
public sealed partial class SimulatorViewModel : ObservableObject
{
    public string? CurrentState { get; }          // -> DfaCanvas.HighlightedState
    public HighlightKind HighlightKind { get; }   // -> DfaCanvas.HighlightKind
    public EdgeRef? LastEdge { get; }             // -> DfaCanvas.HighlightedEdge
    public CanvasMode RequestedCanvasMode { get; }// Edit normally, PickState in "Guess"
    public void OnStateClicked(string state);     // called by the window on DfaCanvas.StateClicked

    public void Start(string input);              // also used by INavigationService.OpenInSimulator
    public void Stop();                           // when leaving the tab: HighlightedState = null
}
```

When the active tab is not "Simulate", the window sets `HighlightedState = null` and `Mode = Edit`.

### `InputTape` (P3)

A `UserControl` in `Dfa.App.Controls`, used only by P3:

| Property | Type | Role |
|---|---|---|
| `Text` | `string` | the word |
| `CursorPosition` | `int` | index of the next symbol (= `Steps[Position-1].Position + 1`, or 0) |
| `ErrorPosition` | `int?` | the cell marked in red when blocked |

---

## 7. How it is assembled (P2, in `MainViewModel`)

```csharp
var dialogs    = new DialogService(owner);   // owner = MainWindow (TopLevel): for ShowDialog and StorageProvider
var session    = new DocumentSession();
var navigation = new NavigationService(this, session);

Editor     = new EditorViewModel(session, dialogs);
Table      = new TransitionTableViewModel(session);
Simulator  = new SimulatorViewModel(session, dialogs);
BatchTests = new BatchTestViewModel(session);
```

Until a panel is ready, `MainWindow` shows a "Work in progress" `TextBlock` in its place. Everyone can also test their panel separately by putting it temporarily in a test window (`DevWindow`, which does not go into `main`).
