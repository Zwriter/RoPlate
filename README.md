<h1 align="center">RoPlate · DFA Simulator</h1>

<p align="center">
  <strong>Draw a finite automaton, feed it a word, and watch every step.</strong><br>
  A desktop DFA editor and step-by-step simulator for Windows, Linux and macOS, built with .NET 10 and Avalonia.
</p>

<p align="center">
  <a href="https://github.com/Zwriter/RoPlate/actions/workflows/ci.yaml"><img alt="CI" src="https://github.com/Zwriter/RoPlate/actions/workflows/ci.yaml/badge.svg"></a>
  <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10-512BD4">
  <img alt="Avalonia 12" src="https://img.shields.io/badge/UI-Avalonia%2012-8B44AC">
  <img alt="Coverage gate" src="https://img.shields.io/badge/core%20coverage-%E2%89%A570%25-brightgreen">
</p>

---

**RoPlate** is a tool for learning, teaching and debugging **deterministic finite automata (DFA)**. Build an automaton on a canvas, see its δ table fill in as you go, then run a word through it one symbol at a time. You can also have the tool explain why the word was accepted or rejected.

It ships with a real-world app built on the same engine: **RoPlate**, a Romanian license-plate checker. The plate format is a 14-state automaton, and the checker validates a plate as you type. It tells you what it expects next ("waiting for 3 letters") and can open the automaton so you can see exactly where an input got stuck.

> **Status:** the engine (`Dfa.Core`) is complete and fully tested. The editor, simulator and RoPlate screens are under active development; see the [team plan](public/docs/plan/README.md).

## Contents

