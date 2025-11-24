# REQ_CHAT_INIT_QA

**Název:** Inicializační konverzace pro získání obecných údajů o úřadu  
**Typ:** funkční / interaktivní  
**Stav:** koncept  
**Priorita:** vysoká  

## Požadavek

Asistent při inicializaci dokumentu spustí řízený dialog s uživatelem. Cílem je získat hodnoty pro předem definovaný seznam kandidátních polí, která se často používají při vyplňování IK OVS.

Tento dialog je speciální scénář chatbota podle `REQ_CHAT_01` určený pro inicializační fázi.

## Vstupy

Konfigurační seznam kandidátních polí. Pro každé pole je definováno:

- interní identifikátor kandidátního pole  
- typ hodnoty (text, číslo, datum, jiný)  
- jeden nebo více textových vzorů otázek (příklady)  
- pravidla validace odpovědi (minimálně základní kontrola typu / formátu)  

## Průběh
1. Pro každé kandidátní pole:
   - ověří, zda je relevantní pro aktuální dokument (například zda existuje mapovaný alias)  
   - zkontroluje, zda už nemá hodnotu z jiného zdroje (deterministické doplnění, cache)  
2. U polí, která jsou relevantní a bez spolehlivé hodnoty:
   - chatbot položí jednu z konfigurovaných otázek, případně ji nechá přeformulovat LLM na základě příkladu  
   - přijme odpověď uživatele a zvaliduje ji podle pravidel daného pole  
3. Pokud je odpověď akceptovatelná:
   - uloží ji do cache podle `REQ_CHAT_03` pod definovaným klíčem  
   - pokud existuje mapování na aliasy, zapíše hodnotu do odpovídajícího pole interního modelu jako návrh typu `ai_interactive`  
4. Pokud odpověď chybí nebo je uživatel odmítne poskytnout:
   - uloží stav pole jako nevyplněné, případně `not_applicable` s důvodem  

Inicializační dialog proběhne maximálně jednou na začátku práce s dokumentem. Pozdější dotazy chatbota podle `REQ_CHAT_01` mohou využít hodnoty z cache a nemusejí se ptát znovu na totéž.

## Výstupy

Po skončení inicializačního dialogu:

- pro každé kandidátní pole je znám aktuální stav:
  - vyplněno a uloženo v cache  
  - zapsáno do interního modelu (pokud je mapování)  
  - nevyplněno
- interní model obsahuje u mapovaných polí:
  - hodnotu z inicializačního dialogu  
  - informaci, že pochází z interaktivního chatu  
- cache obsahuje sadu odpovědí, které lze znovu použít v dalších krocích  

## Závislosti

- `REQ_MODEL_DEPENDENCIES`  
- `REQ_CHAT_01` (obecné chování chatbota)  
- `REQ_CHAT_03` (ukládání a použití odpovědí v cache)  

## Poznámky

- LLM může měnit formulaci otázky
