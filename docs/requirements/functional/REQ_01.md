# REQ_XX

**Název:** Automatické doplnění identifikačních údajů úřadu z veřejných registrů
**Typ:** funkční/datový  
**Stav:** koncept  
**Jistota:** vysoká | střední | nízká  
**Priorita:** vysoká

## Požadavek <!-- Doladit ako to znie -->
Asistent musí automaticky předvyplnit sekci **„Základní údaje Informační koncepce“** (`IKOVS_PREFACE_BASIC-INFO_INFO-TABLE`) 
na základě veřejně dostupných registrů a systémů veřejné správy.  
Všechna pole s deterministickými údaji (např. název úřadu, IČO, sídlo, typ úřadu, identifikátor ISVS/eSSS, datum, verze) se vyplní bez zásahu uživatele.

Každý vyplněný údaj musí obsahovat **zdroj** (např. RPP, ROS, RÚIAN, ARES, eSSS) a datum načtení.  
V případě konfliktu mezi zdroji musí systém označit hodnotu jako „nejistou“ a připravit ji pro následnou kontrolu (viz REQ_03).

## Odůvodnění
Základní identifikační údaje úřadu jsou dostupné z oficiálních registrů, jejichž využití minimalizuje chybovost, manuální práci a rozdíly mezi úřady.  
Tato část dokumentu je čistě faktická a nevyžaduje AI-generované texty. Cílem je zajistit **přesnost, ověřitelnost a trasovatelnost** dat.

## Předpoklady
- Asistent má přístup k veřejným API nebo exportům následujících registrů: <!-- Zistit ktoré info sú důležité a potrebné-->
  - **RPP – Registr práv a povinností**
  - **ROS – Registr osob**
  - **RÚIAN – Registr územní identifikace, adres a nemovitostí**
  - **ARES – Administrativní registr ekonomických subjektů**
  - **eSSS / ISVS – Evidence informačních systémů veřejné správy**
- Propojení s registry probíhá prostřednictvím dostupných veřejných API, otevřených dat nebo data setů.
- Úřad je identifikován pomocí IČO nebo eSSS identifikátoru. <!-- Nejistota. Zjisti...-->

## Následný stav
Po splnění požadavku bude asistent schopen:
- automaticky vyplnit všechna faktická pole v tabulce(`IKOVS_PREFACE_BASIC-INFO_INFO-TABLE`)
- zobrazit u každého údaje zdroj (např. „RPP, verze 2025-09“),
- označit sporné údaje a připravit je pro validaci,
- doplnit systémové údaje (datum generování, verze, identifikátor instance dokumentu).

## Akceptační kritéria
- Asistent načte platná data z uvedených registrů (RPP, ROS, RÚIAN).
- Asistent se pokusí vyplnit všechna povinná pole sekce **„Základní údaje Informační koncepce“** (`IKOVS_PREFACE_BASIC-INFO_INFO-TABLE`) z dostupných zdrojů.
- Pole, která nelze vyplnit nebo jsou nejednoznačná, jsou označena jako „neúplná“ a doplněna vysvětlující poznámkou.
- Každý údaj obsahuje viditelný záznam o zdroji a datu načtení.
- Export do šablony Word/JSON zachová trasovatelnost zdrojů.

## Vstupy
- IČO úřadu (vstupní identifikátor)
- Veřejné registry (RPP, ROS, RÚIAN, ARES, eSSS)
- Metadata o generování (datum, verze)

## Výstupy
- Vyplněná tabulka (`IKOVS_PREFACE_BASIC-INFO_INFO-TABLE`) v šabloně IK OVS
- Strukturovaný JSON/YAML obsahující vyplněné hodnoty a jejich zdroje
- Log záznam o načtení a případných konfliktech

## Závislosti
- REQ_02 – AI doplnění kontextových polí  
- Přístupové moduly k registrům (integrace)

## Poznámky
- Tento požadavek se týká pouze **deterministických polí** (faktických údajů).  
- Textová a kontextová pole (např. poznámky, odkazy, shrnutí) jsou řešena v REQ_02.  
- Zajištění právního souladu se Zákonem č. 365/2000 Sb. a Vyhláškou 360/2023 Sb. ????