- [Features](#features)
- [Quick start](#quick-start)
- [Using the engine from code](#using-the-engine-from-code)
- [The `.dfa.json` format](#the-dfajson-format)
- [Built-in examples](#built-in-examples)
- [Architecture](#architecture)
- [Contributing](#contributing)

## Features

**Editor**
- Draw states and transitions on a canvas; mark the start state and final states.
- **Symbol classes**: one arrow labelled `L` stands for every letter `A-Z`, so real-world alphabets stay readable.
- **Determinism by construction**: a second transition on the same symbol is refused with a clear message.
- A live **δ table**: missing cells are flagged with `—`, not only by colour.
- **Complete with trap state** in one click: every missing transition goes to a new non-final state, and the language does not change.
- Undo and redo for every edit, including moving a state.
- A validator warns about a missing start state, an empty alphabet, unreachable states, no final states, and missing transitions.

**Simulator**
- Step forward and back, run to the end, or play at your own speed.
- An input tape and an execution table that show what has been read and what is left at every step.
- **A verdict in plain English**, for example `REJECTED: symbol 'c' is not in the alphabet (position 3).`
- **Batch testing**: keep a list of words with their expected verdicts inside the file and check them all at once ("9 of 10 correct").
- **Guess mode**: predict the next state before each step and get a score at the end.
- **A word generator**: list the shortest words the automaton accepts.

**Safe by design**
- Malformed files never crash the app: every problem is reported, with the field that caused it.
- Files are saved atomically, so a failed save never corrupts the original.

## Quick start

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
git clone https://github.com/Zwriter/RoPlate.git
cd RoPlate
dotnet run --project src/Dfa.App
```

Run the test suite:

```bash
dotnet test DfaSimulator.slnx
```

## Using the engine from code

`Dfa.Core` is a plain class library with no UI dependency, so you can use it in a console app, a web service or your own tests.

```csharp
namespace MyApp;

using Dfa.Core.Model;       // put usings after the namespace: the class Dfa shares its name with the root namespace
using Dfa.Core.Simulation;

var dfa = new Dfa();
dfa.AddState("q0");
dfa.AddState("q1");
dfa.AddSymbol("a");
dfa.AddSymbol("b");
dfa.AddTransitions("q0", ["a"], "q1");
dfa.AddTransitions("q0", ["b"], "q0");
dfa.AddTransitions("q1", ["a"], "q0");
dfa.AddTransitions("q1", ["b"], "q1");
dfa.SetStart("q0");
dfa.SetFinal("q0", true);

dfa.Accepts("abba");                         // true: an even number of a

var run = new DfaRun(dfa, "aab");
run.RunToEnd();
Console.WriteLine(run.Explanation);          // ACCEPTED: the word ended in the final state q0.

LanguageGenerator.FirstAccepted(dfa, 4);     // "", "b", "aa", "bb"
```

User mistakes never throw. Every edit returns an `OpResult`, and a failed edit leaves the automaton untouched:

```csharp
var result = dfa.AddTransitions("q0", ["a"], "q0");
// result.Ok == false
// result.Error == "From q0 there is already a transition on 'a' to q1."
```

The full API is in the [engine contract](public/docs/plan/contracts/01-core-api.md), and the [engine guide](public/docs/core.md) explains how to use it.

## The `.dfa.json` format

Automata are stored as human-readable JSON that you can write and review by hand. A file keeps the canvas layout, the state descriptions and the test words alongside the automaton.

```json
{
  "format": "dfa-simulator",
  "version": 1,
  "name": "Even number of a",
  "alphabet": ["a", "b"],
  "symbolClasses": [],
  "states": ["q0", "q1"],
  "start": "q0",
  "finals": ["q0"],
  "transitions": [
    { "from": "q0", "symbol": "a", "to": "q1" },
    { "from": "q0", "symbol": "b", "to": "q0" },
    { "from": "q1", "symbol": "a", "to": "q0" },
    { "from": "q1", "symbol": "b", "to": "q1" }
  ],
  "layout": { "q0": { "x": 200, "y": 200 }, "q1": { "x": 450, "y": 200 } },
  "descriptions": { "q0": "seen an even number of a", "q1": "seen an odd number of a" },
  "tests": [
    { "word": "abba", "expected": true },
    { "word": "bab", "expected": false }
  ]
}
```

The complete specification is in [contract 2](public/docs/plan/contracts/02-json-format.md).

## Built-in examples

Open any of these from the **Examples** menu. Each one comes with a position and a description for every state, plus test words that run in CI.

| Example | Language |
|---|---|
| `even-a` | words with an even number of `a` |
| `starts-a` | words that start with `a` (with a trap state) |
| `ends-ab` | words that end in `ab` |
| `contains-aba` | words that contain `aba` |
| `div3` | binary numbers divisible by 3 |
| `length-3` | words whose length is a multiple of 3 |
| `license-plate` | Romanian license plates (RoPlate), coming soon |

To add your own, drop a `{id}.dfa.json` file into `src/Dfa.Core/Examples/`. It is embedded and tested automatically.

## Architecture

```
src/
├─ Dfa.Core/     the engine: model, simulation, validation, documents, JSON, examples (no UI)
└─ Dfa.App/      the Avalonia desktop app (MVVM with CommunityToolkit.Mvvm)
tests/
└─ Dfa.Tests/    xUnit + Shouldly
public/
├─ docs/
│  ├─ core.md    the engine guide
│  └─ plan/      the team plan and the contracts between the parts
└─ icons/        the app icon (SVG and PNG sizes)
```

- **Dependencies flow one way.** `Dfa.App` and `Dfa.Tests` reference `Dfa.Core`, and the engine never references the UI.
- **Determinism is structural.** δ is a `(state, symbol) → state` dictionary, so an automaton cannot have two targets for the same pair.
- **One document, one source of truth.** The UI changes the automaton only through `DfaDocument`, which raises exactly one change event per successful edit. That event is what drives the canvas, the δ table, undo and redo, and the simulation reset.

## Contributing

Contributions are welcome. Before opening a pull request, make sure these commands all succeed:

```bash
dotnet build DfaSimulator.slnx
dotnet test DfaSimulator.slnx
dotnet format DfaSimulator.slnx --verify-no-changes
```

The project rules are in the [constitution](public/docs/constitution.md). The main ones:

- new engine behaviour comes with tests;
- `Dfa.Core` line coverage stays at or above 70% (CI enforces it);
- source files stay below 250 lines of code;
- all user-facing text is in English;
- the public API and messages follow the [contracts](public/docs/plan/contracts/), and changing a contract needs a reviewed PR.
