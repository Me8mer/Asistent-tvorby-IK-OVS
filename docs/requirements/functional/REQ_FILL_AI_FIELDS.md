# REQ_02

**Název:** AI doplnění neúplných nebo nejistých polí  
**Typ:** funkční / datový  
**Stav:** koncept  
**Jistota:** střední  
**Priorita:** vysok8 

## Požadavek
Asistent po dokončení deterministického doplnění (REQ_01) identifikuje všechna pole označená jako empty nebo needs_validation a vygeneruje pro ně návrhy pomocí AI. Každý návrh musí být zapsán jako samostatný záznam typu ai_proposal obsahující navrženou hodnotu, seznam použitých zdrojů, metodu odvození, numerickou úroveň jistoty a čas vytvoření. Návrhy se neukládají automaticky jako potvrzené hodnoty; zůstávají v modelu ve stavu ai_proposal až do uživatelské akce.

## Odůvodnění
Deterministické zdroje nezajistí plné pokrytí. AI návrhy sníží množství manuální práce a přitom ponechají člověka v roli konečného rozhodovatele. Zároveň musí být zajištěna dohledatelnost a možnost auditovat zdroje a postupy, které návrhy vytvořily.

## Předpoklady
- Výsledky z `REQ_01` (deterministická data) jsou dostupné a označené.  
- Asistent má přístup k veřejným zdrojôm úřadu (`WEB_OFFICE`, `ARCHI_PORTAL`, případně veřejné datasety).  
- AI komponenta dokáže:
  - pracovat s přirozeným jazykem (NLP, extrakce faktů),  
  - vyhodnotit míru jistoty doplněné hodnoty,  
  - zaznamenat zdroj a metodu odvození.  
- UI poskytuje ovládací prvky pro zobrazení, přijetí, odmítnutí a regeneraci návrhu.

## Následný stav
Po splnění požadavku bude asistent schopen:
- doplnit chybějící nebo nejistá pole tabulek pomocí AI,  
- u každého doplněného pole zobrazit zdroj, metodu a úroveň jistoty,  
- označit doplněné hodnoty jako „AI návrh“, dokud nejsou potvrzené,  
- uložit všechny návrhy v jednotném formátu pro export a audit.

## Akceptační kritéria
- Systém rozpozná a vyhledá všechna pole označená jako „neúplná“ nebo „nejistá“ po `REQ_01`.  
- Pro každé takové pole AI nabídne návrh hodnoty s uvedeným zdrojem a úrovní jistoty.  
- Návrhy jsou viditelně označené jako „AI návrh“ a nejsou automaticky potvrzené. <!-- bud automaticky od nejakej istoty hore alebo pre vsetky -->
- Uživatel může návrh potvrdit, odmítnout nebo upravit.  
- Export do Word/JSON uchovává označení *AI-generated*, *confirmed* a *uncertain*

## Vstupy
- Výstupy z `REQ_01` (deterministická data, označená nejistá pole)  
- Veřejné zdroje (`WEB_OFFICE`, `IKCR`, `ARCHI_PORTAL`)  
- Kontext sekce a šablony (aliasy polí)

## Výstupy
- Seznam ai_proposal záznamů v interním modelu.
- Záznam zdroje a úrovně jistoty  
- Strukturovaný JSON/YAML export se stavovým značením  
- Auditní log s přehledem potvrzených a odmítnutých návrhů

## Závislosti 
- `REQ_01` – deterministické doplnění faktických dat   
- UI komponenty pro review a rozhodování.

## Poznámky
- Tento požadavek řeší **AI inference** – odvozování hodnot z dostupných textových zdrojů.  