# REQ_01

**Název:** Automatické doplňování deterministických dat z veřejných zdrojů.
**Typ:** funkční / datový  
**Stav:** koncept  
**Jistota:** střední / vysoká  
**Priorita:** vysoká

## Požadavek
Asistent automaticky vytváří nebo předvyplňuje specifická pole v tabulkách šablony IK OVS,  
pokud jejich hodnoty lze jednoznačně určit z veřejných či oficiálních datových zdrojů.  

Deterministická data zahrnují zejména identifikační, organizační a evidenční údaje úřadu  
(např. název, IČO, sídlo, identifikátor ISVS/eSSS, agendu, kontaktní údaje).  

Vyplnění probíhá bez zásahu uživatele. Každý doplněný údaj obsahuje informaci o **zdroji** a **datumu načtení**.  
Pokud nelze hodnotu spolehlivě určit nebo se zdroje liší, pole je označeno jako **nejisté**  
a předáno k následné validaci.

## Odůvodnění
Základní identifikační údaje úřadu jsou dostupné z oficiálních registrů.
Tato část dokumentu je čistě faktická a nevyžaduje AI-generované texty. Cílem je zajistit **přesnost, ověřitelnost a trasovatelnost** dat.

## Předpoklady
- Asistent má přístup k zdrojúm **RPP**, **WEB_OFFICE**, ***ARCHI_PORTAL**
- Propojení s registry probíhá prostřednictvím dostupných veřejných API, otevřených dat nebo data setů.<!-- Nejistota. Zjisti...-->
- Úřad je identifikován pomocí IČO nnebo jiného unikátního identifikátoru. <!-- Nejistota. Zjisti...-->
- pole na vyplnení jsou specificky označená pro doplnění

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
- Metadata o generování (datum, verze)

## Výstupy
- Vyplněné deterministické tabulky šablony IK OVS  
- Strukturovaný JSON/YAML obsahující vyplněné hodnoty a jejich zdroje
- Auditní log s konfliktními nebo nejistými poli

## Závislosti 
- `REQ_02` – AI doplňování textových a kontextových polí  
- `REQ_03` – Validace konzistence a úplnosti dat  
- Přístupové moduly k veřejným datovým zdrojům <!-- Find out -->

## Poznámky
- Tento požadavek se týká pouze **deterministických polí** (faktických údajů).   
- Zajištění právního souladu se Zákonem č. 365/2000 Sb. a Vyhláškou 360/2023 Sb. <!-- Find out -->


