# REQ_03

**Název:** AI generování celých tabulek nebo jejich částí  
**Typ:** funkční / datový  
**Stav:** koncept  
**Jistota:** střední  
**Priorita:** vysoká

## Požadavek
Asistent v rámci fáze AI fill dokáže vygenerovat celou tabulku nebo její část v šabloně IK OVS (`IKOVS_A_AI-GENERATED_TABLE`) pro případy, kdy tabulka nemá plně deterministický zdroj dat nebo když je žádoucí navrhnout obsah podle předdefinovaného formátu a dostupného kontextu. Generování probíhá jen pro aliasy označené jako vhodné pro AI generování a nesmí přepsat existující hodnoty ve stavu confirmed nebo deterministic bez explicitního uživatelského souhlasu.

Generovaná tabulka musí obsahovat strukturované sloupce definované šablonou a u každého řádku a každého pole uchovávat metadata o původu návrhu, metodě generování, verzi transformační šablony a číselnou úroveň jistoty.

## Odůvodnění
Tabulky s neúplnými nebo nejednoznačnými daty z registrů vyžadují návrhy, které urychlí práci uživatele a zlepšují konzistenci dokumentu. AI generování poskytne návrh, nikoli definitivní obsah. Uživatel zkontroluje, upraví a potvrdí řádky podle potřeby.

## Předpoklady
- Je definován předem formát tabulky nebo šablona polí, která má být generována (field list, datové typy, alias sekce).  
- Dostupné zdroje informací jsou identifikovány v `sources.md` (např. `IKCR`, `WEB_OFFICE`, `ARCHI_PORTAL`).  
- AI komponenta má schopnost extrakce z textu, agregace a generování strukturovaných tabulek, včetně kvantifikace úrovně jistoty pro každou navrženou hodnotu.  
- Uživatelské rozhraní umožňuje prohlédnout, upravit a potvrdit generovanou tabulku.  
- Protokolování a auditní logy jsou nasazeny tak, aby uchovávaly původ návrhu, čas a verzi generování.

## Následný stav
Po provedení generování bude dostupná:
- V interním modelu existuje IKOVS_A_AI-GENERATED_TABLE_<instance> obsahující řádky s unikátním row_id a poli odpovídajícími definici tabulky.
- u každého pole metadata: `source`, `generation_method` (např. "ai_inference", "pattern_match"), `confidence` (číselné nebo kategoriální), `generated_at`, `generated_by`,  
- možnost editace a schválení uživatelem, změny se zapisují do auditního logu,  
- Export (Word/JSON/YAML) zachovává strukturu tabulky, stavy řádků a veškerou provenance.

## Akceptační kritéria
- Systém vygeneruje tabulku podle definovaného formátu (sloupce a datové typy) pro zadanou instanci `IKOVS_A_AI-GENERATED_TABLE`.   
- U každé položky je uveden alespoň jeden zdroj a úroveň jistoty.  
- Generované položky jsou označeny jako *AI návrh* a nejsou považovány za konečné dokud uživatel nepotvrdí jejich stav.  <!-- Not sure about this -->  
- Exporty (Word/JSON/YAML) obsahují informaci o tom, které řádky jsou *AI-generated* a které *confirmed*, včetně referencí na zdroje a čas generování.  
- Vygenerovaná tabulka je použitelná jako kontext pro potenciálni následný AI chatbot workflow (možnost klást dotazy a odkazovat na řádky tabulky).

## Vstupy
- Definice cílové tabulky (pole, typy, alias sekce)  
- Veřejné zdroje 
- Volitelné: výstupy z REQ_01 a REQ_02 (deterministická data a AI návrhy)  
- Parametry generování (např. požadovaná hloubka návrhu, maximální počet položek, threshold pro confidence)

## Výstupy
- `IKOVS_A_AI-GENERATED_TABLE`<instance> v interním modelu.
- dostupný JSON/YAML export tabulky se stavovými flagy a metadaty o zdrojích a úrovni jistoty  
- Auditní log s historií generování, úprav a potvrzení

## Závislosti
- `REQ_01` – deterministické doplnění, které může sloužit jako vstup nebo filtr pro generování  
- `REQ_02` – AI doplnění neúplných polí, výsledky mohou být integrovány do generované tabulky  
- UI komponenty pro editaci tabulek a prohlížení provenance.
- Veřejné zdroje 


