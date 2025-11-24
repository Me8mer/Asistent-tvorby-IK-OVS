# REQ_TEMPLATE_VALIDATE

**Název:** Vstupní podmínky a minimální validace šablony IK OVS  
**Typ:** funkční / infrastrukturní  
**Stav:** koncept  
**Priorita:** střední  

## Požadavek

Asistent při načtení dokumentu zkontroluje, zda šablona IK OVS splňuje minimální technické a strukturální požadavky. Tento krok neprovádí parsování SDT prvků, nevytváří alias mapu a nebuduje interní model. Jediným cílem je potvrdit, zda je šablona použitelná pro pokračování inicializační fáze.

Minimální validace zahrnuje:

- dokument je formátu DOCX  
- dokument obsahuje SDT prvky (content controls)  
- šablona obsahuje metadata nebo identifikační prvek umožňující určit typ dokumentu a jeho verzi  
- žádné SDT nejsou prázdné nebo poškozené na úrovni XML   

V této fázi se nenačítají aliasy, nekontroluje se obsah tagů kromě základní technické integrity a nevytváří se žádný interní strom dokumentu.

Pokud ne, inicializace se zastaví a systém vrátí seznam kritických chyb.

## Odůvodnění

Pozdější kroky inicializace pracují s interním modelem polí, strukturou šablony, aliasy a závislostmi. Tyto funkce potřebují jistotu, že šablona je technicky stabilní a nevykazuje chyby na úrovni dokumentu DOCX. Tento minimální krok odděluje čistou validaci šablony od samotného parsování a vytváření modelu.

## Předpoklady

- dostupný DOCX dokument  
- knihovna schopná provést bezpečné otevření a kontrolu SDT prvků  
- definovaný seznam povinných částí šablony podle IK OVS  

## Výstupy

- seznam chyb v případě neúspěchu, například:
  - chybějící povinná část
  - nelze určit verzi šablony
  - obsahuje poškozené nebo nevalidní SDT  
- struktura dokumentu se v této fázi nevytváří

## Závislosti

- žádné další závislosti  
- vsrupní parametr
