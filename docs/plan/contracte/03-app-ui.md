# Contractul 3: interfața grafică (`Dfa.App`)

**Proprietari:** P2 (sesiune, navigare, dialoguri, fereastră, `DfaCanvas`), P1 (panoul tabel δ), P3 (panoul de simulare, banda, RoPlate).

Acest contract definește **cum se leagă panourile între ele**, ca fiecare să-și poată construi partea separat și ca la final totul să se potrivească.

---

## 1. Decizii tehnice comune

| Aspect | Decizie |
|---|---|
| Platformă | .NET 10, **Avalonia** (Windows, Linux, macOS). Fișierele de interfață sunt `.axaml`; proprietățile controalelor proprii sunt `StyledProperty` |
| Binding-uri | compilate: fiecare view are `x:DataType` setat pe ViewModel-ul lui |
| Dialoguri | asincrone (în Avalonia ferestrele modale și selectarea fișierelor întorc `Task`); comenzile care le folosesc sunt `[RelayCommand]` pe metode `async Task` |
| MVVM | `CommunityToolkit.Mvvm`: `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]` |
| Fără container DI | obiectele se creează manual în `MainViewModel` (secțiunea 7) |
| Limba | toate textele vizibile sunt în română |
| Code-behind | doar pentru desen și mouse (în `DfaCanvas`, `InputTape`); restul în ViewModel |

---

## 2. Aspectul ferestrei principale

```
┌──────────────────────────────────────────────────────────────────────┐
│ Fișier ▾  Exemple ▾  Editare ▾   [↶] [↷]   [Completează cu capcană]  │  ← meniu (P2)
├───────────────────────────────────────────┬──────────────────────────┤
│                                           │ [ Editare | Simulare |   │
│                                           │   Teste ]                │
│            DfaCanvas (P2)                 │                          │
│     (desenul automatului; în modul        │  Editare:  alfabet,      │
│      Simulare arată starea curentă)       │            clase (P2)    │
│                                           │            tabel δ (P1)  │
│                                           │  Simulare: SimulatorPanel│
│                                           │            (P3)          │
│                                           │  Teste:    BatchTestPanel│
│                                           │            (P3)          │
├───────────────────────────────────────────┴──────────────────────────┤
│ bara de stare: mesaje / erori de validare (P2)                       │
└──────────────────────────────────────────────────────────────────────┘
Fereastră separată:  RoPlateWindow (P3), deschisă din meniul „Aplicații ▸ RoPlate”
```

Canvasul este **unul singur** și este folosit atât la editare, cât și la simulare.

---

## 3. `DocumentSession`: documentul deschis (P2)

Un singur obiect, comun tuturor panourilor. Toate panourile citesc automatul de aici.

```csharp
namespace Dfa.App.Services;

public sealed partial class DocumentSession : ObservableObject
{
    [ObservableProperty] private DfaDocument document = DfaDocument.CreateEmpty();
    [ObservableProperty] private string? filePath;          // null = nesalvat încă
    [ObservableProperty] private bool isDirty;              // modificări nesalvate (afișat cu * în titlu)

    public DocumentHistory History { get; }                 // undo/redo pentru documentul curent

    // Ridicat când documentul a fost ÎNLOCUIT (New / Open / Exemple)
    public event EventHandler? DocumentReplaced;

    // Retransmite Document.Changed pentru documentul curent, ca să nu vă reabonați la fiecare înlocuire
    public event EventHandler<DocumentChangedEventArgs>? DocumentEdited;

    public void Replace(DfaDocument document, string? filePath);   // golește History, ridică DocumentReplaced
}
```

**Regula SI-07:** panoul de simulare ascultă `DocumentEdited` și se **resetează** când `Kind` este `Structure` sau `Reset`, afișând mesajul `Automatul a fost modificat: simularea a fost resetată.` Mutarea stărilor (`Layout`) **nu** resetează simularea.

---

## 4. `INavigationService` și `IDialogService` (P2)

```csharp
namespace Dfa.App.Services;

public enum MainTab { Editor, Simulator, Tests }

public interface INavigationService
{
    void ShowTab(MainTab tab);
    // „Arată automatul” din RoPlate: încarcă o COPIE a documentului și pornește simularea pe input
    void OpenInSimulator(DfaDocument document, string input);
    void ShowRoPlate();
}

public interface IDialogService
{
    Task ShowErrorAsync(string message);
    Task ShowErrorsAsync(string title, IEnumerable<string> messages);
    Task ShowInfoAsync(string message);
    Task<bool> ConfirmAsync(string message);                                   // Da / Nu
    Task<string?> AskTextAsync(string title, string prompt, string initial = ""); // null = Anulare
    Task<string?> PickOpenFileAsync();                                          // filtru *.dfa.json
    Task<string?> PickSaveFileAsync(string suggestedName);
}
```

Pentru teste și pentru lucrul în paralel, P2 pune la dispoziție din prima zi implementări simple (o fereastră de mesaj proprie, pentru că Avalonia nu are `MessageBox` încorporat; fișierele se aleg cu `TopLevel.StorageProvider`), ca P3 să le poată folosi imediat.

---

## 5. `DfaCanvas`: desenul (P2, folosit de P3)

`UserControl` în `Dfa.App.Controls`. Desenează `Document` și se redesenează singur la `Document.Changed`.

### Proprietăți (StyledProperty, legabile din AXAML)

