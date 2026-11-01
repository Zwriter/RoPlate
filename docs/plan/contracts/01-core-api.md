# Contract 1: the engine API (`Dfa.Core`)

**Owner:** P1. **Used by:** P2 (editor, canvas), P3 (simulation, RoPlate), the tests.

`Dfa.Core` is a **class library** project with no reference to Avalonia or any other UI framework. Namespaces follow the folders: `Dfa.Core.Model` (`Dfa`, `TransitionKey`, `Edge`, `SymbolClass`, `OpResult`), `Dfa.Core.Simulation` (`DfaRun`, `DfaStep`, `StepOutcome`, `Verdict`, `RejectReason`, `DfaExtensions`, `BatchTester`, `BatchResult`, `TestResult`, `LanguageGenerator`), `Dfa.Core.Validation` (`DfaValidator`, `ValidationIssue`, `Severity`), `Dfa.Core.Documents` (`DfaDocument`, `DocumentHistory`, `DocumentChangeKind`, `DocumentChangedEventArgs`, `Point2`, `TestCase`), `Dfa.Core.Serialization` (`DfaJson`, `LoadResult`), `Dfa.Core.Examples` (`ExampleCatalog`, `ExampleInfo`). `using` directives go **after** the `namespace …;` line; otherwise `Dfa` resolves to the root namespace (error CS0118). The project has `<Nullable>enable</Nullable>`.

Below are **only the public signatures** and the behavior rules. The implementation is free.

---

## 1. The result of an operation

Operations that can fail because of the user **do not throw**: they return an `OpResult` with a message ready to display.

```csharp
public sealed record OpResult(bool Ok, string? Error = null)
{
    public static OpResult Success { get; } = new(true);
    public static OpResult Fail(string message) => new(false, message);
}
```

Exceptions are only for programming errors (e.g. `null` where it is not allowed).

---

## 2. Symbols and symbol classes

```csharp
public sealed record SymbolClass(string Name, string Characters)
{
    public bool Contains(char c);
    // Builds from a range description: FromRanges("L", "A-Z"), FromRanges("C", "0-9")
    public static SymbolClass FromRanges(string name, string ranges);
}
```

**Alphabet (Σ) rules:**
- Σ is a list of **symbol names** (`string`).
- A **simple symbol** has exactly 1 character (`"a"`, `"0"`).
- A **class** has any name (`"L"`, `"C"`, `"_"`) and a list of characters. The class name appears in Σ.
- Classes **must not share characters** with each other or with simple symbols (otherwise determinism is lost).
- **Mapping an input character** (`Dfa.SymbolOf(char c)`):
  1. if a class contains `c` ⇒ the class name;
  2. otherwise, if `c.ToString()` is in Σ and is **not** a class name ⇒ `c.ToString()`;
  3. otherwise ⇒ `null` (the character is not in the alphabet).

Example: Σ = {L, C, _}, `_` = a class holding the space character. Then `SymbolOf(' ') == "_"` and `SymbolOf('_') == null`.

---

## 3. `Dfa`: the automaton

```csharp
public readonly record struct TransitionKey(string State, string Symbol);

// One arrow in the drawing: all symbols leading from From to To
public sealed record Edge(string From, string To, IReadOnlyList<string> Symbols);

public sealed class Dfa
{
    // ---- reading ----
    public IReadOnlyList<string> States { get; }                 // in insertion order
    public IReadOnlyList<string> Alphabet { get; }               // Σ
    public IReadOnlyList<SymbolClass> SymbolClasses { get; }
    public string? StartState { get; }                           // null = not set
    public IReadOnlySet<string> FinalStates { get; }
    public IReadOnlyDictionary<TransitionKey, string> Transitions { get; }   // δ

    public string? Next(string state, string symbol);            // δ(state, symbol) or null
    public string? SymbolOf(char c);                             // see section 2
    public IReadOnlyList<Edge> Edges { get; }                    // transitions grouped by (From, To)
    public bool IsFinal(string state);

    // ---- editing (used by DfaDocument and the tests) ----
    public OpResult AddState(string name);
    public OpResult RemoveState(string name);                    // also removes the transitions touching it
    public OpResult RenameState(string oldName, string newName);
    public OpResult SetStart(string? name);
    public OpResult SetFinal(string name, bool isFinal);
    public OpResult AddSymbol(string symbol);                    // simple symbol, 1 character
    public OpResult AddSymbolClass(SymbolClass symbolClass);
    public OpResult RemoveSymbol(string symbol);                 // fails if used in transitions
    public OpResult AddTransition(string from, string symbol, string to);
    public OpResult AddTransitions(string from, IEnumerable<string> symbols, string to); // "a,b": all or nothing
    public OpResult RemoveTransition(string from, string symbol);

    public Dfa Clone();
}
```

