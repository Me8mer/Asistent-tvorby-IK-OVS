# REQ_01

**Název:** Automatické doplňování deterministických dat z veřejných zdrojů.
**Typ:** funkční / datový  
**Stav:** koncept  
**Jistota:** střední / vysoká  
**Priorita:** vysoká

## Požadavek
Asistent v rámci fáze Inicializace → Deterministické předvyplnění automaticky načte a zapíše faktická pole šablony IK OVS, která jsou označena jako deterministic_fill. Vyplnění proběhne pouze pokud lze hodnotu jednoznačně určit z důvěryhodných veřejných registrů. Každé uložené pole musí nést kompletní provenance metadata a stav pole v interním modelu. Pokud zdroje poskytují konfliktní nebo neúplné údaje, pole se nevyplní a nastaví se stav needs_validation pro následné kroky.

## Odůvodnění
Základní identifikační údaje úřadu jsou dostupné z oficiálních registrů.
Tato část dokumentu je čistě faktická a nevyžaduje AI-generované texty. Cílem je zajistit **přesnost, ověřitelnost a trasovatelnost** dat.

## Předpoklady
* K dispozici je vstupní identifikátor úřadu (např. IČO nebo jiný unikátní identifikátor).
- Asistent má přístup k zdrojúm **RPP**, **WEB_OFFICE**, ***ARCHI_PORTAL**
- Propojení s registry probíhá prostřednictvím dostupných veřejných API, otevřených dat nebo data setů.<!-- Nejistota. Zjisti...-->
- Pole na vyplnení jsou specificky označená pro doplnění

## Následný stav
Po splnění požadavku se asistent pokusí:
- automaticky vyplnit všechna faktická pole v tabulce
- zobrazit u každého údaje zdroj (např. „RPP, verze 2025-09“),
- označit sporné údaje a připravit je pro validaci,
- doplnit systémové údaje (datum generování, verze, identifikátor instance dokumentu).
- uložit výsledky v jednotné formě pro export i audit.

## Akceptační kritéria
- Asistent načte platná data z registrů
- Asistent se pokusí vyplnit všechna povinná pole z dostupných zdrojů.
- Nevyplnitelná nebo konfliktní data jsou označena jako „nejistá“.
- Každý údaj obsahuje viditelný záznam o zdroji a datu načtení.
- Export do šablony Word/JSON zachová trasovatelnost zdrojů.

## Vstupy <!-- Find out -->
- IČO úřadu (vstupní identifikátor)
- Veřejné zdroje
- Verzovaná DOCX šablona s SDT tagy

## Výstupy
- aAktualizovaný interní stav 
- Strukturovaný JSON/YAML obsahující vyplněné hodnoty a jejich zdroje
- Auditní log s konfliktními nebo nejistými poli

## Závislosti 
- Parsování SDT a vytvoření interního modelu (Inicializace).
- `REQ_02` – (AI doplnění) pracuje pouze s poli, která zůstala empty 
- Přístupové moduly k veřejným datovým zdrojům <!-- Find out -->

## Poznámky
- Tento požadavek se týká pouze **deterministických polí** (faktických údajů).   
- Zajištění právního souladu se Zákonem č. 365/2000 Sb. a Vyhláškou 360/2023 Sb. <!-- Find out -->


