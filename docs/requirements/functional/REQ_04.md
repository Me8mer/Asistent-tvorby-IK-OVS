# REQ_04

**Název:** AI návrhy textů pro specifické sekce na základě veřejných zdrojů  
**Typ:** funkční / datový  
**Stav:** koncept  
**Jistota:** střední  
**Priorita:** vysoká

## Požadavek
Asistent vytvoří návrhy textů pro předem definované textové sekce šablony IK OVS (např. `IKOVS_PREFACE_MANDATE_TEXT-BLOCK`), a to na základě veřejně dostupných zdrojů (legislativa, metodiky, oficiální web úřadu, IKČR apod.).  

Výstupem jsou formální, linguisticky konzistentní texty, které odpovídají stylu a struktuře příkladů IK. Každý návrh je vždy označen jako **„AI návrh“**, obsahuje uvedení použitých zdrojů a indikaci úrovně jistoty. Návrhy vyžadují uživatelské potvrzení před tím, než se stanou součástí finálního dokumentu.

## Odůvodnění
Mnohé textové sekce lze připravit automaticky z dostupných veřejných zdrojů, přestože zdrojová informace nemusí být ve strukturované podobě. AI usnadní tvorbu konzistentních, formálně správných návrhů, které sníží manuální práci a zrychlí tvorbu IK OVS. Vždy je nutné zachovat kontrolu uživatele nad finálním zněním.

## Předpoklady
- Seznam a aliasy zdrojů jsou definovány v `sources.md` (např. `LAW_365_2000`, `DECREE_360_2023`, `IKCR`, `WEB_OFFICE`).  
- Jsou dostupné vzorové texty / příklady IK, které slouží jako stylistická referenční šablona.  
- AI modul má schopnost:
  - extrahovat relevantní informace z textových dokumentů a webu,  
  - generovat formální text v češtině odpovídající zvolenému stylu,  
  - kvantifikovat úroveň jistoty (confidence) pro generovaný obsah.  

## Následný stav
Po provedení budou k dispozici:
- návrhy textů pro cílové sekce se stavem `AI návrh`,  
- u každého návrhu metadata: `sources[]`, `generated_at`, `confidence`... 
- možnost uživatelského potvrzení, úpravy nebo zamítnutí. Potvrzení se zapisuje do auditního logu,  
- export (Word/JSON/YAML) zachovávající označení stavu a zdroje.

## Akceptační kritéria
- Pro zadanou sekci systém vygeneruje text v odpovídající formálnímu stylu příkladů IK.  
- U každého návrhu jsou uvedeny použité zdroje.
- Každý návrh nese hodnotu `confidence` a štítek `AI návrh`.  
- Uživatel může návrh upravit, potvrdit nebo zamítnout. Potvrzení se zanesou do auditního záznamu.  
- Export do Word/JSON/YAML zřetelně rozlišuje mezi `AI návrh` a `confirmed` texty.  
- Generované texty odpovídají struktuře příkladů (použité vzory/příklady) a splňují formální náležitosti (odkazy na legislativu, citace tam, kde je to relevantní).

## Vstupy
- Alias cílové sekce (např. `IKOVS_PREFACE_MANDATE_TEXT-BLOCK`)  
- Zdrojové dokumenty / aliasy ze `sources.md`   
- Stylová šablona / příklady IK pro referenci
- Vstupní identifikátor úradu

## Výstupy
- Návrh textu v češtině označený jako `AI návrh` s metadaty (sources, confidence, generated_at)  
- JSON/YAML export s rozlišením stavu a odkazem na zdroje  
- Auditní záznam změn a potvrzení

## Závislosti

## Poznámky
- Texty musí být formální a držet se stylu poskytnutých příkladů IK; implementace by měla umožnit dodání sady vzorů pro ladění stylu.  
- Všechny AI návrhy musí být plně auditovatelné: kdo, kdy a proč byl návrh vygenerován, jaké zdroje byly použity a jaká byla úroveň jistoty.
