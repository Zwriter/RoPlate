# Contractul 1: API-ul motorului (`Dfa.Core`)

**Proprietar:** P1. **Folosit de:** P2 (editor, canvas), P3 (simulare, RoPlate), testele.

`Dfa.Core` este un proiect **class library**, fără nicio referință la Avalonia sau la alt framework de interfață. Namespace-urile urmează folderele: `Dfa.Core.Model` (`Dfa`, `TransitionKey`, `Edge`, `SymbolClass`, `OpResult`), `Dfa.Core.Simulation` (`DfaRun`, `DfaStep`, `StepOutcome`, `Verdict`, `RejectReason`, `DfaExtensions`, `BatchTester`, `BatchResult`, `TestResult`, `LanguageGenerator`), `Dfa.Core.Validation` (`DfaValidator`, `ValidationIssue`, `Severity`), `Dfa.Core.Documents` (`DfaDocument`, `DocumentHistory`, `DocumentChangeKind`, `DocumentChangedEventArgs`, `Point2`, `TestCase`), `Dfa.Core.Serialization` (`DfaJson`, `LoadResult`), `Dfa.Core.Examples` (`ExampleCatalog`, `ExampleInfo`). Directivele `using` se scriu **după** linia `namespace …;`, altfel `Dfa` este interpretat ca namespace-ul rădăcină (eroarea CS0118). Proiectul are `<Nullable>enable</Nullable>`.

Mai jos sunt **doar semnăturile publice** și regulile de comportament. Implementarea e liberă.

---

## 1. Rezultatul unei operații

Operațiile care pot eșua din vina utilizatorului **nu aruncă excepții**: returnează un `OpResult` cu mesajul în română, gata de afișat.

```csharp
public sealed record OpResult(bool Ok, string? Error = null)
{
    public static OpResult Success { get; } = new(true);
    public static OpResult Fail(string message) => new(false, message);
}
```

Excepțiile rămân doar pentru erori de programare (de ex. `null` unde nu e permis).

---

## 2. Simboluri și clase de simboluri

```csharp
public sealed record SymbolClass(string Name, string Characters)
{
    public bool Contains(char c);
    // Construiește dintr-o descriere cu intervale: FromRanges("L", "A-Z"), FromRanges("C", "0-9")
    public static SymbolClass FromRanges(string name, string ranges);
}
```

**Reguli pentru alfabet (Σ):**
- Σ este o listă de **nume de simboluri** (`string`).
- Un **simbol simplu** are exact 1 caracter (`"a"`, `"0"`).
- O **clasă** are orice nume (`"L"`, `"C"`, `"_"`) și o listă de caractere. Numele clasei apare în Σ.
- Clasele **nu au voie să aibă caractere comune** între ele și nici cu simbolurile simple (altfel se pierde determinismul).
- **Maparea unui caracter de intrare** (`Dfa.SymbolOf(char c)`):
  1. dacă o clasă conține `c` ⇒ numele clasei;
  2. altfel, dacă `c.ToString()` este în Σ și **nu** este numele unei clase ⇒ `c.ToString()`;
  3. altfel ⇒ `null` (caracterul nu aparține alfabetului).

Exemplu: Σ = {L, C, _}, `_` = clasă cu caracterul spațiu. Atunci `SymbolOf(' ') == "_"`, iar `SymbolOf('_') == null`.

---

## 3. `Dfa`: automatul