| Proprietate | Tip | Rol |
|---|---|---|
| `Document` | `DfaDocument?` | ce se desenează |
| `Mode` | `CanvasMode` | `Edit`, `ReadOnly`, `PickState` |
| `HighlightedState` | `string?` | starea curentă din simulare |
| `HighlightKind` | `HighlightKind` | `None`, `Current`, `Accepted`, `Rejected` (culoarea stării evidențiate) |
| `HighlightedEdge` | `EdgeRef?` | săgeata tocmai parcursă |
| `SelectedState` | `string?` | starea selectată în editor (two-way: `defaultBindingMode: BindingMode.TwoWay`) |

```csharp
namespace Dfa.App.Controls;

public enum CanvasMode { Edit, ReadOnly, PickState }
public enum HighlightKind { None, Current, Accepted, Rejected }
public sealed record EdgeRef(string From, string To);      // o săgeată = o pereche (From, To), ca Dfa.Edges
```

### Evenimente

| Eveniment | Când | Folosit de |
|---|---|---|
| `StateClicked(string state)` | clic pe o stare, în orice mod | P3: modul „Ghicește” (SI-09), cu `Mode = PickState` |
| `StatusMessage(string text)` | eroare/info la editare (ex. mesajul ED-04) | P2: bara de stare |

### Comportament pe moduri

| Mod | Ce se poate face |
|---|---|
| `Edit` | tot ce cere ED-01…ED-04 (adăugare, mutare, ștergere, tranziții, meniu contextual). Simularea poate fi activă: editarea o resetează (SI-07) |
| `ReadOnly` | doar vizualizare (RoPlate: „Arată automatul”, export) |
| `PickState` | clicul pe o stare doar ridică `StateClicked`; nicio editare |

### Convenții vizuale (cerința de claritate: **nu doar culoare**)

| Element | Desen |
|---|---|
| stare | cerc cu raza 28 px, nume în centru |
| stare finală | cerc dublu (al doilea cerc, raza 22 px) |
| stare inițială | săgeată scurtă din stânga, cu textul „start” |
| stare curentă (`Current`) | contur **gros (4 px)** + culoarea `Brush.StateCurrent` |
| acceptat / respins | contur gros + `Brush.Accepted` / `Brush.Rejected` + simbol ✓ / ✗ lângă stare |
| ultima săgeată | linie **groasă** + `Brush.EdgeLast` |
| descriere stare (SI-10) | text mic sub cerc, gri |

### Stiluri comune (`Themes/Styles.axaml`, P2)

Cheile de resurse folosite de toți: `Brush.StateCurrent`, `Brush.Accepted`, `Brush.Rejected`, `Brush.EdgeLast`, `Brush.MissingCell` (celule goale în tabelul δ), `Brush.TapeRead` (simboluri citite, estompate), `Brush.TapeCursor`.

---

## 6. Panourile

Fiecare panou = un `UserControl` + ViewModel-ul lui. Constructorul ViewModel-ului primește doar ce are nevoie.

| Panou | Proprietar | ViewModel | Constructor |
|---|---|---|---|
| `EditorPanel` (alfabet, clase, descrieri) | P2 | `EditorViewModel` | `(DocumentSession, IDialogService)` |
| `TransitionTablePanel` (tabel δ, ED-06) | P1 | `TransitionTableViewModel` | `(DocumentSession)` |
| `SimulatorPanel` (SI-01…SI-07, SI-09) | P3 | `SimulatorViewModel` | `(DocumentSession, IDialogService)` |
| `BatchTestPanel` (SI-08) | P3 | `BatchTestViewModel` | `(DocumentSession)` |
| `RoPlateWindow` (A1) | P3 | `RoPlateViewModel` | `(INavigationService)` |

### Ce expune `SimulatorViewModel` pentru canvas (P3 → P2)

Fereastra principală (P2) leagă canvasul de aceste proprietăți:

```csharp
public sealed partial class SimulatorViewModel : ObservableObject
{
    public string? CurrentState { get; }          // -> DfaCanvas.HighlightedState
    public HighlightKind HighlightKind { get; }   // -> DfaCanvas.HighlightKind
    public EdgeRef? LastEdge { get; }             // -> DfaCanvas.HighlightedEdge
    public CanvasMode RequestedCanvasMode { get; }// Edit în mod normal, PickState în „Ghicește”
    public void OnStateClicked(string state);     // apelat de fereastră la DfaCanvas.StateClicked

    public void Start(string input);              // folosit și de INavigationService.OpenInSimulator
    public void Stop();                           // la ieșirea din tab: HighlightedState = null
}
```

Când tab-ul activ nu este „Simulare”, fereastra pune `HighlightedState = null` și `Mode = Edit`.

### `InputTape` (P3)

`UserControl` în `Dfa.App.Controls`, folosit doar de P3:

| Proprietate | Tip | Rol |
|---|---|---|
| `Text` | `string` | cuvântul |
| `CursorPosition` | `int` | indexul simbolului următor (= `Steps[Position-1].Position + 1`, sau 0) |
| `ErrorPosition` | `int?` | căsuța marcată cu roșu la blocaj |

---

## 7. Cum se asamblează (P2, în `MainViewModel`)

```csharp
var dialogs    = new DialogService(owner);   // owner = MainWindow (TopLevel): pentru ShowDialog și StorageProvider
var session    = new DocumentSession();
var navigation = new NavigationService(this, session);

Editor     = new EditorViewModel(session, dialogs);
Table      = new TransitionTableViewModel(session);
Simulator  = new SimulatorViewModel(session, dialogs);
BatchTests = new BatchTestViewModel(session);
```

Până când un panou e gata, `MainWindow` afișează în locul lui un `TextBlock` „În lucru”. Fiecare își poate testa panoul și separat, punându-l temporar într-o fereastră de test (`DevWindow`, care nu intră în `main`).