**Why a dictionary for δ:** the key `(state, symbol)` cannot appear twice ⇒ determinism is guaranteed by the structure (ED-04).

### Error messages (exactly these, for consistency in the UI)

| Situation | Message |
|---|---|
| state already exists | `A state named '{name}' already exists.` |
| state does not exist | `State '{name}' does not exist.` |
| empty name | `The state name cannot be empty.` |
| duplicate transition (ED-04) | `From {from} there is already a transition on '{symbol}' to {to}.` |
| symbol outside Σ (ED-05) | `Symbol '{symbol}' is not in the alphabet.` |
| duplicate symbol in Σ | `Symbol '{symbol}' is already in the alphabet.` |
| simple symbol with more than one character | `A simple symbol must have exactly one character. Use a symbol class for groups.` |
| overlapping classes | `Character '{c}' already belongs to class '{other}'.` |
| removing a used symbol | `Symbol '{symbol}' is used in {n} transitions. Remove them first.` |
| removing a missing transition | `From {from} there is no transition on '{symbol}'.` |

---

## 4. `DfaRun`: one run (the execution table)

```csharp
public enum StepOutcome { Moved, NoTransition, NotInAlphabet }

public sealed record DfaStep(
    int Number,            // 1, 2, 3… (step 0 = the initial configuration, NOT in the list)
    int Position,          // index of the character in the word, from 0
    char Character,        // the character read
    string? Symbol,        // the symbol after mapping (e.g. 'K' -> "L"); null if NotInAlphabet
    string From,           // the state before
    string? To,            // the state after; null if blocked
    StepOutcome Outcome);

public enum Verdict { InProgress, Accepted, Rejected }
public enum RejectReason { None, NoStartState, EndedInNonFinal, NoTransition, NotInAlphabet }

public sealed class DfaRun
{
    public DfaRun(Dfa dfa, string input);          // input "" = ε

    public Dfa Dfa { get; }
    public string Input { get; }
    public IReadOnlyList<DfaStep> Steps { get; }   // ALL steps, computed in the constructor
    public int Position { get; }                   // how many steps are "done": 0..Steps.Count
    public bool IsAtStart { get; }                 // Position == 0
    public bool IsAtEnd { get; }                   // Position == Steps.Count
    public bool IsBlocked { get; }                 // the last step done was NoTransition / NotInAlphabet

    public string? CurrentState { get; }           // when blocked, stays the state it blocked in; null without a start state
    public DfaStep? LastStep { get; }              // Steps[Position - 1] or null
    public string RemainingInput { get; }          // what is left to read after the current step

    public Verdict Verdict { get; }                // at the current position: InProgress until the end
    public RejectReason Reason { get; }
    public string Explanation { get; }             // the SI-06 message, see the table below

    public Verdict FinalVerdict { get; }           // the verdict for the whole word, regardless of Position

    public bool StepForward();                     // false if already at the end
    public bool StepBack();                        // false if already at the start
    public void Reset();                           // Position = 0
    public void RunToEnd();                        // Position = Steps.Count

    // For RoPlate (incremental run, one character at a time):
    public void Append(char c);                    // adds at most one new step; Position moves to the end
    public void RemoveLast();                      // the inverse of Append (Backspace)

    public event EventHandler? Changed;            // after any change of Position / Input
}

public static class DfaExtensions
{
    public static bool Accepts(this Dfa dfa, string input);   // shortcut for tests
}
```

