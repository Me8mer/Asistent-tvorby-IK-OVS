# REQ_EXPORT_SECTION

**Název:** Sekční export pro manuální doplnění (V1 + JSON metadata)
**Typ:** funkční / exportní
**Stav:** koncept
**Jistota:** střední
**Priorita:** vysoká

## Požadavek

Uživatel v UI vybere konkrétní **sekci**. Systém automaticky vybere **všechna nevyplněná pole** v dané sekci a vytvoří dva výstupy:

1. **DOCX** určený pro manuální doplnění.
2. **JSON soubor** s metadaty exportu.

V DOCX bude pro **každé vybrané pole** samostatná **mini-sekce** obsahující identifikaci pole, stručný popis, instrukce k vyplnění a případné poznámky k závislostem. JSON bude obsahovat doplňující metadata pro snadný merge zpět. Informace o exportované sekci, verzi šablony, seznam exportovaných polí a jejich stavy.

## Odůvodnění

Workflow vyžaduje iteraci „vyber sekci → vyexportuj, co chybí -> manuálně doplň -> merge“. Sekční export V1 minimalizuje rozsah práce mimo systém a brání zásahu do struktury šablony. JSON metadata zajistí spolehlivé párování při merge.

## Předpoklady

* Sekce je adresovatelná (`section_alias`) a interní model zná stav každého pole.
* UI umožňuje spustit export nad vybranou sekcí.
* Každé pole má definovaný alias, název a typ.

## Následný stav

* Vzniknou dva soubory: **DOCX** a **JSON** se stejným identifikátorem exportu.
* DOCX obsahuje mini-sekce pro všechna nevyplněná pole, každá se strojově čitelným aliasem.
* JSON obsahuje přehled exportovaných polí, jejich aliasy, původní stavy a základní metadata exportu.
* Oba soubory jsou propojené společným identifikátorem a hashovány pro kontrolu integrity.

## Akceptační kritéria

1. Export se řídí výběrem sekce v UI a zahrne pouze **nevyplněná** pole této sekce.
2. Každá mini-sekce v DOCX obsahuje stabilní **alias** pole, název, stručné **instrukce** k vyplnění a volitelnou poznámku k závislostem.
4. JSON obsahuje minimálně:

   * `section_alias`, `template_version`, `export_id`, `exported_at`, `exported_by`
   * seznam polí s aliasy a aktuálním stavem (`state`)
   * informaci o tom, která pole byla zahrnuta a proč (např. required / závislost)
5. Export neobsahuje žádná data z jiných sekcí.
6. JSON i DOCX jsou propojené pomocí identifikátoru `export_id` a kontrolních hashů.
7. Exportní metadata umožňují jednoznačné spárování při `REQ_MERGE_SECTION`.

## Vstupy

* `section_alias` z UI.
* `template_version` a interní stav polí v sekci.
* Volitelně seznam explicitně přidaných polí.

## Výstupy

* Jeden soubor **DOCX** s mini-sekcemi pro doplnění.
* Jeden soubor **JSON** s metadaty exportu a přehledem zahrnutých polí.
* Auditní záznam o provedeném exportu.

## Závislosti

* `REQ_MERGE_SECTION` – zpětný import doplněného DOCX a použití JSON metadat pro párování.

## Poznámky

* JSON schema bude upřesněno podle finální struktury interního modelu.
* Textové instrukce v DOCX mají být stabilní a bez konkrétních příkladů hodnot.

