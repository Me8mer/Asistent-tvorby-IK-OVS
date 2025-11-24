# REQ_MODEL_DEPENDENCIES

**Název:** Výpočet závislostí polí a stavu ready/blocked  
**Typ:** funkční / infrastrukturní  
**Stav:** koncept  
**Priorita:** vysoká  

## Požadavek

Asistent po vytvoření interního JSON modelu podle `REQ_MODEL_PARSE` spočítá závislosti mezi poli a označí každé pole jako `ready` nebo `blocked` pro další vyplňování.

Funkce musí:

- pro každé pole zjistit, na jakých jiných polích nebo externích zdrojích závisí  
- vytvořit interní graf závislostí (pole → seznam aliasů, na kterých závisí)  
- provést výpočet stavu:
  - `ready` pokud nemá žádné závislosti nebo všechny závislosti mají platnou hodnotu  
  - `blocked` pokud je alespoň jedna závislost bez hodnoty nebo ve stavu, který neumožňuje další odvození  
- detekovat cykly nebo neřešitelné závislosti a označit taková pole jako `blocked` s důvodem  

Výpočet probíhá nad již existujícím modelem a nemění žádné hodnoty polí. Mění pouze jejich stav a doplňuje informace o závislostech.

## Odůvodnění

Deterministické doplňování, AI doplňování i chatbot mají pracovat jen s poli, která jsou připravená pro vyplnění. K tomu je potřeba znát minimálně:

- která pole mohou být vyplněna hned  
- která pole musí počkat na jiné vstupy  

Tento požadavek definuje mechaniku, podle které se pole rozdělují na `ready` a `blocked`.

## Vstupy

- interní JSON model dokumentu z `REQ_MODEL_PARSE`  
- konfigurační definice závislostí:
  - pravidla typu „pole B závisí na poli A“ přes aliasy  

## Výstupy

Rozšířený interní model, kde:

- každé pole obsahuje:
  - `depends_on`: seznam aliasů nebo identifikátorů, na kterých závisí  
  - `dependents`: volitelně seznam polí, která závisejí na tomto poli  
  - `ready_state`: hodnota `ready` nebo `blocked`  

## Závislosti

- `REQ_MODEL_PARSE`  

## Poznámky

- Počáteční implementace může podporovat jen jednoduché závislosti „A musí být vyplněno před B“.  
