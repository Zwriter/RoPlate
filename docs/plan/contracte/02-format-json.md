# Contractul 2: formatul fișierelor `.dfa.json`

**Proprietar:** P1 (citire/scriere în `DfaJson`). **Folosit de:** toți. P3 scrie de mână `nr-inmatriculare.dfa.json` încă din săptămâna 1, deci formatul trebuie fixat din prima zi.

Extensia fișierelor: `.dfa.json`. Codificare: UTF-8.

---

## 1. Structura

```json
{
  "format": "dfa-simulator",
  "version": 1,
  "name": "Număr par de a",
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
    "q0": "am văzut un număr par de a",
    "q1": "am văzut un număr impar de a"
  },
  "tests": [
    { "word": "", "expected": true },
    { "word": "abba", "expected": true },
    { "word": "bab", "expected": false }
  ]
}
```

| Câmp | Tip | Obligatoriu | Note |
|---|---|---|---|
| `format` | string | da | mereu `"dfa-simulator"`; altă valoare ⇒ eroare |
| `version` | int | da | `1`. Dacă formatul se schimbă, se crește versiunea |
| `name` | string | nu | implicit: numele fișierului |
| `alphabet` | string[] | da | Σ: simboluri simple (1 caracter) și nume de clase |
| `symbolClasses` | obiect[] | nu | `{ "name": "C", "chars": "0123456789" }` |
| `states` | string[] | da | ordinea contează (așa apar în tabelul δ) |
| `start` | string / null | nu | `null` sau lipsă = fără stare inițială (validatorul dă eroare la simulare, dar fișierul se încarcă) |
| `finals` | string[] | nu | implicit `[]` |
| `transitions` | obiect[] | nu | **un obiect per simbol**, chiar dacă pe desen sunt grupate pe aceeași săgeată |
| `layout` | obiect | nu | coordonatele **centrului** cercului, în pixeli |
| `descriptions` | obiect | nu | ce „ține minte” starea (SI-10); folosite de RoPlate pentru mesaje |
| `tests` | obiect[] | nu | `word: ""` înseamnă ε |

Câmpurile necunoscute sunt **ignorate** (nu e eroare).

---

## 2. Clase de simboluri

`chars` conține **toate caracterele, scrise explicit** (fără intervale de tip `A-Z`), ca fișierul să fie ușor de citit de oricine:

```json
"alphabet": ["L", "C", "_"],
"symbolClasses": [
  { "name": "L", "chars": "ABCDEFGHIJKLMNOPQRSTUVWXYZ" },
  { "name": "C", "chars": "0123456789" },
  { "name": "_", "chars": " " }
]
```

Fiecare `name` din `symbolClasses` trebuie să apară și în `alphabet`.

---

## 3. Ce face `DfaJson.Deserialize` cu un fișier greșit

Nu aruncă excepții și nu blochează aplicația (cerința de robustețe). Returnează `LoadResult` cu lista de erori în română.

| Problemă | Rezultat | Mesaj |
|---|---|---|
| nu e JSON valid | eroare | `Fișierul nu este un JSON valid (linia {n}).` |
| `format` greșit / lipsă | eroare | `Fișierul nu este un automat (lipsește "format": "dfa-simulator").` |
| `version` mai mare decât cea cunoscută | eroare | `Fișierul a fost creat cu o versiune mai nouă ({v}).` |
| stare duplicat în `states` | eroare | `Starea «{s}» apare de două ori.` |
| tranziția folosește o stare inexistentă | eroare | `Tranziția {from} –{symbol}→ {to} folosește o stare inexistentă.` |
| tranziția folosește un simbol din afara lui Σ | eroare | `Simbolul «{symbol}» nu aparține alfabetului.` |
| două tranziții pe același `(from, symbol)` | eroare | `Din {from} există deja o tranziție pe «{symbol}».` |
| `start` sau `finals` cu stare inexistentă | eroare | `Starea «{s}» nu există.` |
| stare fără `layout` | **acceptat** | primește poziție automată |
| `layout`/`descriptions` pentru stare inexistentă | **acceptat** | intrarea se ignoră |

Dacă există cel puțin o eroare, `Document` este `null`, iar UI-ul afișează toate erorile.

---

## 4. Ce face `DfaJson.Serialize`

- Indentare de 2 spații (fișierele se compară ușor în Git).
- Ordinea câmpurilor: ca în exemplul de la secțiunea 1.
- Stările, în ordinea din `Dfa.States`; tranzițiile, sortate după ordinea stării `from`, apoi după ordinea simbolului în `alphabet`.
- Coordonatele, rotunjite la întregi.
- **Proprietatea de bază, testată de P1:** `Deserialize(Serialize(doc))` dă un document echivalent.

---

## 5. Exemplu complet: automatul RoPlate (schelet)

Fișierul complet îl scrie P3 (sarcina R-01), după [../../teorie/05-roplate-automatul.md](../../teorie/05-roplate-automatul.md).

```json
{
  "format": "dfa-simulator",
  "version": 1,
  "name": "Număr de înmatriculare",
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
    "q0": "Introdu codul județului",
    "S": "Aștept 3 litere",
    "T3": "Număr valid"
  },
  "tests": [
    { "word": "B 123 ABC", "expected": true },
    { "word": "CJ 123 ABC", "expected": false }
  ]
}
```
