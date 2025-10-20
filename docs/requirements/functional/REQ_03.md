# REQ_03

**Název:** AI generování celých tabulek nebo jejich částí  
**Typ:** funkční / datový  
**Stav:** koncept  
**Jistota:** střední  
**Priorita:** vysoká

## Požadavek
Asistent dokáže vygenerovat celou tabulku nebo její část v šabloně IK OVS (`IKOVS_A_AI-GENERATED_TABLE`), pokud tato tabulka nemá plně deterministický zdroj dat, nebo pokud je žádoucí vytvořit navrhovaný obsah podle předdefinovaného formátu a dostupných informací.  

Generovaná tabulka obsahuje strukturované sloupce podle šablony (např. položka, popis, zdroj informací, poznámka úřadu, stav plnění), a pro každou položku ukládá metadata o původu návrhu a úrovni jistoty. Tabulka je předkládána uživateli jako *AI návrh*.

## Odůvodnění
Některé tabulky šablony nelze spolehlivě naplnit z veřejných registrů, přesto je užitečné získat předvyplněný návrh (např. seznam očekávaných agend, navrhované projekty, souhrn služeb). AI generování umožní rychlé vytvoření konzistentních návrhů, které uživatelé doplní nebo potvrdí, a zajistí konzistenci napříč dokumenty.

## Předpoklady
- Je definován předem formát tabulky nebo šablona polí, která má být generována (field list, datové typy, alias sekce).  
- Dostupné zdroje informací jsou identifikovány v `sources.md` (např. `IKCR`, `WEB_OFFICE`, `ARCHI_PORTAL`).  
- AI komponenta má schopnost extrakce z textu, agregace a generování strukturovaných tabulek, včetně kvantifikace úrovně jistoty pro každou navrženou hodnotu.  
- Uživatelské rozhraní umožňuje prohlédnout, upravit a potvrdit generovanou tabulku.  
- Protokolování a auditní logy jsou nasazeny tak, aby uchovávaly původ návrhu, čas a verzi generování.

## Následný stav
Po provedení generování bude dostupná:
- nová tabulka v šabloně (`IKOVS_A_AI-GENERATED_TABLE_<instance>`) s řádky a sloupci podle předem definovaného formátu,  
- u každého pole metadata: `source`, `generation_method` (např. "ai_inference", "pattern_match"), `confidence` (číselné nebo kategoriální), `generated_at`, `generated_by`,  
- možnost editace a schválení uživatelem, změny se zapisují do auditního logu,  
- exportovatelný formát (Word/JSON/YAML) zachovávající stav (AI-generated, confirmed, rejected) a původ dat.

## Akceptační kritéria
- Systém vygeneruje tabulku podle definovaného formátu (sloupce a datové typy) pro zadanou instanci `IKOVS_A_AI-GENERATED_TABLE_<instance>`.   
- U každé položky je uveden alespoň jeden zdroj a úroveň jistoty.  
- Generované položky jsou označeny jako *AI návrh* a nejsou považovány za konečné dokud uživatel nepotvrdí jejich stav.  <!-- Not sure about this -->  
- Exporty (Word/JSON/YAML) obsahují informaci o tom, které řádky jsou *AI-generated* a které *confirmed*, včetně referencí na zdroje a čas generování.  
- Vygenerovaná tabulka je použitelná jako kontext pro potenciálni následný AI chatbot workflow (možnost klást dotazy a odkazovat na řádky tabulky).

## Vstupy
- Definice cílové tabulky (pole, typy, alias sekce)  
- Veřejné zdroje 
- Volitelné: výstupy z REQ_01 a REQ_02 (deterministická data a AI návrhy)  
- Parametry generování (např. požadovaná hloubka návrhu, maximální počet položek, threshold pro confidence)
- 
## Výstupy
- Vygenerovaná tabulka `IKOVS_A_AI-GENERATED_TABLE_<instance>` v šabloně IK OVS  
- JSON/YAML export tabulky se stavovými flagy a metadaty o zdrojích a úrovni jistoty  
- Auditní log s historií generování, úprav a potvrzení

## Závislosti
- `REQ_01` – deterministické doplnění, které může sloužit jako vstup nebo filtr pro generování  
- `REQ_02` – AI doplnění neúplných polí, výsledky mohou být integrovány do generované tabulky  
- Rozhraní chatbotu, pokud se má tabulka použít pro následnou interakci  
- Veřejné zdroje 


