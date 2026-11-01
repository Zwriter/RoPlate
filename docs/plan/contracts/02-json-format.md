# Contract 2: the `.dfa.json` file format

**Owner:** P1 (reading/writing in `DfaJson`). **Used by:** everyone. P3 writes `license-plate.dfa.json` by hand from week 1, so the format must be fixed from day one.

File extension: `.dfa.json`. Encoding: UTF-8.

---

## 1. Structure

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
  "layout": {
    "q0": { "x": 210, "y": 120 },
    "q1": { "x": 430, "y": 120 }
  },
  "descriptions": {
    "q0": "seen an even number of a",
    "q1": "seen an odd number of a"
  },
  "tests": [
    { "word": "", "expected": true },
    { "word": "abba", "expected": true },
    { "word": "bab", "expected": false }
  ]
}
```

| Field | Type | Required | Notes |
|---|---|---|---|
| `format` | string | yes | always `"dfa-simulator"`; any other value ⇒ error |
| `version` | int | yes | `1`. If the format changes, the version goes up |
| `name` | string | no | default: the file name |
| `alphabet` | string[] | yes | Σ: simple symbols (1 character) and class names |
| `symbolClasses` | object[] | no | `{ "name": "C", "chars": "0123456789" }` |
| `states` | string[] | yes | order matters (it is the row order in the δ table) |
| `start` | string / null | no | `null` or missing = no start state (the validator reports an error at simulation time, but the file loads) |
| `finals` | string[] | no | default `[]` |
| `transitions` | object[] | no | **one object per symbol**, even if they share an arrow in the drawing |
| `layout` | object | no | coordinates of the circle's **center**, in pixels |
| `descriptions` | object | no | what the state "remembers" (SI-10); used by RoPlate for its messages |
| `tests` | object[] | no | `word: ""` means ε |

Unknown fields are **ignored** (not an error).

---

## 2. Symbol classes

`chars` holds **every character, written out** (no ranges like `A-Z`), so anyone can read the file easily:

```json
"alphabet": ["L", "C", "_"],
"symbolClasses": [
  { "name": "L", "chars": "ABCDEFGHIJKLMNOPQRSTUVWXYZ" },
  { "name": "C", "chars": "0123456789" },
  { "name": "_", "chars": " " }
]
```

Every `name` in `symbolClasses` must also appear in `alphabet`.

---

## 3. What `DfaJson.Deserialize` does with a bad file

It does not throw and does not freeze the app (the robustness requirement). It returns a `LoadResult` with the list of errors.

| Problem | Result | Message |
|---|---|---|
| not valid JSON | error | `The file is not valid JSON (line {n}).` |
| wrong / missing `format` | error | `The file is not an automaton (missing "format": "dfa-simulator").` |
| `version` newer than known | error | `The file was created with a newer version ({v}).` |
| duplicate state in `states` | error | `State '{s}' appears twice.` |
| a transition uses a missing state | error | `Transition {from} –{symbol}→ {to} uses a state that does not exist.` |
| a transition uses a symbol outside Σ | error | `Symbol '{symbol}' is not in the alphabet.` |
| two transitions on the same `(from, symbol)` | error | `From {from} there is already a transition on '{symbol}'.` |
| `start` or `finals` with a missing state | error | `State '{s}' does not exist.` |
| state without `layout` | **accepted** | gets an automatic position |
| `layout`/`descriptions` for a missing state | **accepted** | the entry is ignored |

If there is at least one error, `Document` is `null` and the UI shows all the errors.

---

## 4. What `DfaJson.Serialize` does

- 2-space indentation (files diff easily in Git).
- Field order: as in the example in section 1.
- States in `Dfa.States` order; transitions sorted by the order of the `from` state, then by the symbol's order in `alphabet`.
- Coordinates rounded to integers.
- **The core property, tested by P1:** `Deserialize(Serialize(doc))` gives an equivalent document.

---

## 5. Full example: the RoPlate automaton (skeleton)

P3 writes the complete file (task R-01), following [../../theory/05-roplate-automaton.md](../../theory/05-roplate-automaton.md).

```json
{
  "format": "dfa-simulator",
  "version": 1,
  "name": "License plate",
  "alphabet": ["L", "C", "_"],
  "symbolClasses": [
    { "name": "L", "chars": "ABCDEFGHIJKLMNOPQRSTUVWXYZ" },
    { "name": "C", "chars": "0123456789" },
    { "name": "_", "chars": " " }
  ],
  "states": ["q0", "q1", "q2", "B0", "B1", "B2", "B3", "J0", "J1", "J2", "S", "T1", "T2", "T3"],
  "start": "q0",
  "finals": ["T3"],
  "transitions": [
    { "from": "q0", "symbol": "L", "to": "q1" },
    { "from": "q1", "symbol": "L", "to": "q2" },
    { "from": "q1", "symbol": "_", "to": "B0" }
  ],
  "descriptions": {
    "q0": "Enter the county code",
    "S": "Waiting for 3 letters",
    "T3": "Valid plate"
  },
  "tests": [
    { "word": "B 123 ABC", "expected": true },
    { "word": "CJ 123 ABC", "expected": false }
  ]
}
```