**Rules:**
- A DFA takes at most `|input|` steps. After a block **no more steps are taken**; the last element of `Steps` is the blocking step.
- If `StartState == null` ⇒ `Steps` is empty, `Verdict = Rejected`, `Reason = NoStartState`.
- For ε: `Steps` is empty, and the verdict depends only on whether the start state is final.
- "Remaining input" for a step's row in the table: `Input[(step.Position + 1)..]`.

### Verdict messages (`Explanation`)

Positions in messages **start at 1** (for the user).

| Situation | Message |
|---|---|
| in progress | `Step {Position} of {Steps.Count}.` |
| accepted | `ACCEPTED: the word ended in the final state {state}.` |
| non-final state | `REJECTED: the word ended in {state}, which is not final.` |
| missing transition | `REJECTED: from {state} there is no transition on '{c}' (position {pos}).` |
| missing transition, with class | `REJECTED: from {state} there is no transition on '{c}' (class {symbol}, position {pos}).` |
| foreign symbol | `REJECTED: symbol '{c}' is not in the alphabet (position {pos}).` |
| no start state | `REJECTED: the automaton has no start state.` |

---

## 5. `DfaValidator`

```csharp
public enum Severity { Error, Warning, Info }

public sealed record ValidationIssue(
    Severity Severity, string Code, string Message,
    string? State = null, string? Symbol = null);

public static class DfaValidator
{
    public static IReadOnlyList<ValidationIssue> Validate(Dfa dfa);
    public static bool CanSimulate(IReadOnlyList<ValidationIssue> issues);   // no Error
}
```

| Code | Severity | Message |
|---|---|---|
| `NO_START` | Error | `The automaton has no start state.` |
| `NO_STATES` | Error | `The automaton has no states.` |
| `EMPTY_ALPHABET` | Error | `The alphabet is empty.` |
| `CLASS_OVERLAP` | Error | `Classes '{a}' and '{b}' share characters.` |
| `UNREACHABLE` | Warning | `State {state} cannot be reached from the start state.` |
| `NO_FINAL` | Warning | `The automaton has no final states: it will reject every word.` |
| `MISSING_TRANSITION` | Info | `From {state} the transition on '{symbol}' is missing.` |

The simulation starts only if there is no `Error` (step 1 of the simulation flow).

---

## 6. `DfaDocument`: what is saved and edited

The UI **modifies the automaton only through `DfaDocument`**, because the document also holds the positions, descriptions and tests, and announces changes.