```csharp
public readonly record struct TransitionKey(string State, string Symbol);

// O săgeată de pe desen: toate simbolurile care duc din From în To
public sealed record Edge(string From, string To, IReadOnlyList<string> Symbols);

public sealed class Dfa
{
    // ---- citire ----
    public IReadOnlyList<string> States { get; }                 // în ordinea adăugării
    public IReadOnlyList<string> Alphabet { get; }               // Σ
    public IReadOnlyList<SymbolClass> SymbolClasses { get; }
    public string? StartState { get; }                           // null = nu e setată
    public IReadOnlySet<string> FinalStates { get; }
    public IReadOnlyDictionary<TransitionKey, string> Transitions { get; }   // δ

    public string? Next(string state, string symbol);            // δ(state, symbol) sau null
    public string? SymbolOf(char c);                             // vezi secțiunea 2
    public IReadOnlyList<Edge> Edges { get; }                    // tranzițiile grupate pe (From, To)
    public bool IsFinal(string state);

    // ---- modificare (folosite de DfaDocument și de teste) ----
    public OpResult AddState(string name);
    public OpResult RemoveState(string name);                    // șterge și tranzițiile care o ating
    public OpResult RenameState(string oldName, string newName);
    public OpResult SetStart(string? name);
    public OpResult SetFinal(string name, bool isFinal);
    public OpResult AddSymbol(string symbol);                    // simbol simplu, 1 caracter
    public OpResult AddSymbolClass(SymbolClass symbolClass);
    public OpResult RemoveSymbol(string symbol);                 // eșuează dacă e folosit în tranziții
    public OpResult AddTransition(string from, string symbol, string to);
    public OpResult AddTransitions(string from, IEnumerable<string> symbols, string to); // „a,b”: totul sau nimic
    public OpResult RemoveTransition(string from, string symbol);

    public Dfa Clone();
}
```

**De ce dicționar pentru δ:** cheia `(stare, simbol)` nu poate apărea de două ori ⇒ determinismul e garantat de structură (ED-04).

### Mesajele de eroare (exact acestea, pentru consistență în UI)

| Situație | Mesaj |
|---|---|
| stare existentă | `Există deja o stare numită «{name}».` |
| stare inexistentă | `Starea «{name}» nu există.` |
| nume gol | `Numele stării nu poate fi gol.` |
| tranziție duplicat (ED-04) | `Din {from} există deja o tranziție pe «{symbol}» către {to}.` |
| simbol în afara lui Σ (ED-05) | `Simbolul «{symbol}» nu aparține alfabetului.` |
| simbol duplicat în Σ | `Simbolul «{symbol}» există deja în alfabet.` |
| simbol simplu cu mai mult de un caracter | `Un simbol simplu trebuie să aibă exact un caracter. Pentru grupuri folosiți o clasă de simboluri.` |
| clase care se suprapun | `Caracterul «{c}» aparține deja clasei «{other}».` |
| ștergere simbol folosit | `Simbolul «{symbol}» este folosit în {n} tranziții. Ștergeți-le întâi.` |

---

## 4. `DfaRun`: o execuție (tabelul de execuție)

```csharp
public enum StepOutcome { Moved, NoTransition, NotInAlphabet }

public sealed record DfaStep(
    int Number,            // 1, 2, 3… (pasul 0 = configurația inițială, NU e în listă)
    int Position,          // indexul caracterului în cuvânt, de la 0
    char Character,        // caracterul citit
    string? Symbol,        // simbolul după mapare (ex. 'K' -> "L"); null dacă NotInAlphabet
    string From,           // starea înainte
    string? To,            // starea după; null dacă s-a blocat
    StepOutcome Outcome);

public enum Verdict { InProgress, Accepted, Rejected }
public enum RejectReason { None, NoStartState, EndedInNonFinal, NoTransition, NotInAlphabet }

public sealed class DfaRun
{
    public DfaRun(Dfa dfa, string input);          // input "" = ε

    public Dfa Dfa { get; }
    public string Input { get; }
    public IReadOnlyList<DfaStep> Steps { get; }   // TOȚI pașii, calculați în constructor
    public int Position { get; }                   // câți pași sunt „făcuți”: 0..Steps.Count
    public bool IsAtStart { get; }                 // Position == 0
    public bool IsAtEnd { get; }                   // Position == Steps.Count
    public bool IsBlocked { get; }                 // ultimul pas făcut a fost NoTransition / NotInAlphabet

    public string? CurrentState { get; }           // la blocaj rămâne starea din care s-a blocat; null fără stare inițială
    public DfaStep? LastStep { get; }              // Steps[Position - 1] sau null
    public string RemainingInput { get; }          // ce a rămas de citit după pasul curent

    public Verdict Verdict { get; }                // la poziția curentă: InProgress până la final
    public RejectReason Reason { get; }
    public string Explanation { get; }             // mesajul SI-06, vezi tabelul de mai jos

    public Verdict FinalVerdict { get; }           // verdictul pentru tot cuvântul, indiferent de Position

    public bool StepForward();                     // false dacă e deja la final
    public bool StepBack();                        // false dacă e deja la început
    public void Reset();                           // Position = 0
    public void RunToEnd();                        // Position = Steps.Count

    // Pentru RoPlate (rulare incrementală, câte un caracter):
    public void Append(char c);                    // adaugă cel mult un pas nou; Position ajunge la final
    public void RemoveLast();                      // inversul lui Append (Backspace)

    public event EventHandler? Changed;            // după orice schimbare de Position / Input
}

public static class DfaExtensions
{
    public static bool Accepts(this Dfa dfa, string input);   // scurtătură pentru teste
}
```

