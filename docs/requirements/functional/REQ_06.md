# REQ_06

**Název:** Validace a kontrola souladu výstupů s požadavky a legislativou  
**Typ:** funkční / kontrolní  
**Stav:** koncept  
**Jistota:** střední  
**Priorita:** vysoká  

## Požadavek
Asistent provede vícestupňovou validaci všech částí dokumentu IK OVS po dokončení jejich vyplnění.  
Validace kontroluje formální správnost, úplnost, věcnou konzistenci a soulad s legislativními a metodickými požadavky.  

Cílem je zajistit, že výsledný dokument odpovídá:
- požadavkům **Zákona č. 365/2000 Sb.**, o informačních systémech veřejné správy,  
- **Vyhlášce č. 360/2023 Sb.**, o informační koncepci OVS,  
- a doporučením z **Informační koncepce ČR (IK ČR)** a **Metodiky řízení ICT VS (MRICT)**.

Validace se provádí kombinací deterministických kontrolních pravidel (formát, přítomnost, struktura)  
a AI hodnocení obsahu (věcný a metodický soulad).  
Každý nalezený problém je klasifikován jako **chyba**, **varování** nebo **poznámka**  
a je doplněn odkazem na příslušný zdroj požadavku.

## Odůvodnění
IK OVS musí splňovat přesně stanovené legislativní a metodické požadavky.  
Manuální kontrola je časově náročná a náchylná k chybám.  
Automatizovaná validace pomúže pri procesu ověření správnosti,  
umožní rychlou zpětnou vazbu a označí části, které je třeba upravit nebo doplnit.  
Hybridní přístup (deterministická + AI validace) umožní spojit přesnost formálních pravidel s kontextovým hodnocením obsahu.

## Předpoklady
- Jsou k dispozici definice validačních pravidel (checklisty, kontrolní seznamy, formální šablony).  
- V `sources.md` jsou uvedeny všechny právní a metodické dokumenty, které určují validační požadavky.  
- Všechny části dokumentu (tabulky, texty, sekce) obsahují metadata o zdrojích, stavech a úrovni jistoty.  
- AI komponenta má schopnost:
  - analyzovat text v češtině a porovnávat ho s obsahem právních a metodických dokumentů,  
  - detekovat chybějící nebo nesouladné části,  
  - generovat shrnutí validačních výsledků s úrovní jistoty.  
- Validace probíhá jako samostatný krok po dokončení vyplňování nebo před exportem.

## Následný stav
Po dokončení validace bude k dispozici:
- souhrnný validační report obsahující seznam všech nalezených chyb, varování a poznámek,  
- přehled souladu s jednotlivými zdroji (zákon, vyhláška, metodika),  
- možnost přejít z reportu přímo na označené místo v dokumentu,  
- validační metadata uložená v každé části dokumentu (`validation_status`, `issues[]`, `validated_at`),  
- možnost opakované validace po úpravách.

## Akceptační kritéria
- Systém provede formální validaci všech částí dokumentu (kontrola formátu, povinných polí, struktury).  
- AI validace zkontroluje textové sekce na základě obsahu vyhlášky 360/2023 Sb. a IK ČR a označí chybějící části.  
- Každý výsledek obsahuje odkaz na konkrétní ustanovení nebo metodické doporučení, které kontrolu vyvolalo.  
- Report obsahuje klasifikaci výsledků: **chyba**, **varování**, **poznámka**, **OK**.  
- Export do Word/JSON/YAML obsahuje i výsledky validace a jejich stav.  
- Uživatel může validaci spustit znovu po úpravách a porovnat nové výsledky s předchozí verzí.  
- Všechny validační výsledky jsou uloženy do auditního logu s časem a verzí dokumentu.

## Vstupy
- Kompletně vyplněné tabulky a textové sekce s metadaty  
- Definice validačních pravidel a kontrolních šablon  
- Parametry validace (úroveň podrobnosti, jazyk, verze pravidel)
- Vstupní identifikátor úradu
- Veřejné zdroje 

## Výstupy
- Validační report (Word/JSON/YAML) se seznamem výsledků  
- Anotace chyb a upozornění v dokumentu  
- Metadata validace uložená v jednotlivých sekcích  
- Auditní log validací (čas, verze dokumentu, počet chyb, soulad)

## Závislosti
- `REQ_01` – deterministické doplnění polí (formální validace formátů a přítomnosti)  
- `REQ_02` – AI doplnění neúplných polí (ověření kvality a jistoty návrhů)  
- `REQ_03` – AI generované tabulky (validace obsahu a úplnosti)  
- `REQ_04` – AI texty z veřejných zdrojů (věcná validace správnosti obsahu)  
- `REQ_05` – odvozené texty z tabulek (kontrola souladu mezi tabulkami a odvozeným textem)  
- `sources.md` – zdroje legislativy a metodických rámců

## Poznámky
- Každé validační pravidlo by mělo mít definován zdroj (např. článek, odstavec, kapitolu) a verzi.  
- AI validace nesmí měnit obsah dokumentu. Pouze komentovat nebo označit problém.  
