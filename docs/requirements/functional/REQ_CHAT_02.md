# REQ_CHAT_02

**Název:** Křížové dotazování podle závislostí (cross-section) — propagace odpovědí napříč sekcemi  
**Identifikátor:** REQ_CHAT_02  
**Typ:** funkční / interaktivní / integrační  
**Stav:** koncept  
**Jistota:** nízká  
**Priorita:** nízká

## Požadavek
Chatbot umí promítnout jednu uživatelskou odpověď do více tabulek a textových sekcí, které jsou na ní závislé.  
Pro každou konfigurovanou závislost platí pravidlo mapování (zdrojová otázka -> cílová pole/sekce, transformační pravidla, podmínky aplikace).  
Po získání odpovědi chatbot provede **propagaci** hodnoty do všech navázaných míst jako návrh (`ai_interactive`), přičemž u každé změny uchová odkaz na původní Q/A a možnost uživatelem potvrdit nebo upravit výsledky.

## Odůvodnění
Mnoho informací je společných pro více částí IK OVS. Křížové dotazování umožní jednou získanou odpověď využít konzistentně napříč dokumentem, zjednodušit workflow a zajistit konzistentní auditní stopu.

## Předpoklady
- Existuje definovaný **graf závislostí** / mapovací konfigurace, kde jsou pro jednotlivé otázky specifikovány cílové aliasy a pravidla transformace.
- Každé cílové pole má metadata, která umožňují přiřazení hodnoty z konverzace (pole id, typ, validacem, ...).  
- Systém uchovává cache potvrzených odpovědí, kterou lze použít při opakovaných konverzacích.  
- Uživatelé mají možnost schvalovat nebo upravovat propagované návrhy před jejich finalizací.  
- Auditní systém a verzování dokumentu jsou dostupné pro zpětné zobrazení změn.

## Následný stav
- Po dokončení konverzace budou v dokumentu dostupné návrhy hodnot v cílových polích se štítkem **`ai_interactive`** a s metadaty ukazujícími na původní Q→A (otázka, odpověď, timestamp, uživatel).  
- Všechny propagace jsou zaznamenány v audit logu (kdo odpověděl, kdy, jaká transformace byla použita, které cílové položky byly ovlivněny).  
- Uživatel může hromadně nebo po položkách potvrdit/odmítnout propagované hodnoty; potvrzení mění stav na `confirmed`.  

## Akceptační kritéria
- Systém interpretuje mapovací konfiguraci a rozpozná závislé cíle pro zadanou otázku.  
- Po odpovědi chatbot vytvoří návrhy pro všechny navázané cíle v souladu s transformačními pravidly anebo uloží potenciálni odpovedi do cache??.  
- Všechny navržené změny jsou jasně označeny jako `ai_interactive` a obsahují odkaz na původní Q/A.  
- Při potvrzení se originální Q/A uloží do cache s verzí a metadaty.

## Vstupy
- Odpověď z proběhlé konverzace (Q/A)
- Mapovací konfigurace závislostí (zdrojová otázka → seznam cílových aliasů + transformační pravidla)  
- Kontext dokumentu (aktuální hodnoty v cílových polích, stav vyplnění)  
- Uživatelská identita a oprávnění

## Výstupy
- Návrhy hodnot v cílových polích/sekcích se stavem `ai_interactive` a metadaty Q/A  
- Seznam konfliktů (pokud některé cíle obsahují nekompatibilní hodnoty)  
- Auditní záznam propagace a verze změn  
- Aktualizovaná cache potvrzených odpovědí

## Závislosti
- REQ_CHAT_01 (otázkové scénáře a interaktivní Q→A)  
- REQ_06 (validace pravidel a legislativní kontroly)  
- Mapovací konfigurace závislostí

## Poznámky a doporučení implementace
- **Transformační šablony** by měly být jednoduché a verzeované (např. mapování typu: věta → pole A; rozdělení seznamu → více položek).  
