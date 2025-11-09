# REQ_MERGE_SECTION

**Název:** Sekční merge zpět do interního modelu (V1)
**Typ:** funkční / importní
**Stav:** koncept
**Jistota:** střední
**Priorita:** vysoká

## Požadavek

Systém umožní načíst výsledky manuálního doplnění jedné sekce z dvojice souborů **DOCX + JSON** vytvořených `REQ_EXPORT_SECTION`. Při merge se strojově spárují hodnoty s interním modelem podle **aliasů polí**, **identifikátoru exportu** a **verze šablony**. Nové hodnoty se uloží jako vstup uživatele se stavem `user_input`, bez automatického potvrzení. Systém detekuje konflikty, zachová auditní stopu a po úspěšném zapsání spustí přepočet závislostí sekce.

## Odůvodnění

Sekční merge uzavírá iteraci „export -> doplnění -> merge“. Potřebujeme deterministické párování, bezpečné zpracování rozdílů a okamžité odblokování navazujících polí bez rizika ztráty dat.

## Předpoklady

* Importované soubory pocházejí z `REQ_EXPORT_SECTION` a obsahují společný `export_id`.
* JSON obsahuje minimální metadata
* Dokument je v rámci stejné šablony nebo kompatibilní verze.

## Následný stav

* Všechny úspěšně spárované hodnoty jsou zapsány do interního modelu se stavem `user_input` a časovým razítkem merge.
* U každé změněné položky existuje auditní záznam s původní a novou hodnotou.
* Nad dotčenou sekcí proběhne přepočet závislostí a aktualizace dostupnosti `ready/blocked`.

## Akceptační kritéria

1. **Identifikace balíčku:** DOCX a JSON patří k sobě (`export_id`) a odpovídají importované sekci (`section_alias`).
2. **Kompatibilita šablony:** `template_version` v JSON je kompatibilní s aktuální verzí. Pokud není, merge se přeruší bez zápisu.
3. **Deterministické mapování:** Hodnoty se párují výhradně podle `field_alias`. Položky bez aliasu nebo mimo sekci se ignorují.
4. **Zápis hodnot:** Hodnoty z importu se zapisují jako pravdivé a přepisují existující hodnoty v interním modelu. Stav pole se aktualizuje podle logiky systému (typicky `confirmed` nebo `final`).
5. **Chybné položky:** Pokud pole v importu chybí nebo má nečitelnou hodnotu, systém jej přeskočí a uvede v souhrnu jako chybu.
6. **Částečný úspěch:** Úspěšně zapsané položky se uloží i v případě, že jiné skončí chybou. Systém vypíše úplný souhrn průběhu merge.
7. **Audit:** Každá změna je logována s `user_id`, `export_id`, `template_version` a časovými razítky `exported_at` a `merged_at`.
9. **Recalc:** Po dokončení zápisu systém provede přepočet závislostí sekce a uloží `last_recalc_at` pro dotčené položky.

## Vstupy

* Soubor **DOCX** s vyplněnými mini-sekcemi pro danou sekci.
* Soubor **JSON** s metadaty exportu a seznamem zahrnutých polí.
* Volitelně parametry režimu merge: `on_conflict = stop|skip|record` (V1 default `record`).

## Výstupy

* Aktualizovaný interní model s novými hodnotami ve stavu `user_input`.
* Souhrnný report merge s členěním `applied/skipped/conflicts/errors`.
* Záznamy v audit logu pro všechny zapsané i konfliktní položky.

## Závislosti

* `REQ_EXPORT_SECTION` – původ exportovaných souborů a struktura mini-sekcí.
* `REQ_DEPS_MIN` – přepočet závislostí po merge.
* UI pro následné potvrzení hodnot v sekci.

## Poznámky

* V1 nepřepisuje strukturální části dokumentu. Importuje pouze hodnoty polí. Strukturální změny spadají do kapitálového merge.
* Je nutné definovat minimální JSON schema tak, aby bylo stabilní i při změně interního modelu.



