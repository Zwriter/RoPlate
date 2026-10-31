# Sarcinile P2: fereastra principală, desenul și editorul

**Deții:** `App.xaml`, `MainWindow.xaml`, `Services/**`, `Themes/**`, `Controls/DfaCanvas/**`, `Views/Editor/**`.
**Contract pe care îl implementezi:** [03-app-ui.md](contracte/03-app-ui.md) (sesiune, navigare, dialoguri, canvas). **Folosești:** [01-core-api.md](contracte/01-core-api.md).

Până termină P1 motorul (săpt. 1), construiește documentele de test direct în cod; după C-07 folosește `ExampleCatalog`.

---

### E-00 · Stub-urile UI ⚠ blochează pe P1 (C-11) și P3
- **Săpt.:** 1 (ziua 2–3) · **Depinde de:** C-00
- Scrie `DocumentSession`, `INavigationService`, `IDialogService` (+ `DialogService` simplu cu `MessageBox`), enum-urile și `EdgeRef` din contractul 3, și un `DfaCanvas` gol care are deja toate proprietățile și evenimentele.
- Adaugă `CommunityToolkit.Mvvm`.
- **Gata când:** P3 poate scrie `SimulatorViewModel(DocumentSession, IDialogService)` și îl poate lega de un `DfaCanvas` (chiar dacă acesta încă nu desenează nimic).

### E-01 · Fereastra principală și meniurile
- **Săpt.:** 1–2 · **Cerințe:** ED-07 · **Depinde de:** E-00 (meniurile Open/Save după C-06)
- Aspectul din contractul 3 (canvas stânga, tab-uri dreapta, bara de stare), cu „În lucru” în locul panourilor neterminate.
- Meniul **Fișier**: Nou, Deschide, Salvează, Salvează ca. Erorile de încărcare apar toate, cu `ShowErrors`. Întrebare „Salvați modificările?” când `IsDirty`.
- Meniul **Exemple**: generat din `ExampleCatalog.All`.
- Meniul **Aplicații ▸ RoPlate** ⇒ `INavigationService.ShowRoPlate()`.
- Titlul ferestrei: `{nume} — Simulator AFD`, cu `*` când sunt modificări nesalvate.
- `NavigationService.OpenInSimulator`: `session.Replace(document.Clone(), null)`, comută pe tab-ul Simulare, apelează `Simulator.Start(input)`.

### E-02 · Desenarea automatului ⚠ P3 are nevoie de el în săpt. 3
- **Săpt.:** 2 · **Depinde de:** C-05 (sau un document construit în cod)
- Desenează `Document` după convențiile vizuale din contract:
  - stări, cerc dublu, săgeata „start”;
  - săgețile pornesc și se termină **pe marginea** cercului (direcția dintre centre, minus raza);
  - tranzițiile în ambele sensuri între aceleași două stări = două curbe arcuite;
  - buclele = arc deasupra stării;
  - o singură săgeată per `Dfa.Edges`, cu eticheta `a, b`.
- Redesenare la `Document.Changed`.
- **Punct M2:** deschizi un exemplu din meniu și apare desenat corect.

### E-05 · Evidențierea pentru simulare
- **Săpt.:** 2 (împreună cu E-02) · **Cerințe:** SI-03 · **Depinde de:** E-02
- `HighlightedState`, `HighlightKind`, `HighlightedEdge`: **se schimbă doar stilul** elementelor existente, fără redesenarea întregului graf.
- Diferențe nu doar prin culoare: contur gros, ✓ / ✗.
- `Mode = PickState` ⇒ clicul ridică doar `StateClicked`.
- **Predă lui P3** și anunță-l (S-06 depinde de asta).

### E-03 · Editarea pe desen
- **Săpt.:** 3 · **Cerințe:** ED-01…ED-04 · **Depinde de:** E-02, C-05
- Clic pe zona goală ⇒ `AddState(position)`.
- Drag & drop ⇒ mutare vizuală în timpul tragerii; `MoveState` **doar la eliberarea mouse-ului** (un singur pas de undo).
- Selecție + tasta Delete ⇒ `RemoveState` / `RemoveEdge`.
- Meniu contextual pe stare: Stare inițială, Stare finală (bifă), Redenumește (`AskText`), Șterge.
- Tranziție: Shift + tragere de la o stare la alta (sau la ea însăși) ⇒ `AskText("Simboluri", "ex: a,b")` ⇒ `AddTransitions`. Dacă primești `OpResult` cu eroare, afișezi mesajul (ex. „Din q0 există deja o tranziție pe «a» către q1”).
- Dublu-clic pe o săgeată ⇒ editarea simbolurilor ei.

### E-04 · Alfabetul și clasele de simboluri
- **Săpt.:** 3 · **Cerințe:** ED-05, ED-10 · **Depinde de:** C-02
- În `EditorPanel`: lista Σ (adăugare/ștergere simbol), lista claselor (nume + caractere, cu ajutor pentru intervale: „A-Z” ⇒ `SymbolClass.FromRanges`). Erorile din `OpResult` apar sub câmp.
- Aici se pune și `TransitionTablePanel` al lui P1 (sub alfabet).

### E-06 · Undo/Redo și capcana în UI
- **Săpt.:** 4 · **Cerințe:** ED-08, ED-09 · **Depinde de:** C-09, C-10
- Butoanele ↶ / ↷ și Ctrl+Z / Ctrl+Y legate de `session.History`, activate după `CanUndo`/`CanRedo`.
- Butonul „Completează cu capcană” ⇒ `CompleteWithTrap(poziție liberă)`.

### E-07 · Descrierile stărilor
- **Săpt.:** 4 · **Cerințe:** SI-10 · **Depinde de:** C-05
- Text mic sub fiecare stare pe desen; editare din meniul contextual („Descriere…”) ⇒ `SetDescription`.
- Sunt importante pentru RoPlate (mesajele vin din ele).

### E-08 · Export imagine (bonus)
- **Săpt.:** 5 · **Cerințe:** ED-12 · PNG cu `RenderTargetBitmap`; SVG opțional.

### Comun
- Raportul: capturi de ecran ale editorului, desenele automatelor pentru raport (după E-08 se pot exporta direct).