**Reguli:**
- Un AFD face cel mult `|input|` pași. După un blocaj **nu se mai fac pași**; ultimul element din `Steps` este pasul de blocaj.
- Dacă `StartState == null` ⇒ `Steps` e gol, `Verdict = Rejected`, `Reason = NoStartState`.
- Pentru ε: `Steps` e gol, iar verdictul depinde doar de faptul că starea inițială e finală.
- „Rest de citit” pentru rândul unui pas din tabel: `Input[(step.Position + 1)..]`.

### Mesajele verdictului (`Explanation`)

Pozițiile din mesaje sunt **de la 1** (pentru utilizator).

| Situație | Mesaj |
|---|---|
| în desfășurare | `Pasul {Position} din {Steps.Count}.` |
| acceptat | `ACCEPTAT: cuvântul s-a terminat în starea finală {state}.` |
| stare nefinală | `RESPINS: cuvântul s-a terminat în {state}, care nu este finală.` |
| tranziție lipsă | `RESPINS: din {state} nu există tranziție pe «{c}» (poziția {pos}).` |
| tranziție lipsă, cu clasă | `RESPINS: din {state} nu există tranziție pe «{c}» (clasa {symbol}, poziția {pos}).` |
| simbol străin | `RESPINS: simbolul «{c}» nu aparține alfabetului (poziția {pos}).` |
| fără stare inițială | `RESPINS: automatul nu are stare inițială.` |

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
    public static bool CanSimulate(IReadOnlyList<ValidationIssue> issues);   // fără Error
}
```

| Code | Severitate | Mesaj |
|---|---|---|
| `NO_START` | Error | `Automatul nu are stare inițială.` |
| `NO_STATES` | Error | `Automatul nu are nicio stare.` |
| `EMPTY_ALPHABET` | Error | `Alfabetul este gol.` |
| `CLASS_OVERLAP` | Error | `Clasele «{a}» și «{b}» au caractere comune.` |
| `UNREACHABLE` | Warning | `Starea {state} nu poate fi atinsă din starea inițială.` |
| `NO_FINAL` | Warning | `Automatul nu are stări finale: va respinge orice cuvânt.` |
| `MISSING_TRANSITION` | Info | `Din {state} lipsește tranziția pe «{symbol}».` |

Simularea pornește doar dacă nu există `Error` (pasul 1 din fluxul de simulare).

---

## 6. `DfaDocument`: ce se salvează și se editează

Interfața **modifică automatul doar prin `DfaDocument`**, pentru că documentul ține și pozițiile, descrierile, testele, și anunță schimbările.

```csharp
public readonly record struct Point2(double X, double Y);
public sealed record TestCase(string Word, bool Expected);   // Word "" = ε

public enum DocumentChangeKind
{
    Structure,     // s-a schimbat limbajul: stări, Σ, δ, start, finale  ⇒ simularea se resetează (SI-07)
    Layout,        // doar poziții                                         ⇒ simularea NU se resetează
    Description,   // descrierea unei stări
    Tests,         // lista de cuvinte de test
    Reset          // tot documentul a fost înlocuit (undo/redo)
}

public sealed class DocumentChangedEventArgs(DocumentChangeKind kind) : EventArgs
{
    public DocumentChangeKind Kind { get; } = kind;
}

public sealed class DfaDocument
{
    public string Name { get; set; }
    public Dfa Dfa { get; }                                     // a se citi; nu se modifică direct din UI
    public IReadOnlyDictionary<string, Point2> Layout { get; }
    public IReadOnlyDictionary<string, string> Descriptions { get; }
    public IReadOnlyList<TestCase> Tests { get; }

