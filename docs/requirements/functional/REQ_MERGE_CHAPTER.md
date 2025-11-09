# REQ_MERGE_CHAPTER

**Název:** Strukturální merge kapitoly zpět do interního stavu (V1)
**Typ:** funkční / importní
**Stav:** koncept
**Jistota:** střední
**Priorita:** vysoká

## Požadavek

Systém načte finálně doplněnou **kapitolu** z exportu `REQ_EXPORT_CHAPTER` a aktualizuje **interní stav**. Tato kapitola je od tohoto okamžiku považována za **dokončenou** a stává se zdrojem pravdy pro svůj rozsah. Merge má:

* z dokumentu převzít nové nebo upravené hodnoty v rámci vymezené kapitoly,
* promítnout je do interního stavu,
* označit kapitolu jako strukturálně uzavřenou,
* uložit kapitolu pro final export

## Odůvodnění

Cílem je dokončit práci na kapitole mimo asistenta a následně **sjednotit stav**: co je v kapitole, to platí. Tím se kapitola stabilizuje pro finální export celého dokumentu a odpadá potřeba dalšího dolaďování uvnitř asistenta.

## Předpoklady

* Importovaný balíček (DOCX + doprovodná metadata) pochází z `REQ_EXPORT_CHAPTER`.
* Lze jednoznačně určit cílovou kapitolu a kompatibilitu se šablonou.

## Následný stav

* Interní stav kapitoly odpovídá obsahu importovaného dokumentu.
* Kapitola je označena jako **strukturálně uzavřená** a připravená pro finální export.
* Změny jsou dohledatelné v auditu.

## Akceptační kritéria

1. **Identifikace kapitoly:** Import jednoznačně určí cílovou kapitolu a ověří, že balíček odpovídá této kapitole. Při nesouladu se merge neprovede.
2. **Kompatibilita:** Ověří se kompatibilita se šablonou (verze/hranice). 
3. **Převzetí hodnot:** Všechny nové nebo upravené hodnoty z dokumentu jsou promítnuty do interního stavu v rozsahu kapitoly. Stávající hodnoty v těchto místech jsou nahrazeny.
4. **Označení stavu:** Kapitola je po úspěšném merge označena jako **uzavřená** pro další automatické zásahy asistenta.
5. **Audit:** Systém uloží záznam o provedeném merge (kdo, kdy, co) a stručný souhrn změn.
6. **Recalc:** Proběhne přepočet navázaných závislostí a dostupnosti v rámci kapitoly a nadřazených souhrnů.

## Vstupy

* Exportovaná kapitola (DOCX) a doprovodná metadata k identifikaci a kontrole kompatibility.

## Výstupy

* Aktualizovaný interní stav kapitoly.
* Auditní záznam a souhrn provedených změn.
* uložená upravená kapitola

## Závislosti

* `REQ_EXPORT_CHAPTER` – zdroj importovaného balíčku.
* Finální export celého dokumentu.

## Poznámky

* V1 neřeší jemnozrnná konfliktní pravidla. Import kapitoly má prioritu v rámci jejích hranic.
* Konkrétní struktura metadat importu zůstává záměrně obecná; musí postačovat k identifikaci kapitoly a kontrole kompatibility.
