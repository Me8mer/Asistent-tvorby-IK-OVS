# REQ_04

**Název:** AI návrhy textů pro specifické sekce na základě veřejných zdrojů  
**Typ:** funkční / datový  
**Stav:** koncept  
**Jistota:** střední  
**Priorita:** vysoká

## Požadavek
Asistent v rámci fáze AI fill generuje návrhy textů pro předdefinované textové sekce šablony IK OVS (např. `IKOVS_PREFACE_MANDATE_TEXT-BLOCK`) na základě ověřených veřejných zdrojů. Pro každý vygenerovaný blok se uloží záznam typu ai_proposal obsahující text, seznam použitých zdrojů, metodu odvození, číslicovou úroveň jistoty a čas vytvoření. Návrhy zůstávají v modelu jako AI návrh dokud je uživatel nepotvrdí nebo neupraví.

Generování musí podporovat větnou nebo segmentovou provenienci, aby šlo přesně dohledat, které zdroje podporují která tvrzení v rámci navrženého textu.

## Odůvodnění
Mnohé textové sekce lze připravit automaticky z dostupných veřejných zdrojů, přestože zdrojová informace nemusí být ve strukturované podobě. AI usnadní tvorbu konzistentních, formálně správných návrhů, které sníží manuální práci a zrychlí tvorbu IK OVS. Vždy je nutné zachovat kontrolu uživatele nad finálním zněním.

## Předpoklady
- Seznam a aliasy zdrojů jsou definovány v `sources.md` 
- Jsou dostupné vzorové texty / příklady IK, které slouží jako stylistická referenční šablona.  
- AI modul má schopnost:
  - extrahovat relevantní informace z textových dokumentů a webu,  
  - generovat formální text v češtině odpovídající zvolenému stylu,  
  - kvantifikovat úroveň jistoty (confidence) pro generovaný obsah.  

## Následný stav
Po provedení budou k dispozici:
- návrhy textů pro cílové sekce se stavem `AI návrh`,  
- u každého návrhu metadata: `sources[]`, `generated_at`, `confidence`... 
- možnost uživatelského potvrzení, úpravy nebo zamítnutí.  
- možnost exportu (Word/JSON/YAML) zachovávající označení stavu a zdroje.

## Akceptační kritéria
- Pro každou cílovou sekci systém vygeneruje text označený AI návrh s metadaty a confidence_score  
- U každého návrhu jsou uvedeny použité zdroje.
- Každý návrh nese hodnotu `confidence` a štítek `AI návrh`.  
- Uživatel může návrh regenerovat, potvrdit nebo zamítnout. Potvrzení se zanesou do auditního záznamu.  
- Generované texty odpovídají struktuře příkladů (použité vzory/příklady) a splňují formální náležitosti (odkazy na legislativu, citace tam, kde je to relevantní).

## Vstupy
- Alias cílové sekce (např. `IKOVS_PREFACE_MANDATE`)  
- Zdrojové dokumenty / aliasy ze `sources.md`   
- Stylová šablona / příklady IK pro referenci
- Vstupní identifikátor úradu

## Výstupy
- Návrh textu v češtině označený jako `AI návrh` s metadaty (sources, confidence, generated_at)  
- JSON/YAML export s rozlišením stavu a odkazem na zdroje  
- Auditní záznam změn a potvrzení


## Závislosti
- REQ_06 pro následnou validaci a kontrolu legislativních požadavků.
- UI komponenty pro zobrazení sentence-level provenance a editaci.

## Poznámky
- Texty musí být formální a držet se stylu poskytnutých příkladů IK; implementace by měla umožnit dodání sady vzorů pro ladění stylu.  
- Všechny AI návrhy musí být plně auditovatelné: kdo, kdy a proč byl návrh vygenerován, jaké zdroje byly použity a jaká byla úroveň jistoty.
