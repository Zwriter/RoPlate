# P3 tasks: the simulation and the RoPlate app

**You own:** `Views/Simulator/**`, `Controls/InputTape/**`, `RealApps/RoPlate/**`, `Dfa.Core/Examples/license-plate.dfa.json`, `tests/Dfa.Tests/RealApps/**`.
**You use:** [01-core-api.md](contracts/01-core-api.md) (`DfaRun`, `BatchTester`), [03-app-ui.md](contracts/03-app-ui.md) (`DocumentSession`, `DfaCanvas`). **The format you write:** [02-json-format.md](contracts/02-json-format.md).

The theory for your automaton: [../theory/05-roplate-automaton.md](../theory/05-roplate-automaton.md).

In weeks 1–2 you don't have the drawing (E-02/E-05 belong to P2): you work on the input tape and the execution table, which don't need it.

---

### R-01 · Automaton A1 + its tests
- **Week:** 1 (the hand-written file), 4 (laid out in the editor) · **Depends on:** C-00 (format), C-06 (for the tests in the file)
- Write the complete `license-plate.dfa.json`: 14 states, the classes `L`, `C`, `_`, the δ table from the theory, **a description for every state** (these are RoPlate's messages), the 18 test words.
- Team decision: the simple variant or the one with the letter `B` separate (section 5.6 of the theory). Recommended: the variant with `B`.
- **Tests** in `tests/Dfa.Tests/RealApps/`: a `[Theory]` with every word, using `ExampleCatalog.Load("license-plate")`. Until C-07, build the automaton in code.
- In week 4: open the file in P2's editor, lay out the states nicely, save. **Milestone M4:** all tests also pass in the batch testing panel.

### S-01 · `SimulatorViewModel`
- **Week:** 1–2 · **Requirements:** SI-01, SI-03, SI-07 · **Depends on:** C-00, E-00 (works on the stubs too)
- An input field (empty = ε; shows the text "ε" when empty), the **Start** button: runs `DfaValidator`; if there are errors, it shows them and does not start.
- Commands: Step forward, Step back, Reset, Run to end, enabled/disabled correctly (`IsAtStart`, `IsAtEnd`).
- Exposes for the canvas: `CurrentState`, `HighlightKind`, `LastEdge`, `RequestedCanvasMode` (contract 3, section 6).
- **SI-07:** on `DocumentEdited` with `Structure`/`Reset` and on `DocumentReplaced` ⇒ stops the simulation + the message `The automaton was modified: the simulation was reset.`

### S-02 · The input tape (`InputTape`)
- **Week:** 2 · **Requirements:** SI-02
- Cells with one character each (space shown as `␣`), a cursor below the next symbol, symbols already read dimmed (`Brush.TapeRead`), the blocking cell marked (`ErrorPosition`).

### S-03 · The execution table
- **Week:** 2 · **Requirements:** SI-05
- Columns: Step, Read, (Class, if different), Remaining, State. Row 0 = the initial configuration. The current row highlighted; future rows hidden or dimmed.
- It must look exactly like the table in [../theory/02-step-by-step-simulation.md](../theory/02-step-by-step-simulation.md).

### S-04 · The verdict
- **Week:** 2 · **Requirements:** SI-06
- Shows `DfaRun.Explanation`, with a green ✓ / red ✗ (symbol + color).

### S-05 · Play / Pause
- **Week:** 3 · **Requirements:** SI-04
- A `DispatcherTimer` that calls `StepForward()`; a speed slider (e.g. 100–2000 ms) bound to `Interval`; it stops by itself at the end.

### S-06 · The animation on the drawing
- **Week:** 3 · **Requirements:** SI-03 · **Depends on:** E-05 (P2)
- Check with P2 that the `CurrentState`/`LastEdge`/`HighlightKind` → `DfaCanvas` bindings work.
- **Milestone M3:** full simulation on examples 1–3.

### S-07 · The batch testing panel
- **Week:** 4 · **Requirements:** SI-08 · **Depends on:** C-08
- `BatchTestPanel` + `BatchTestViewModel(DocumentSession)`: an editable list (word + an "expected accepted" check), the "Run tests" button, ✓ / ✗ on every row, the summary "9 of 10 correct".
- Changes to the list ⇒ `Document.SetTests` (saved with the automaton).
- Double-click on a row ⇒ opens the word in the simulator.

### S-08 · "Guess" mode
- **Week:** 4 · **Requirements:** SI-09 · **Depends on:** E-05 (`PickState`)
- The "Guess" check: before each step, `RequestedCanvasMode = PickState`; the user clicks the state they think the automaton goes to. The answer is confirmed or corrected ("Correct!" / "Wrong: the automaton goes to q2"), then the step is taken. The score appears at the end: "Score: 4 of 5".

### R-02 · The RoPlate screen
- **Week:** 5 · **Requirements:** app A1 · **Depends on:** R-01, C-03 (`Append`/`RemoveLast`)
- `RoPlateWindow` + `RoPlateViewModel(INavigationService)`: loads `ExampleCatalog.Load("license-plate")`.
- A large text field. Lowercase letters automatically become uppercase. On every character typed/deleted: `Append` / `RemoveLast` (for pasting or editing in the middle: `new DfaRun`).
- Three visual states (see the theory, section 5.8): red (blocked), neutral (incomplete, with the current state's description: "Incomplete format: waiting for 3 letters"), green ("Valid plate").
- The **"Show automaton"** button ⇒ `navigation.OpenInSimulator(document, the current text)`.
- **The messages come from the state descriptions** (data), they are not written in code.

### R-03 · Real county codes (bonus)
- **Week:** 5 · The variant with transitions only on the allowed letter pairs (AB, AR, AG, … VN). A separate file `license-plate-counties.dfa.json`, so you don't break the main automaton.

### Shared
- The report for app A1: the DFA drawing, the δ table, the meaning of each state, the list of test words (almost everything is already in the theory, section 5).
- The script for the final demo.
