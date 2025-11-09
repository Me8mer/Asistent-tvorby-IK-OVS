# REQ_05

**Název:** Odvozené texty generované z potvrzených tabulek a sekcí  
**Typ:** funkční / transformační  
**Stav:** koncept  
**Jistota:** nízká
**Priorita:** vysoká / střední

## Požadavek
Asistent vygeneruje návrhy textových bloků pro sekce šablony IK OVS, které jsou deterministicky odvozeny z již vyplněných a potvrzených tabulek nebo jiných sekcí. Generování se spouští až po potvrzení zdrojových dat a výsledné návrhy se uloží jako ai_proposal s jasnou vazbou na zdrojová data a na použité transformační pravidlo.

Každý návrh musí obsahovat explicitní reference na původní položky (alias tabulky, row_id, pole), popis transformace (verze šablony nebo pravidla), čas vytvoření a číselnou úroveň jistoty. Návrhy zůstávají v modelu ve stavu ai_proposal do okamžiku, kdy je uživatel akceptuje nebo upraví..

## Odůvodnění
Některé sekce dokumentu lze vyplnit pouze po dokončení specifických tabulek nebo částí.  
Tento přístup šetří čas při následném shrnování a přepisování údajů, které již byly vyplněny,  
a umožňuje automaticky vytvářet návrhy textů, které mohou být dále využity nebo upraveny uživatelem.

## Předpoklady
- Zdrojové tabulky a sekce jsou vyplněné a mají stav `confirmed` nebo `final` (nebo jsou jasně označené, které hodnoty jsou potvrzené).  
- Každý řádek v tabulce má unikátní identifikátor a alias sekce, dostupné v metadatech.  
- Jsou definována pravidla nebo šablony pro transformaci dat do textu (např. vzory vět, agregace, seznamy).  
- AI modul umí pracovat s datovými vstupy a generovat text v češtině s kvantifikací jistoty.  

## Následný stav
Po provedení bude k dispozici:
- Interní model obsahuje ai_proposal blok pro cílovou sekci s metadaty
- explicitní vazby na originální data (tabulka:alias, řádek:id, pole:name),  
- stav návrhu `AI návrh` až do uživatelského potvrzení, po potvrzení se stav změní na `confirmed` a zaznamená se do auditu,  
- možnost regresního vygenerování při změně zdrojových dat s verzováním návrhů.

## Akceptační kritéria
- Systém vygeneruje text, který obsahuje referenci na zdrojová data (alespoň alias tabulky a id řádku pro zmíněné položky).  
- Každý návrh obsahuje metadata `confidence` a popis použité transformační šablony nebo pravidla.  
- Návrhy jsou označeny jako `AI návrh` a nejsou považovány za finální, dokud je uživatel nepotvrdí.  
- Uživatel může návrhy editovat, potvrdit nebo zamítnout, změny jsou auditovány.  
- Pokud se změní zdrojová tabulka, lze spustit regeneraci návrhu a systém zachová historii předchozích verzí.  

## Vstupy
- Potvrzené tabulky a sekce s metadaty (alias tabulky, id řádků, pole)  
- Transformační šablony nebo pravidla pro cílovou sekci  
- Doplňující kontext z `REQ_01`, `REQ_02`, `REQ_03` a `REQ_04`  
- Parametry generování (maximální délka, požadovaný level detailu)
- Stylová šablona / příklady IK pro referenci
- Vstupní identifikátor úradu
- Veřejné zdroje 

## Výstupy
- Návrh textového bloku s metadaty  
- JSON/YAML export s vazbami na originální data a stavovými flagy  
- Auditní log s historií generování, úprav a potvrzení

## Závislosti
- `REQ_01` – deterministická data (mohou sloužit jako část vstupu)  
- `REQ_02` – AI návrhy pro doplnění polí (mohou být integrovány)  
- `REQ_03` – AI generované tabulky (zdroj dat pro odvozené texty)  
- `REQ_04` – AI návrhy textů z veřejných zdrojů (mohou doplnit kontext)  
- `sources.md` – seznam zdrojů a aliasů

## Poznámky
- Transformace musí být deterministicky popsatelná, tj. každé pravidlo musí mít jasný vstup a očekávaný výstup a být verzeováno.  
- Všechny generované texty musí být plně auditovatelné a obsahovat odkaz na původní data (alias tabulky a id řádku).  
- Generované texty mohou být použity jako kontext pro následné interaktivní chatboty a dotazovací workflow.