    public event EventHandler<DocumentChangedEventArgs>? Changed;

    // stări
    public OpResult AddState(Point2 position);                 // nume automat q0, q1… (primul liber)
    public OpResult AddState(string name, Point2 position);
    public OpResult RemoveState(string name);
    public OpResult RenameState(string oldName, string newName); // actualizează și Layout, Descriptions
    public OpResult MoveState(string name, Point2 position);     // apelat la sfârșitul drag & drop, nu în timpul lui
    public OpResult SetStart(string? name);
    public OpResult SetFinal(string name, bool isFinal);
    public OpResult SetDescription(string state, string? text);

    // alfabet
    public OpResult AddSymbol(string symbol);
    public OpResult AddSymbolClass(SymbolClass symbolClass);
    public OpResult RemoveSymbol(string symbol);

    // tranziții
    public OpResult AddTransitions(string from, IEnumerable<string> symbols, string to);
    public OpResult RemoveTransition(string from, string symbol);
    public OpResult RemoveEdge(string from, string to);          // toată săgeata, cu toate simbolurile

    // ED-08
    public OpResult CompleteWithTrap(Point2 position, string name = "capcană");

    // teste
    public void SetTests(IEnumerable<TestCase> tests);

    // pentru undo/redo
    public DfaDocument Clone();
    public void RestoreFrom(DfaDocument snapshot);              // ridică Changed(Reset)

    public static DfaDocument CreateEmpty(string name = "Automat nou");
}
```

**Reguli:**
- Fiecare operație reușită ridică **exact un** eveniment `Changed`. O operație eșuată nu modifică nimic și nu ridică evenimentul.
- O stare fără poziție în `Layout` (de ex. dintr-un fișier scris de mână) primește o poziție automată la încărcare (pe un cerc).

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
    public void Clear();                   // apelat la deschiderea unui fișier nou
    public event EventHandler? Changed;    // pentru activarea butoanelor
}
```

Se abonează singur la `document.Changed` și salvează câte o copie (`Clone()`) la fiecare schimbare, mai puțin cele de tip `Reset`.

---

## 8. JSON, exemple, testare în lot, generator

```csharp
public sealed record LoadResult(DfaDocument? Document, IReadOnlyList<string> Errors)
{
    public bool Ok => Document is not null;
}

public static class DfaJson
{
    public static string Serialize(DfaDocument document);
    public static LoadResult Deserialize(string json);      // nu aruncă excepții: erorile vin în LoadResult
    public static LoadResult LoadFile(string path);
    public static OpResult SaveFile(DfaDocument document, string path);
}

public sealed record ExampleInfo(string Id, string Title);

public static class ExampleCatalog
{
    public static IReadOnlyList<ExampleInfo> All { get; }
    public static DfaDocument Load(string id);              // din resursele încorporate în Dfa.Core
}

public sealed record TestResult(TestCase Case, bool Actual, string Explanation)
{
    public bool Passed => Actual == Case.Expected;
}

public sealed record BatchResult(IReadOnlyList<TestResult> Results)
{
    public int Passed { get; }
    public int Total { get; }
    public string Summary { get; }                          // „9 din 10 corecte”
}

public static class BatchTester
{
    public static BatchResult Run(Dfa dfa, IEnumerable<TestCase> tests);
}

public static class LanguageGenerator                       // SI-11, bonus
{
    // Primele n cuvinte acceptate, în ordinea lungimii; pentru o clasă se folosește primul ei caracter.
    public static IReadOnlyList<string> FirstAccepted(Dfa dfa, int count, int maxLength = 12);
}
```

### Id-urile exemplelor

| Id | Titlu | Proprietar |
|---|---|---|
| `par-a` | Număr par de a | P1 |
| `incepe-a` | Începe cu a | P1 |
| `termina-ab` | Se termină în ab | P1 |
| `contine-aba` | Conține aba | P1 |
| `div3` | Numere binare divizibile cu 3 | P1 |
| `lungime-3` | Lungime multiplu de 3 | P1 |
| `nr-inmatriculare` | Număr de înmatriculare (RoPlate) | P3 |

Formatul fișierelor: [02-format-json.md](02-format-json.md).
