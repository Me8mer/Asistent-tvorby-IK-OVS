# REQ_CHAT_01

**Název:** Chatbot pro vyplnění předem určených tabulek a textových sekcí pouze po konverzaci s užívatelem
**Typ:** funkční / interaktivní  
**Stav:** koncept  
**Jistota:** nízka  
**Priorita:** nízka

## Požadavek
Chatbot vede řízený rozhovor s uživatelem z úřadu a získané odpovědi používá k vyplnění **předem určených** tabulek a textových sekcí šablony IK OVS, které **nelze** vyplnit deterministicky ani AI z veřejných zdrojů.  
Cílové části jsou identifikovány aliasy (např. `IKOVS_<BLOCK>_<SECTION>[_...]`) a označeny jako *interaktivně vyplnitelné*.  
Každé vyplněné pole nebo text je uloženo se stavem **„ai_interactive – návrh“** a vyžaduje uživatelské potvrzení.

## Odůvodnění
Část informací existuje pouze u daného úřadu (interní postupy, rozhodnutí, specifické parametry). Tyto údaje nelze spolehlivě získat z registrů ani z veřejných textů. Řízený rozhovor minimalizuje zátěž uživatele, sjednocuje sběr informací a zajišťuje trasovatelnost odpovědí do příslušných polí/sekcí.

## Předpoklady
- V `mapping_req_to_template.md` (nebo obdobném konfiguračním souboru) je seznam aliasů cílových částí označených jako **interactive-only** (tabulky/sekce).  
- Pro každý cílový alias existuje **otázkový scénář** (sada povinných a volitelných otázek, typ očekávané odpovědi, validační pravidla).  

## Následný stav
- Pro každý cílový alias existuje záznam rozhovoru (Q/A) a z něj odvozené hodnoty pro konkrétní pole/sekce.  
- Vyplněné položky jsou ve stavu **„AI návrh (ai_interactive)“**, dokud je uživatel nepotvrdí (stav **confirmed**).  
- Každá položka obsahuje metadata o konverzaci 
- Nejednoznačné či chybějící odpovědi jsou označeny a ponechány k doplnění (např. `needs_answer`, `not_applicable`).

## Akceptační kritéria
- Systém umí podle konfigurace vybrat správný **otázkový scénář** pro daný alias (sekci/tabulku).  
- Každá **povinná otázka** má stav: zodpovězeno / nezodpovězeno / neuplatňuje se, s odůvodněním.  
- Každá vyplněná položka je **trasovatelná** k otázce/odpovědi (Q/A -> pole/sekce) a nese stav **„AI návrh (ai_interactive)“**.  
- Uživatel může hodnoty **upravit nebo potvrdit**; potvrzení mění stav na **confirmed** a ukládá auditní záznam (kdo/kdy/co).  
- Export (Word/JSON/YAML) rozlišuje `ai_interactive` vs. `confirmed` a obsahuje odkazy na konverzační záznam.  
- Systém nevyplňuje hodnotu bez odpovědi uživatele; místo toho označí položku jako `needs_answer` a nabídne pokračování rozhovoru.

## Vstupy
- Seznam cílových aliasů/placeholderů určených pro **interactive-only**  
- Otázkové scénáře (povinné/volitelné otázky, typy odpovědí, validace)  
- Kontext dokumentu (již vyplněná data z předchozích REQ)  
- Identita/role uživatele

## Výstupy
- Vyplněná pole/tabulky/sekce ve stavu `ai_interactive` + metadata Q/A  
- Auditní záznamy o rozhovoru a potvrzeních  
- Exportovatelná data s rozlišením stavů (`ai_interactive`, `confirmed`, `needs_answer`, ...)

## Závislosti
- REQ_01, REQ_02, REQ_03, REQ_04, REQ_05 – stav vyplnění předává kontext; chatbot se spouští jen pro části bez deterministického/AI pokrytí.  
- REQ_06 – validační pravidla mohou upozornit na chybějící odpovědi a vrátit se do rozhovoru.  
- `template_aliases.md`, `mapping_req_to_template.md` – definice aliasů a označení *interactive-only*.

## Poznámky
- Chatbot **nehádá** hodnoty; bez explicitní odpovědi uživatele nevkládá data.  
