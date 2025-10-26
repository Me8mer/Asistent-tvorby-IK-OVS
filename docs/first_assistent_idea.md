# Injector-based assistant

## Účel

Asistent vlastní pouze explicitně označené placeholdery a kontextové bloky v dokumentu. Dokument zůstává uživatelův. Asistent do něj vkládá nebo aktualizuje pouze obsah, který výslovně vlastní.

## Vstupy

* ID úřadu a zvolená šablona.
* Volitelný počáteční mapping aliasů.
* Uživatel vybere sekci, na které chce pracovat.

## Minimální UI (beta)

* Výběr sekce.
* Seznam placeholderů a kontextů v sekci.
* Panel AI návrhů s možnostmi akceptovat / upravit / odmítnout.
* Tlačítka Export a Import.
* Fronta položek na manuální rozhodnutí.

## Hlavní pracovní cyklus (pro jednu sekci)

1. Extrahujeme placeholdery a kontexty pro vybranou sekci.
2. Deterministické předvyplnění hodnot, které jsou dostupné bez AI.
3. AI doplní zbývající položky a vygeneruje návrhy s metadaty (zdroj, confidence).
4. Zajistíme způsob, jak AI změny navrhnout uživateli. Rozhodneme později, zda přes UI nebo přímo v dokumentu.
4. Chatbot? 
5. Exportujeme předvyplněnou část nebo celý dokument. Při exportu upravujeme jen označené rozsahy. Vše ostatní zůstává nedotčeno.
6. Uživatel manuálně doplní a upraví dokument.
7. Uživatel importuje upravený dokument zpět. Načteme obsah označených placeholderů a kontextů.
8. Aktualizujeme interní stav podle importovaných hodnot.
9. Pokud jsou splněny závislosti, zopakujeme AI doplnění dalších položek. Iterujeme podle potřeby.

## Závislosti

* Každý placeholder může definovat, které jiné položky nebo uživatelské akce musí být vyplněny před dalším doplněním.
* Nevyplněné závislosti blokují automatické doplnění a jsou zobrazeny v UI jako úkoly pro uživatele.

## Zachování uživatelského obsahu

* Všechno bez markeru zůstává v dokumentu nedotčeno.
* Pokud uživatel chce, aby asistent spravoval novou oblast, přidá marker. Pak se oblast stane součástí modelu.

## Otázky a rozhodnutí na později

1. Formát markeru (možnosti: comment, bookmark, hidden control).
2. Jak přesně budeme prezentovat AI návrhy uživateli v exportovaném dokumentu (komentáře, sledované změny, nebo pouze v UI).
3. Granularita exportu: sekce vs celý dokument jako výchozí.
4. Formát definice závislostí a způsob jejich vizualizace v UI.
5. Politika verzování při exportu/importu a základní synchronizace dokumentu.
6. Workflow pro případ, kdy uživatel přidá novou oblast a chce ji převést pod správu asistenta.
7. Rozsah UI funkcí potřebných v první iteraci vs pozdější rozšíření.
8. Volba úložiště: prototyp vs produkce.

