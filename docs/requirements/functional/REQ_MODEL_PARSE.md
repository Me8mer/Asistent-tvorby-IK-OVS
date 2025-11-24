# REQ_MODEL_PARSE

**Název:** Parsování SDT do interního JSON modelu  
**Typ:** funkční / infrastrukturní  
**Stav:** koncept  
**Jistota:** střední  
**Priorita:** vysoká  

## Požadavek

Asistent po úspěšné validaci šablony podle `REQ_TPL_CORE_MIN` provede parsování všech SDT prvků v šabloně IK OVS do interního JSON modelu.

Cílem je vytvořit prázdný, ale kompletní interní model dokumentu, který:

- kopíruje strukturu šablony na úrovni dokument → kapitola → sekce → pole  
- pro každé pole vytvoří záznam s aliasem, typem hodnoty a výchozím stavem  
- spojí aliasy z šablony s interními identifikátory tak, aby na ně mohly navazovat další kroky inicializace, exportu, merge a validace  

Minimální rozsah parsování:

- načtení všech SDT v šabloně (textové kontroly, buňky tabulek a další použité typy SDT)  
- přiřazení aliasu a umístění v dokumentu (kapitola, sekce, tabulka, řádek, buňka)  
- vytvoření interního JSON modelu, kde je pro každé pole uloženo alespoň:
  - stabilní identifikátor pole v modelu  
  - alias (tag ze šablony)   
  - datový typ nebo základní kategorie (text, číslo, datum, podobně)  
  - aktuální hodnota, na začátku prázdná  
  - stav `state` nastavený na `empty`  

V tomto kroku se žádná data nevyplňují, pouze se vytváří struktura a výchozí stav modelu.

## Odůvodnění

Deterministické doplnění (`REQ_01`), AI doplnění (`REQ_02` až `REQ_04`), exporty a merge operace pracují s interním modelem a stavem polí. Bez konzistentního parsování SDT do jednotného JSON modelu by nebylo možné:

- jednoznačně mapovat hodnoty na konkrétní pole  
- rozhodovat podle stavu pole (`empty`, `needs_validation`, `ai_proposal`, `user_input`)  
- spolehlivě provádět export a následný merge na úrovni sekcí a kapitol  

Tento požadavek vytváří technický základ pro všechny navazující kroky inicializace.

## Předpoklady

- Proběhla vstupní validace šablony podle `REQ_TPL_CORE_MIN` a existuje:
  - identifikátor šablony a její verze  
  - alias mapa kapitol, sekcí a hlavních struktur  
- K dispozici je knihovna schopná číst DOCX a SDT prvky včetně jejich aliasů a umístění.

## Výstupy

- Interní JSON model dokumentu obsahující:
  - root objekt dokumentu s identifikátorem a verzí šablony  
  - seznam kapitol a sekcí podle struktury šablony  
  - seznam polí, kde je pro každé pole uložen alias, umístění, typ, aktuální hodnota a stav `state = empty`  
- Pomocná struktura (například slovník) pro rychlé vyhledání pole podle aliasu  

## Závislosti

- `REQ_TPL_CORE_MIN`  
 
## Poznámky

- Struktura JSON modelu musí být stabilní pro potřeby exportu a merge, ale může se vyvíjet podle potřeb dalších funkcí, pokud zůstane zachovaná zpětná kompatibilita.  

