# Konvence pojmenování aliasů IK OVS

Tento dokument definuje jednotný systém aliasů pro mapování požadavků, AI logiku a datové výstupy na části šablony IK OVS.  
Alias slouží jako **stabilní identifikátor**, který zůstává platný i při změnách číslování nebo názvů kapitol.  
Popisuje **význam** části dokumentu, nikoli její aktuální umístění nebo formát.

---

```
IKOVS_<BLOCK><SECTION>[<SUBSECTION>[_<ELEMENT>]]
```

---

**Pravidla:**
- Používej pouze velká písmena a ASCII znaky.  
- Úrovně hierarchie odděluj podtržítkem `_`.  
- V rámci segmentu může být použita pomlčka `-` pro čitelnost.  
- Struktura čte dokument zleva doprava: blok → sekce → podsekce → prvek.

---

## 2️⃣ Bloky dokumentu (`<BLOCK>`)

Každý blok odpovídá hlavní části šablony IK OVS, nikoli číslu kapitoly.

| Segment | Význam |
|----------|--------|
| `PREFACE` | Úvodní část – metadata, základní údaje, poslání |
| `A` | Část A – Architektura úřadu |
| `B` | Část B – Řízení ICT služeb |
| `C` | Část C – Plánování a rozvoj |
| `D` | Část D – Přílohy a revize |

---

## 3️⃣ Sekce (`<SECTION>`)

Názvy sekcí vycházejí z jejich **funkčního významu**, ne z aktuálního textu v šabloně.  
Pojmenování musí být stabilní i při přejmenování nebo sloučení částí.

---

## 4️⃣ Podsekce a prvky (`<SUBSECTION>`, `<ELEMENT>`)

Pro detailní odkazy na konkrétní struktury v rámci sekce (např. tabulky, odstavce, seznamy) se přidává další úroveň.  
Používej pouze tam, kde je to nezbytné pro mapování nebo automatizaci.

---

## 6️⃣ Verzování a údržba

- Všechny aliasy jsou vedeny v tomto jediném souboru: **`template_aliases.md`**.  
- Při změně struktury dokumentu se alias **nezmění**, pouze se může označit jako `deprecated`.  
- Nové aliasy se přidávají na konec seznamu, aby byla zachována zpětná kompatibilita.  
