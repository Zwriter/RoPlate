# Sarcinile P3: simularea și aplicația RoPlate

**Deții:** `Views/Simulator/**`, `Controls/InputTape/**`, `RealApps/RoPlate/**`, `Dfa.Core/Examples/nr-inmatriculare.dfa.json`, `tests/Dfa.Tests/RealApps/**`.
**Folosești:** [01-core-api.md](contracte/01-core-api.md) (`DfaRun`, `BatchTester`), [03-app-ui.md](contracte/03-app-ui.md) (`DocumentSession`, `DfaCanvas`). **Formatul pe care îl scrii:** [02-format-json.md](contracte/02-format-json.md).

Teoria pentru automatul tău: [../teorie/05-roplate-automatul.md](../teorie/05-roplate-automatul.md).

În săptămânile 1–2 nu ai desenul (E-02/E-05 sunt ale lui P2): lucrezi pe banda de intrare și pe tabelul de execuție, care nu au nevoie de el.

---

### R-01 · Automatul A1 + testele lui
- **Săpt.:** 1 (fișierul scris de mână), 4 (aranjat în editor) · **Depinde de:** C-00 (format), C-06 (pentru testele din fișier)
- Scrie `nr-inmatriculare.dfa.json` complet: 14 stări, clasele `L`, `C`, `_`, tabelul δ din teorie, **descrierea fiecărei stări** (sunt mesajele din RoPlate), cele 18 cuvinte de test.
- Decizia echipei: varianta simplă sau cea cu litera `B` separată (secțiunea 5.6 din teorie). Recomandat: varianta cu `B`.
- **Teste** în `tests/Dfa.Tests/RealApps/`: `[Theory]` cu toate cuvintele, folosind `ExampleCatalog.Load("nr-inmatriculare")`. Până la C-07, construiește automatul în cod.
- În săpt. 4: deschide fișierul în editorul lui P2, aranjează stările frumos, salvează. **Punct M4:** toate testele trec și în panoul de testare în lot.

### S-01 · `SimulatorViewModel`
- **Săpt.:** 1–2 · **Cerințe:** SI-01, SI-03, SI-07 · **Depinde de:** C-00, E-00 (merge și pe stub-uri)
- Câmp de intrare (gol = ε; arată textul „ε” când e gol), butonul **Start**: rulează `DfaValidator`; dacă există erori, le afișează și nu pornește.
- Comenzi: Pas înainte, Pas înapoi, Reset, Rulează până la final, activate/dezactivate corect (`IsAtStart`, `IsAtEnd`).
- Expune pentru canvas: `CurrentState`, `HighlightKind`, `LastEdge`, `RequestedCanvasMode` (contractul 3, secțiunea 6).
- **SI-07:** la `DocumentEdited` cu `Structure`/`Reset` și la `DocumentReplaced` ⇒ oprește simularea + mesajul `Automatul a fost modificat: simularea a fost resetată.`

### S-02 · Banda de intrare (`InputTape`)
- **Săpt.:** 2 · **Cerințe:** SI-02
- Căsuțe cu câte un caracter (spațiul afișat ca `␣`), cursor sub simbolul următor, simbolurile citite estompate (`Brush.TapeRead`), căsuța de blocaj marcată (`ErrorPosition`).

### S-03 · Tabelul de execuție
- **Săpt.:** 2 · **Cerințe:** SI-05
- Coloane: Pas, Citit, (Clasă, dacă diferă), Rest, Stare. Rândul 0 = configurația inițială. Rândul curent evidențiat; rândurile viitoare ascunse sau estompate.
- Trebuie să arate exact ca tabelul din [../teorie/02-simularea-pas-cu-pas.md](../teorie/02-simularea-pas-cu-pas.md).

### S-04 · Verdictul
- **Săpt.:** 2 · **Cerințe:** SI-06
- Afișează `DfaRun.Explanation`, cu ✓ verde / ✗ roșu (simbol + culoare).

### S-05 · Play / Pauză
- **Săpt.:** 3 · **Cerințe:** SI-04
- `DispatcherTimer` care apelează `StepForward()`; slider de viteză (ex. 100–2000 ms) legat de `Interval`; se oprește singur la final.

### S-06 · Animația pe desen
- **Săpt.:** 3 · **Cerințe:** SI-03 · **Depinde de:** E-05 (P2)
- Verifică împreună cu P2 că legăturile `CurrentState`/`LastEdge`/`HighlightKind` → `DfaCanvas` funcționează.
- **Punct M3:** simulare completă pe exemplele 1–3.

### S-07 · Panoul de testare în lot
- **Săpt.:** 4 · **Cerințe:** SI-08 · **Depinde de:** C-08
- `BatchTestPanel` + `BatchTestViewModel(DocumentSession)`: listă editabilă (cuvânt + bifă „acceptat așteptat”), butonul „Rulează testele”, ✓ / ✗ pe fiecare rând, sumarul „9 din 10 corecte”.
- Modificările listei ⇒ `Document.SetTests` (se salvează cu automatul).
- Dublu-clic pe un rând ⇒ deschide cuvântul în simulator.

### S-08 · Modul „Ghicește”
- **Săpt.:** 4 · **Cerințe:** SI-09 · **Depinde de:** E-05 (`PickState`)
- Bifa „Ghicește”: înainte de fiecare pas, `RequestedCanvasMode = PickState`; utilizatorul dă clic pe starea în care crede că ajunge automatul. Răspunsul e confirmat sau corectat („Corect!” / „Greșit: automatul merge în q2”), apoi se face pasul. Scorul apare la final: „Scor: 4 din 5”.

### R-02 · Ecranul RoPlate
- **Săpt.:** 5 · **Cerințe:** aplicația A1 · **Depinde de:** R-01, C-03 (`Append`/`RemoveLast`)
- `RoPlateWindow` + `RoPlateViewModel(INavigationService)`: încarcă `ExampleCatalog.Load("nr-inmatriculare")`.
- Câmp de text mare. Literele mici devin automat majuscule. La fiecare caracter tastat/șters: `Append` / `RemoveLast` (pentru lipire sau editare la mijloc: `new DfaRun`).
- Trei stări vizuale (vezi teoria, secțiunea 5.8): roșu (blocat), neutru (incomplet, cu descrierea stării curente: „Format incomplet: aștept 3 litere”), verde („Număr valid”).
- Butonul **„Arată automatul”** ⇒ `navigation.OpenInSimulator(document, textul curent)`.
- **Mesajele vin din descrierile stărilor** (date), nu sunt scrise în cod.

### R-03 · Coduri reale de județ (bonus)
- **Săpt.:** 5 · Varianta cu tranziții doar pe perechile de litere permise (AB, AR, AG, … VN). Fișier separat `nr-inmatriculare-judete.dfa.json`, ca să nu strici automatul principal.

### Comun
- Raportul pentru aplicația A1: desenul AFD-ului, tabelul δ, semnificația fiecărei stări, lista cuvintelor de test (aproape totul e deja în teorie, secțiunea 5).
- Scenariul demonstrației finale.
