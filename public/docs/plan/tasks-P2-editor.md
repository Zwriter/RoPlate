# P2 tasks: the main window, the drawing and the editor

**You own:** `App.axaml`, `MainWindow.axaml`, `Services/**`, `Themes/**`, `Controls/DfaCanvas/**`, `Views/Editor/**`.
**Contract you implement:** [03-app-ui.md](contracts/03-app-ui.md) (session, navigation, dialogs, canvas). **You use:** [01-core-api.md](contracts/01-core-api.md).

Until P1 finishes the engine (week 1), build test documents directly in code; after C-07 use `ExampleCatalog`.

---

### E-00 · The UI stubs ⚠ blocks P1 (C-11) and P3
- **Week:** 1 (days 2–3) · **Depends on:** C-00
- Write `DocumentSession`, `INavigationService`, `IDialogService` (+ a simple `DialogService`: a custom message window and `StorageProvider` for files), the enums and `EdgeRef` from contract 3, and an empty `DfaCanvas` that already has all its properties and events.
- Add `CommunityToolkit.Mvvm`.
- **Done when:** P3 can write `SimulatorViewModel(DocumentSession, IDialogService)` and bind it to a `DfaCanvas` (even if it does not draw anything yet).

### E-01 · The main window and the menus
- **Week:** 1–2 · **Requirements:** ED-07 · **Depends on:** E-00 (the Open/Save menus after C-06)
- The layout from contract 3 (canvas on the left, tabs on the right, status bar), with "Work in progress" in place of unfinished panels.
- The **File** menu: New, Open, Save, Save as. Load errors are all shown, with `ShowErrorsAsync`. A "Save changes?" question when `IsDirty`.
- The **Examples** menu: generated from `ExampleCatalog.All`.
- The **Apps ▸ RoPlate** menu ⇒ `INavigationService.ShowRoPlate()`.
- The window title: `{name} — DFA Simulator`, with `*` when there are unsaved changes.
- `NavigationService.OpenInSimulator`: `session.Replace(document.Clone(), null)`, switches to the Simulate tab, calls `Simulator.Start(input)`.

### E-02 · Drawing the automaton ⚠ P3 needs it in week 3
- **Week:** 2 · **Depends on:** C-05 (or a document built in code)
- Draws `Document` following the visual conventions in the contract:
  - states, double circle, the "start" arrow;
  - arrows start and end **on the edge** of the circle (the direction between centers, minus the radius);
  - transitions in both directions between the same two states = two curved arcs;
  - loops = an arc above the state;
  - a single arrow per `Dfa.Edges`, labeled `a, b`.
- Redraw on `Document.Changed`.
- **Milestone M2:** you open an example from the menu and it appears drawn correctly.

### E-05 · Highlighting for the simulation
- **Week:** 2 (together with E-02) · **Requirements:** SI-03 · **Depends on:** E-02
- `HighlightedState`, `HighlightKind`, `HighlightedEdge`: **only the style** of the existing elements changes, without redrawing the whole graph.
- Differences not by color alone: thick outline, ✓ / ✗.
- `Mode = PickState` ⇒ a click only raises `StateClicked`.
- **Hand it over to P3** and let them know (S-06 depends on it).

### E-03 · Editing on the drawing
- **Week:** 3 · **Requirements:** ED-01…ED-04 · **Depends on:** E-02, C-05
- Click on an empty area ⇒ `AddState(position)`.
- Drag & drop ⇒ visual move while dragging; `MoveState` **only when the mouse is released** (a single undo step).
- Selection + the Delete key ⇒ `RemoveState` / `RemoveEdge`.
- Context menu on a state: Start state, Final state (check), Rename (`AskTextAsync`), Delete.
- Transition: Shift + drag from one state to another (or to itself) ⇒ `AskTextAsync("Symbols", "e.g. a,b")` ⇒ `AddTransitions`. If you get an `OpResult` with an error, show the message (e.g. "From q0 there is already a transition on 'a' to q1.").
- Double-click on an arrow ⇒ edit its symbols.

### E-04 · The alphabet and symbol classes
- **Week:** 3 · **Requirements:** ED-05, ED-10 · **Depends on:** C-02
- In `EditorPanel`: the Σ list (add/remove symbol), the list of classes (name + characters, with help for ranges: "A-Z" ⇒ `SymbolClass.FromRanges`). Errors from `OpResult` appear below the field.
- P1's `TransitionTablePanel` goes here too (below the alphabet).

### E-06 · Undo/redo and the trap state in the UI
- **Week:** 4 · **Requirements:** ED-08, ED-09 · **Depends on:** C-09, C-10
- The ↶ / ↷ buttons and Ctrl+Z / Ctrl+Y bound to `session.History`, enabled by `CanUndo`/`CanRedo`.
- The "Complete with trap state" button ⇒ `CompleteWithTrap(free position)`.

### E-07 · State descriptions
- **Week:** 4 · **Requirements:** SI-10 · **Depends on:** C-05
- Small text below each state on the drawing; edited from the context menu ("Description…") ⇒ `SetDescription`.
- They matter for RoPlate (its messages come from them).

### E-08 · Image export (bonus)
- **Week:** 5 · **Requirements:** ED-12 · PNG with `RenderTargetBitmap`; SVG optional.

### Shared
- The report: screenshots of the editor, the automaton drawings for the report (after E-08 they can be exported directly).
