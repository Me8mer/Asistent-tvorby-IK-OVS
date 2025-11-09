# REQ_FINAL_EXPORT

**Název:** Finální export celého dokumentu (DOCX + JSON)
**Typ:** funkční / exportní
**Stav:** koncept
**Jistota:** střední
**Priorita:** vysoká

## Požadavek

Systém vytvoří finální výstup **celého dokumentu** ve formátu **DOCX** a přiloží **JSON** s metadaty exportu. Při sestavení:

* **dokončené kapitoly** se převezmou jako **zdroj pravdy** (obsah z kapitálového exportu/merge),
* **nedokončené kapitoly** se zkonstruují z **šablony** a asistent do nich **injektuje aktuální interní stav** (hodnoty polí a sekcí, které má k dispozici).
  Tento export je výsledný podklad pro předání. Nepředpokládá se jeho zpětný import.

## Odůvodnění

Finální export sjednotí stav dokumentu do jednoho konzistentního výstupu. Kapitoly uzavřené mimo asistenta se berou bez dalších zásahů, ostatní se doplní z interního stavu a šablony. Tím se zajistí kompletní a předatelný dokument bez nutnosti dalších kroků.

## Předpoklady

* Je znám seznam kapitol a jejich stav (dokončená / nedokončená).
* Šablona dokumentu je dostupná v kompatibilní verzi.
* Interní stav obsahuje hodnoty pro pole a sekce, které budou injektovány.

## Následný stav

* Vzniknou dva soubory: **DOCX** s kompletním dokumentem a **JSON** s metadaty exportu.
* Finální DOCX obsahuje všechny kapitoly, přičemž dokončené části jsou převzaté beze změn, ostatní vznikly injekcí interního stavu do šablony.
* Export je zaznamenán v auditu.

## Akceptační kritéria

1. **Sestavení podle stavu kapitol:** Dokončené kapitoly jsou převzaty bez úprav. Ostatní kapitoly jsou sestaveny ze šablony s injektovaným interním stavem.
2. **Konzistence dokumentu:** V DOCX se nevyskytují duplikované nebo chybějící kapitoly; struktura odpovídá šabloně.
3. **Neměnnost uzavřených částí:** Obsah dokončených kapitol není při finálním exportu modifikován asistentem.
4. **Jazyk a formát:** Exportní dokument je ve formátu DOCX, metadata jsou v JSON. Oba soubory mají společné identifikační údaje exportu.
5. **Identifikace zdrojů:** JSON obsahuje stručnou informaci, které kapitoly byly převzaty jako dokončené a které byly sestaveny injekcí interního stavu.
6. **Kompatibilita:** Export proběhne pouze při kompatibilní verzi šablony; v opačném případě se bezpečně přeruší bez výstupu.
7. **Audit:** Systém eviduje, kdo a kdy export vytvořil, a stručný souhrn sestavení (počty dokončených vs. injektovaných kapitol).
8. **Bez zpětného importu:** Tento výstup není určen k merge zpět do systému; to je v JSON uvedeno jako informace pro příjemce.

## Vstupy

* Stav kapitol (dokončená / nedokončená).
* Šablona dokumentu.
* Interní stav hodnot.

## Výstupy

* Jeden soubor **DOCX** s kompletním finálním dokumentem.
* Jeden soubor **JSON** s metadaty a informací o způsobu sestavení kapitol.
* Záznam v audit logu.

## Závislosti

* `REQ_MERGE_CHAPTER` – kapitoly označené jako dokončené.
* Šablona a interní stav pro injekci obsahu.

## Poznámky

* Struktura JSON je záměrně obecná; má sloužit k identifikaci exportu a stručnému popisu sestavení bez předepsaných rigidních polí.