```csharp
public readonly record struct Point2(double X, double Y);
public sealed record TestCase(string Word, bool Expected);   // Word "" = ε

public enum DocumentChangeKind
{
    Structure,     // the language changed: states, Σ, δ, start, finals  ⇒ the simulation resets (SI-07)
    Layout,        // positions only                                       ⇒ the simulation does NOT reset
    Description,   // a state's description
    Tests,         // the list of test words
    Reset          // the whole document was replaced (undo/redo)
}

public sealed class DocumentChangedEventArgs(DocumentChangeKind kind) : EventArgs
{
    public DocumentChangeKind Kind { get; } = kind;
}

public sealed class DfaDocument
{
    public string Name { get; set; }
    public Dfa Dfa { get; }                                     // read only; not modified directly from the UI
    public IReadOnlyDictionary<string, Point2> Layout { get; }
    public IReadOnlyDictionary<string, string> Descriptions { get; }
    public IReadOnlyList<TestCase> Tests { get; }

    public event EventHandler<DocumentChangedEventArgs>? Changed;

    // states
    public OpResult AddState(Point2 position);                 // automatic name q0, q1… (first free)
    public OpResult AddState(string name, Point2 position);
    public OpResult RemoveState(string name);
    public OpResult RenameState(string oldName, string newName); // also updates Layout, Descriptions
    public OpResult MoveState(string name, Point2 position);     // called at the end of drag & drop, not during it
    public OpResult SetStart(string? name);
    public OpResult SetFinal(string name, bool isFinal);
    public OpResult SetDescription(string state, string? text);

    // alphabet
    public OpResult AddSymbol(string symbol);
    public OpResult AddSymbolClass(SymbolClass symbolClass);
    public OpResult RemoveSymbol(string symbol);

    // transitions
    public OpResult AddTransitions(string from, IEnumerable<string> symbols, string to);
    public OpResult RemoveTransition(string from, string symbol);
    public OpResult RemoveEdge(string from, string to);          // the whole arrow, with all its symbols

    // ED-08
    public OpResult CompleteWithTrap(Point2 position, string name = "trap");

    // tests
    public void SetTests(IEnumerable<TestCase> tests);

    // for undo/redo
    public DfaDocument Clone();
    public void RestoreFrom(DfaDocument snapshot);              // raises Changed(Reset)

    public static DfaDocument CreateEmpty(string name = "New automaton");
}
```

**Rules:**
- Every successful operation raises **exactly one** `Changed` event. A failed operation changes nothing and raises no event.
- A state with no position in `Layout` (e.g. from a hand-written file) gets an automatic position on load (on a circle).

---

## 7. `DocumentHistory`: undo / redo (ED-09)

```csharp
public sealed class DocumentHistory
{
    public DocumentHistory(DfaDocument document, int capacity = 100);
    public bool CanUndo { get; }
    public bool CanRedo { get; }
    public void Undo();
    public void Redo();
    public void Clear();                   // called when a new file is opened
    public event EventHandler? Changed;    // for enabling the buttons
}
```

It subscribes to `document.Changed` itself and saves a copy (`Clone()`) on every change, except those of kind `Reset`.

---

## 8. JSON, examples, batch testing, generator

```csharp
public sealed record LoadResult(DfaDocument? Document, IReadOnlyList<string> Errors)
{
    public bool Ok => Document is not null;
}

public static class DfaJson
{
    public static string Serialize(DfaDocument document);
    public static LoadResult Deserialize(string json);      // does not throw: errors come in LoadResult
    public static LoadResult LoadFile(string path);
    public static OpResult SaveFile(DfaDocument document, string path);
}

public sealed record ExampleInfo(string Id, string Title);

public static class ExampleCatalog
{
    public static IReadOnlyList<ExampleInfo> All { get; }
    public static DfaDocument Load(string id);              // from the resources embedded in Dfa.Core
}

public sealed record TestResult(TestCase Case, bool Actual, string Explanation)
{
    public bool Passed => Actual == Case.Expected;
}

public sealed record BatchResult(IReadOnlyList<TestResult> Results)
{
    public int Passed { get; }
    public int Total { get; }
    public string Summary { get; }                          // "9 of 10 correct"
}

public static class BatchTester
{
    public static BatchResult Run(Dfa dfa, IEnumerable<TestCase> tests);
}

public static class LanguageGenerator                       // SI-11, bonus
{
    // The first n accepted words, by length; for a class, its first character is used.
    public static IReadOnlyList<string> FirstAccepted(Dfa dfa, int count, int maxLength = 12);
}
```

### Example ids

| Id | Title | Owner |
|---|---|---|
| `even-a` | Even number of a | P1 |
| `starts-a` | Starts with a | P1 |
| `ends-ab` | Ends in ab | P1 |
| `contains-aba` | Contains aba | P1 |
| `div3` | Binary numbers divisible by 3 | P1 |
| `length-3` | Length a multiple of 3 | P1 |
| `license-plate` | License plate (RoPlate) | P3 |

File format: [02-json-format.md](02-json-format.md).
