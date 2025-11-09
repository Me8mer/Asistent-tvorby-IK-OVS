# REQ_EXPORT_CHAPTER

**Název:** Strukturální export kapitoly (DOCX + JSON)
**Typ:** funkční / exportní
**Stav:** koncept
**Jistota:** střední
**Priorita:** vysoká

## Požadavek

Uživatel v UI vybere **kapitolu**. Systém vytvoří **strukturální export** kapitoly do **DOCX** a přiloží **JSON** s metadaty exportu. Export obsahuje celou kapitolu včetně všech sekcí a tabulek vyplněných podle aktuálního interního modelu. Export je určen pro **dokončení práce na kapitole** – tedy pro doplnění sekcí a polí, které asistent nedokázal vyplnit. Uživatel může provést obsahové a dílčí strukturální doplnění uvnitř hranic kapitoly, mimo ni zasahovat nesmí.

## Odůvodnění

Tento export slouží jako **finální pracovní podklad pro dokončení kapitoly**. Umožní uživatelům nebo odborníkům mimo asistenta doplnit části, které nebylo možné vyplnit deterministicky nebo pomocí AI návrhů. Kapitola se po tomto exportu považuje za hotový pracovní celek připravený k finálnímu merge nebo k závěrečnému exportu celého dokumentu.

## Předpoklady

* Kapitola je adresovatelná (`chapter_id`, `chapter_alias`).
* Interní model umí injektovat aktuální hodnoty a stavy do šablony kapitoly.
* Jsou definovány hranice kapitoly (začátek/konec, seznam sekcí) a pravidla, co lze v rámci kapitoly měnit.

## Následný stav

* Vzniknou dva soubory: **DOCX** s kompletní kapitolou a **JSON** s metadaty exportu.
* DOCX obsahuje pouze vybranou kapitolu, mimo ni žádné části dokumentu.
* V rámci kapitoly lze doplnit chybějící sekce a pole, podle označení a instrukcí.
* JSON obsahuje metadata a přehled zahrnutých sekcí a polí potřebných pro `REQ_MERGE_CHAPTER`.

## Akceptační kritéria

1. **Rozsah:** Export zahrnuje výhradně vybranou kapitolu a všechny její sekce, které existují v šabloně.
2. **Injekce obsahu:** Všechna dostupná data z interního modelu jsou vložena do správných míst kapitoly.
3. **Nedoplněné části:** V DOCX jsou jasně označena místa, která je potřeba ručně doplnit (sekce, tabulky, pole).
4. **Identifikace:** JSON obsahuje minimálne metadata.
5. **Mapování:** JSON obsahuje mapu strukturálních prvků (sekce, tabulky, bloky) s jejich identifikátory pro jednoznačný merge.
6. **Integrita:** DOCX a JSON jsou propojené přes `export_id` a kontrolní hash.
7. **Kompatibilita:** Export je kompatibilní s `REQ_MERGE_CHAPTER` a nevyžaduje dodatečné mapování.

## Vstupy

* `chapter_id` nebo `chapter_alias` z UI.
* `template_version` a aktuální interní stav kapitoly.

## Výstupy

* Jeden soubor **DOCX** s kompletní kapitolou pro finální doplnění.
* Jeden soubor **JSON** s metadaty exportu kapitoly.
* Záznam v audit logu o provedeném exportu kapitoly.

## Závislosti

* `REQ_MERGE_CHAPTER` – zpětné sloučení doplněné kapitoly.
* `REQ_TPL_CORE_MIN` – stabilní aliasy a hranice kapitoly.

## Poznámky

* Export je považován za pracovní verzi před finálním sloučením kapitoly.
* Nepřidávají se žádné ukázkové texty ani návrhy; vše nevyplněné se označí jako k doplnění.
* Po importu v `REQ_MERGE_CHAPTER` může být kapitola označena jako strukturálně uzavřená.
