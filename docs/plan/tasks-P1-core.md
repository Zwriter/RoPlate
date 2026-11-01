# P1 tasks: the engine (`Dfa.Core`) + the δ table

**You own:** `src/Dfa.Core/**`, `tests/Dfa.Tests/Core/**`, `src/Dfa.App/Views/TransitionTable/**`, the solution.
**Contracts you implement:** [01-core-api.md](contracts/01-core-api.md), [02-json-format.md](contracts/02-json-format.md).

You are the first one the others depend on, so **C-00 is done in the first 1–2 days**, before any other task.

---

### C-00 · The solution and the contract stubs ⚠ blocks everyone
- **Week:** 1 (days 1–2) · **Depends on:** nothing
- Create `DfaSimulator.slnx` with `Dfa.Core` (class library), `Dfa.App` (Avalonia, from the `avalonia.app` template), `Dfa.Tests` (xUnit + Shouldly) and the references between them.
- Enable `<Nullable>enable</Nullable>` everywhere.
- Write **all the public types** from contract 1, with `throw new NotImplementedException()` bodies (records and enums are written in full).
- **Done when:** the solution compiles, and P2 and P3 can write code that calls the engine.

### C-01 · `Dfa`: the model and editing
- **Week:** 1 · **Requirements:** ED-03, ED-04, ED-05 · **Depends on:** C-00
- States, Σ, start, finals, δ as a `TransitionKey → string` dictionary, grouped `Edges`, `Clone()`.
- Every operation returns `OpResult` with the **exact messages** from the contract.
- **Tests:** duplicate transition refused with the ED-04 message; foreign symbol refused; `RemoveState` removes the transitions; `RenameState` updates δ, start and finals; `AddTransitions` is "all or nothing".

### C-02 · Symbol classes and `SymbolOf`
- **Week:** 1 · **Requirements:** ED-10 · **Depends on:** C-01
- `SymbolClass.FromRanges`, `Contains`, the 3-step mapping rule, refusing overlapping classes.
- **Tests:** `SymbolOf('7') == "C"`, `SymbolOf(' ') == "_"`, `SymbolOf('_') == null` (when `_` is a class), unknown character ⇒ `null`.

### C-03 · `DfaRun`: step-by-step execution
- **Week:** 1 · **Requirements:** SI-01, SI-03, SI-05, SI-06 · **Depends on:** C-02
- Precomputed steps, forward/back navigation, verdict + reason + `Explanation` with the exact texts, `Append`/`RemoveLast` for RoPlate, `Changed`.
- **Tests:** every word in the table in section 9 of the specification (examples 1–6) with `[Theory]`/`[InlineData]`; ε; blocking (the last step = `NoTransition`, the verdict has the right position); foreign symbol; no start state; `Append` ×n equivalent to `new DfaRun(dfa, the whole word)`.
- Build the test automata **in code** (there is no JSON yet).

### C-04 · `DfaValidator`
- **Week:** 1 · **Depends on:** C-02
- Every code in the contract (`NO_START`, `UNREACHABLE` by traversal from the start state, etc.).
- **Tests:** one test per code.
- **Milestone M1:** C-00…C-04 in `main`, tests green.

### C-05 · `DfaDocument`
- **Week:** 2 · **Depends on:** C-01
- Editing operations that keep `Layout`/`Descriptions` in sync, automatic names `q0, q1…` (first free), `Changed` with the right `Kind` (exactly one event per successful operation, none on failure), `Clone`/`RestoreFrom`.
- **Tests:** `RenameState` moves the position and the description; `MoveState` gives `Layout`, not `Structure`; a failed operation does not raise `Changed`.

### C-06 · `DfaJson`
- **Week:** 2 · **Requirements:** ED-07, robustness · **Depends on:** C-05
- Reading/writing per contract 2; every error in its table; automatic positions for states without `layout`.
- **Tests:** round trip (`Deserialize(Serialize(doc))`); one corrupt file per error type; truncated JSON does not throw.

### C-07 · `ExampleCatalog` + examples 1–6
- **Week:** 2 · **Requirements:** ED-07 · **Depends on:** C-06
- Write `even-a`, `starts-a` (with a trap state), `ends-ab`, `contains-aba`, `div3`, `length-3` in `Dfa.Core/Examples/`, as **EmbeddedResource**, with nice positions, a description for every state and the test words from the specification.
- The theory for each example is in [../theory/03-designing-a-dfa.md](../theory/03-designing-a-dfa.md).
- **Tests:** every example loads without errors and passes its own tests (`BatchTester`).

### C-08 · `BatchTester`
- **Week:** 3 · **Requirements:** SI-08 · **Depends on:** C-03
- `Run`, `Summary` = "9 of 10 correct".

### C-09 · `DocumentHistory` (undo/redo)
- **Week:** 3 · **Requirements:** ED-09 · **Depends on:** C-05
- **Tests:** add 3 states ⇒ undo ×3 ⇒ empty document ⇒ redo ×3; a new operation after undo clears redo; the capacity is respected.

### C-10 · `CompleteWithTrap`
- **Week:** 3 · **Requirements:** ED-08 · **Depends on:** C-05
- Adds a non-final state that receives every missing transition (including its own loops). If the automaton is already complete ⇒ `OpResult.Fail("The automaton is already complete.")`.
- **Tests:** after the operation, the validator no longer reports `MISSING_TRANSITION`; the recognized language does not change (same results on the test words).

### C-11 · The δ table panel (UI)
- **Week:** 4 · **Requirements:** ED-06 (+ ED-11 bonus) · **Depends on:** C-05, E-00
- `TransitionTablePanel` + `TransitionTableViewModel(DocumentSession)`: rows = states (with `→` and `*`), columns = Σ, empty cells colored with `Brush.MissingCell`. Updates on `DocumentEdited` and `DocumentReplaced`.
- ED-11 bonus: editable cell (a list of the states) ⇒ `AddTransitions` / `RemoveTransition`.

### C-12 · Coverage ≥ 70%
- **Week:** 4 · Add `coverlet.collector` and check the coverage of `Dfa.Core`:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### C-13 · The word generator (bonus)
- **Week:** 5 · **Requirements:** SI-11 · Breadth-first search (BFS); see [../theory/04-simulator-and-architecture.md](../theory/04-simulator-and-architecture.md#42-the-generator).
- **Tests:** for `even-a`, the first 4 are `"", "b", "aa", "bb"`.

### Shared
- The report: the sections on the engine and on examples 1–6.
