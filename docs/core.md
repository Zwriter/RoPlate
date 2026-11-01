# The `Dfa.Core` engine

A short guide for whoever uses the engine (P2, P3) or changes it (P1). The exact signatures and messages are in [contract 1](plan/contracts/01-core-api.md); this page explains **how** to use it.

## 1. Layout

`Dfa.Core` is a class library with no reference to the UI. Namespaces follow the folders:

| Folder / namespace | Contents |
|---|---|
| `Model` | `Dfa` (the automaton), `TransitionKey`, `Edge`, `SymbolClass`, `OpResult` |
| `Simulation` | `DfaRun` (step-by-step execution), `BatchTester`, `LanguageGenerator` and their types |
| `Validation` | `DfaValidator`, `ValidationIssue`, `Severity` |
| `Documents` | `DfaDocument` (automaton + positions + descriptions + tests), `DocumentHistory` (undo/redo) |
| `Serialization` | `DfaJson`, `LoadResult` |
| `Examples` | `ExampleCatalog` and the embedded `.dfa.json` files |

`Dfa` is a partial class split by responsibility: `Dfa.cs` (states), `Dfa.Alphabet.cs` (Σ), `Dfa.Transitions.cs` (δ).

### Watch the `using` placement

The `Dfa` class has the same name as the root namespace `Dfa`. Write `using` directives **after** the `namespace` line, otherwise the compiler reads `Dfa` as the namespace (error CS0118):

```csharp
namespace Dfa.App.Services;

using Dfa.Core.Model;   // here, not above the namespace line
```

`.editorconfig` enforces this; `dotnet format` moves the directives for you.

## 2. The automaton: `Dfa`

### Building one in code

```csharp
var dfa = new Dfa();
dfa.AddState("q0");
dfa.AddState("q1");
dfa.AddSymbol("a");
dfa.AddSymbol("b");
dfa.AddTransitions("q0", ["a", "b"], "q1");   // the "a,b" arrow
dfa.SetStart("q0");
dfa.SetFinal("q1", true);
```

The UI does not work with `Dfa` directly, but through `DfaDocument`, which also holds the positions and announces changes.

### Rules

- **No exceptions for user mistakes.** Every change returns an `OpResult`; if `Ok` is `false`, `Error` holds a message ready to display, and the automaton **has not changed at all**.
- **Determinism comes from the structure.** δ is a `TransitionKey(state, symbol) → state` dictionary, so a (state, symbol) pair cannot have two targets. A second transition on the same symbol is refused (ED-04).
- **`AddTransitions` is all or nothing.** Every symbol is checked before anything is added; a single bad symbol means nothing is added.
- **Removing and renaming propagate.** `RemoveState` removes the transitions into and out of the state and drops it from finals/start. `RenameState` updates δ, the start state and the finals.
- **A used symbol cannot be removed.** Remove its transitions first.
- **Order is stable.** `States` and `Alphabet` keep insertion order; `Edges` (the arrows to draw) are ordered by state, and the symbols on each arrow by their order in Σ.
- `Clone()` gives a fully independent copy (used by undo/redo).

### Symbols and classes

Σ holds simple symbols (exactly 1 character) and class names (e.g. `L` = `A-Z`). A character read from the input becomes a symbol through `SymbolOf(c)`: classes first, then simple symbols, otherwise `null` (a foreign character). Classes must not share characters.

## 3. Implementation status

| Task | What | Status |
|---|---|---|
| C-00 | the solution, the contract stubs | done |
| C-01 | `Dfa`: the model and editing | done |
| C-02 | `SymbolClass`, `SymbolOf`, `AddSymbolClass` | stub (`NotImplementedException`) |
| C-03 … C-13 | run, validator, document, JSON, examples, batch, undo, trap state, generator | stub |

Until they are implemented, calls to the stubs throw `NotImplementedException`; work with a `Dfa` built in code, as above.

## 4. Commands

```bash
dotnet build DfaSimulator.slnx
dotnet test DfaSimulator.slnx
dotnet format DfaSimulator.slnx --verify-no-changes
```
