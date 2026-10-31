# Sarcinile P1: motorul (`Dfa.Core`) + tabelul δ

**Deții:** `src/Dfa.Core/**`, `tests/Dfa.Tests/Core/**`, `src/Dfa.App/Views/TransitionTable/**`, soluția.
**Contracte pe care le implementezi:** [01-core-api.md](contracte/01-core-api.md), [02-format-json.md](contracte/02-format-json.md).

Ești primul de care depind ceilalți, așa că **C-00 se face în primele 1–2 zile**, înaintea oricărei alte sarcini.

---

### C-00 · Soluția și stub-urile contractului ⚠ blochează pe toți
- **Săpt.:** 1 (ziua 1–2) · **Depinde de:** nimic
- Creează `DfaSimulator.slnx` cu `Dfa.Core` (class library), `Dfa.App` (Avalonia, din șablonul `avalonia.app`), `Dfa.Tests` (xUnit + Shouldly) și referințele dintre ele.
- Activează `<Nullable>enable</Nullable>` peste tot.
- Scrie **toate tipurile publice** din contractul 1, cu corpul `throw new NotImplementedException()` (record-urile și enum-urile se scriu complet).
- **Gata când:** soluția compilează, P2 și P3 pot scrie cod care apelează motorul.

### C-01 · `Dfa`: modelul și modificările
- **Săpt.:** 1 · **Cerințe:** ED-03, ED-04, ED-05 · **Depinde de:** C-00
- Stări, Σ, start, finale, δ ca dicționar `TransitionKey → string`, `Edges` grupate, `Clone()`.
- Toate operațiile întorc `OpResult` cu **mesajele exacte** din contract.
- **Teste:** tranziție duplicat refuzată cu mesajul ED-04; simbol străin refuzat; `RemoveState` șterge tranzițiile; `RenameState` actualizează δ, start și finale; `AddTransitions` „totul sau nimic”.

### C-02 · Clase de simboluri și `SymbolOf`
- **Săpt.:** 1 · **Cerințe:** ED-10 · **Depinde de:** C-01
- `SymbolClass.FromRanges`, `Contains`, regula de mapare în 3 pași, refuzul claselor care se suprapun.
- **Teste:** `SymbolOf('7') == "C"`, `SymbolOf(' ') == "_"`, `SymbolOf('_') == null` (când `_` e clasă), caracter necunoscut ⇒ `null`.

### C-03 · `DfaRun`: execuția pas cu pas
- **Săpt.:** 1 · **Cerințe:** SI-01, SI-03, SI-05, SI-06 · **Depinde de:** C-02
- Pașii precalculați, navigare înainte/înapoi, verdict + motiv + `Explanation` cu textele exacte, `Append`/`RemoveLast` pentru RoPlate, `Changed`.
- **Teste:** toate cuvintele din tabelul secțiunii 9 a specificației (exemplele 1–6) cu `[Theory]`/`[InlineData]`; ε; blocaj (ultimul pas = `NoTransition`, verdictul are poziția corectă); simbol străin; fără stare inițială; `Append` ×n echivalent cu `new DfaRun(dfa, tot cuvântul)`.
- Construiește automatele de test **în cod** (încă nu există JSON).

### C-04 · `DfaValidator`
- **Săpt.:** 1 · **Depinde de:** C-02
- Toate codurile din contract (`NO_START`, `UNREACHABLE` prin parcurgere din starea inițială etc.).
- **Teste:** câte un test pentru fiecare cod.
- **Punct M1:** C-00…C-04 în `main`, testele verzi.

### C-05 · `DfaDocument`
- **Săpt.:** 2 · **Depinde de:** C-01
- Operațiile de editare care țin `Layout`/`Descriptions` sincronizate, nume automat `q0, q1…` (primul liber), `Changed` cu `Kind` corect (exact un eveniment per operație reușită, niciunul la eșec), `Clone`/`RestoreFrom`.
- **Teste:** `RenameState` mută poziția și descrierea; `MoveState` dă `Layout`, nu `Structure`; operația eșuată nu ridică `Changed`.

### C-06 · `DfaJson`
- **Săpt.:** 2 · **Cerințe:** ED-07, robustețe · **Depinde de:** C-05
- Citire/scriere după contractul 2; toate erorile din tabelul de acolo; poziții automate pentru stări fără `layout`.
- **Teste:** dus-întors (`Deserialize(Serialize(doc))`); câte un fișier corupt pentru fiecare tip de eroare; JSON trunchiat nu aruncă excepție.

### C-07 · `ExampleCatalog` + exemplele 1–6
- **Săpt.:** 2 · **Cerințe:** ED-07 · **Depinde de:** C-06
- Scrie `par-a`, `incepe-a` (cu capcană), `termina-ab`, `contine-aba`, `div3`, `lungime-3` în `Dfa.Core/Examples/`, ca **EmbeddedResource**, cu poziții frumoase, descrieri pentru fiecare stare și cuvintele de test din specificație.
- Teoria fiecărui exemplu e în [../teorie/03-cum-proiectezi-un-afd.md](../teorie/03-cum-proiectezi-un-afd.md).
- **Teste:** fiecare exemplu se încarcă fără erori și își trece propriile teste (`BatchTester`).

### C-08 · `BatchTester`
- **Săpt.:** 3 · **Cerințe:** SI-08 · **Depinde de:** C-03
- `Run`, `Summary` = „9 din 10 corecte”.

### C-09 · `DocumentHistory` (undo/redo)
- **Săpt.:** 3 · **Cerințe:** ED-09 · **Depinde de:** C-05
- **Teste:** adaugă 3 stări ⇒ undo ×3 ⇒ document gol ⇒ redo ×3; o operație nouă după undo șterge redo-ul; capacitatea e respectată.

### C-10 · `CompleteWithTrap`
- **Săpt.:** 3 · **Cerințe:** ED-08 · **Depinde de:** C-05
- Adaugă o stare nefinală care primește toate tranzițiile lipsă (inclusiv propriile bucle). Dacă automatul e deja complet ⇒ `OpResult.Fail("Automatul este deja complet.")`.
- **Teste:** după operație, validatorul nu mai dă `MISSING_TRANSITION`; limbajul recunoscut nu se schimbă (aceleași rezultate pe cuvintele de test).

### C-11 · Panoul tabelului δ (UI)
- **Săpt.:** 4 · **Cerințe:** ED-06 (+ ED-11 bonus) · **Depinde de:** C-05, E-00
- `TransitionTablePanel` + `TransitionTableViewModel(DocumentSession)`: rânduri = stări (cu `→` și `*`), coloane = Σ, celule goale colorate cu `Brush.MissingCell`. Se actualizează la `DocumentEdited` și `DocumentReplaced`.
- Bonus ED-11: celula editabilă (listă cu stările) ⇒ `AddTransitions` / `RemoveTransition`.

### C-12 · Acoperire ≥ 70%
- **Săpt.:** 4 · Adaugă `coverlet.collector` și verifică acoperirea pe `Dfa.Core`:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### C-13 · Generatorul de cuvinte (bonus)
- **Săpt.:** 5 · **Cerințe:** SI-11 · Parcurgere în lățime (BFS); vezi [../teorie/04-simulatorul-si-arhitectura.md](../teorie/04-simulatorul-si-arhitectura.md#42-generatorul).
- **Teste:** pentru `par-a`, primele 4 sunt `"", "b", "aa", "bb"`.

### Comun
- Raportul: secțiunile despre motor și despre exemplele 1–6.
