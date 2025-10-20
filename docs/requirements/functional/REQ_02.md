# REQ_02

**Název:** AI doplnění neúplných nebo nejistých polí  
**Typ:** funkční / datový  
**Stav:** koncept  
**Jistota:** střední  
**Priorita:** střední 

## Požadavek
Asistent využívá umělou inteligenci k doplnění polí v tabulkách šablony IK OVS,  
která zůstala po deterministickém doplnění (`REQ_01`) prázdná, neúplná nebo označená jako **nejistá**.  

AI doplnění se opírá o dostupné veřejné informace a textové zdroje  
(např. **WEB_OFFICE**, **IKCR**, **ARCHI_PORTAL**) a snaží se odvodit nebo doplnit hodnoty,  
které lze z těchto kontextů věrohodně získat.  

Každé pole doplněné pomocí AI musí být označeno jako **„AI návrh“**,  
včetně uvedení zdroje, úrovně jistoty a způsobu odvození (např. *z textu webu úřadu* nebo *z dokumentu IK ČR*).

## Odůvodnění
Po deterministickém vyplnění zůstává část polí v tabulkách neúplná nebo bez zdroje.  
AI doplnění umožní předvyplnit tato pole automaticky,  
čímž snižuje rozsah ruční práce.

## Předpoklady
- Výsledky z `REQ_01` (deterministická data) jsou dostupné a označené.  
- Asistent má přístup k veřejným zdrojôm úřadu (`WEB_OFFICE`, `ARCHI_PORTAL`, případně veřejné datasety).  
- AI komponenta dokáže:
  - pracovat s přirozeným jazykem (NLP, extrakce faktů),  
  - vyhodnotit míru jistoty doplněné hodnoty,  
  - zaznamenat zdroj a metodu odvození.  
- Uživatel má možnost návrhy potvrdit nebo upravit.

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
- Metadata o generování (datum, verze)
- Vstupní identifikátor úradu 

## Výstupy
- Vyplněná tabulková pole 
- Záznam zdroje a úrovně jistoty  
- Strukturovaný JSON/YAML export se stavovým značením  
- Auditní log s přehledem potvrzených a odmítnutých návrhů

## Závislosti 
- `REQ_01` – deterministické doplnění faktických dat  
- `REQ_03` – AI generování textových a kontextových polí  

## Poznámky
- Tento požadavek řeší **AI inference** – odvozování hodnot z dostupných textových zdrojů.  
- Nevztahuje se na čistě textové části dokumentu (poznámky, shrnutí, komentáře), které jsou popsány v `XXX`.  
- Každý návrh musí být trasovatelný k původnímu zdroji a doplněn o časové metadata.