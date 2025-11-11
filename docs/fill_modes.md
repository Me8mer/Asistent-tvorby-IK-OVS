# Fill-módy asistenta

| ID | Název režimu | Popis | Odkaz |
|----|---------------|--------|-------|
| FILL_01 | Deterministické vyplnění | Automatické doplnění faktických polí z důvěryhodných registrů. | [FILL_01](#fill_01) |
| FILL_02 | AI vyplnění bez kontextu | Automatické nebo manuální doplnění polí a tabulek bez závislostí. | [FILL_02](#fill_02) |
| FILL_03 | AI vyplnění z kontextu | Generování hodnot na základě potvrzených nebo jistých dat z jiných částí dokumentu. | [FILL_03](#fill_03) |
| FILL_04 | Chatbotové vyplnění | Interaktivní doplnění polí, tabulek a sekcí po konverzaci s uživatelem. | [FILL_04](#fill_04) |
| FILL_05 | Manuální vyplnění | Pole, která zůstala prázdná po předchozích módech a byla vyplněna uživatelem. | [FILL_05](#fill_05) |

---


## FILL_01  

**Název:** Deterministické vyplnění  
**Typ:** automatické  
**Fáze:** po inicializaci a výpočtu závislostí  
**Zdroje:** RPP, ARES, WEB_OFFICE, ARCHI_PORTAL  
**Cíl:** vyplnit všechna jednoznačně zjistitelná faktická pole  
**Příklady:** [EX_CELL_FILL](#ex_cell_fill), [EX_TEXT_PUBLIC](#ex_text_public)

Asistent automaticky doplní hodnoty všech polí označených `deterministic_fill`.  
Hodnota se uloží jako `deterministic`, pokud je jednoznačná a ověřená.  
Nejasná nebo chybějící data se označí `needs_validation`.  
Nepřepisují se žádné hodnoty se stavem `confirmed`, `ai_proposal` nebo `user_input`.  

**Výstup:** aktualizovaný interní model s příznaky `deterministic` / `needs_validation` a auditní log zdrojů.  

---

## FILL_02  

**Název:** AI vyplnění bez kontextu  
**Typ:** generativní  
**Spuštění:** automaticky po deterministické fázi nebo manuálně při regeneraci sekce  
**Cíl:** doplnit pole a tabulky bez závislostí  
**Příklady:** [EX_CELL_FILL](#ex_cell_fill), [EX_ROW_FILL](#ex_row_fill), [EX_TABLE_PUBLIC](#ex_table_public), [EX_TEXT_PUBLIC](#ex_text_public)


Asistent spouští základní AI doplnění ihned po deterministickém kole.  
Vyplňuje pouze ta pole, která jsou:
- označená `deterministic_fill` a zůstala prázdná (`empty`),  
- nebo označená `AI_fill`, pokud nejsou blokovaná závislostmi.

Generuje návrhy (`ai_proposal`) pro jednotlivá pole, tabulky nebo celé sekce podle aliasu a typu obsahu.  
Při manuálním spuštění (např. „Regenerovat sekci“) se postupuje shodně, jen se omezuje rozsah na daný výběr.  

### Pod-mód: Instrukční AI Fill
Pokud má pole uložené specifické instrukce nebo příklad promptu,  
použijí se pro řízení výstupu. Tyto instrukce mohou definovat styl, formát nebo rozsah hodnot.

**Výstup:** návrhy vytvořené z dostupných veřejných zdrojů a šablon, připravené k potvrzení uživatelem.  

---

## FILL_03

**Název:** AI vyplnění z kontextu  
**Typ:** generativní závislé  
**Spuštění:** po potvrzení předchozích dat nebo manuálně v sekci s platným kontextem  
**Cíl:** doplnit části dokumentu podle existujících hodnot  
**Příklady:** [EX_TABLE_CONTEXT](#ex_table_context), [EX_TEXT_CONTEXT](#ex_text_context), [EX_SECTION_SUMMARY](#ex_section_summary), [EX_ROW_FILL](#ex_row_fill)


Asistent využívá data, která už jsou v dokumentu potvrzená (`confirmed`),  
deterministická nebo s vysokou jistotou.  
Z těchto údajů odvozuje další návrhy pro sekce, tabulky a pole, které jsou na ně logicky nebo tematicky navázané.  
Kontext může být sbírán napříč sekcemi i celými kapitolami (cross-section / cross-chapter).

### Pod-mód: Instrukční AI Fill
Při generování může být využit doplňující prompt nebo šablona,  
která přesněji určuje, jak má být obsah vytvořen (např. formální styl, rozsah, hierarchie údajů).

**Výstup:** návrhy odvozené z potvrzených nebo jistých dat, včetně vazeb na původní kontext.

---

## FILL_04

**Název:** Chatbotové vyplnění  
**Typ:** interaktivní  
**Spuštění:** manuálně uživatelem (spustit chatbot pro sekci) nebo po iniciaci po deterministickém a AI vyplnění.  
**Cíl:** získat od uživatele hodnoty, které nelze spolehlivě zjistit deterministicky nebo AI, a uložit je jako návrhy k potvrzení
**Příklady:** [EX_CELL_FILL](#ex_cell_fill), [EX_ROW_FILL](#ex_row_fill), [EX_TEXT_CONTEXT](#ex_text_context), [EX_TEXT_PUBLIC](#ex_text_public)

**Krátký popis**
Chatbot vede řízenou konverzaci podle předdefinovaných scénářů. Odpovědi uživatele se mapují na konkrétní aliasy polí/tabulek/sekcí a ukládají se jako návrhy stavu `ai_interactive`. Systém vždy vyžaduje explicitní potvrzení nebo úpravu před tím, než se hodnota označí `confirmed`. Jedna odpověď může být použita k vyplnění více navázaných polí (cross-section nebo cross-chapter fill).  

**Vstupy**
- Scénář otázek pro cílový alias (question flow)  
- Kontext sekce (aktuální hodnoty, závislosti)  
- Identita uživatele a oprávnění

**Výstupy**
- Pro každou odpověď záznam typu Q/A mapovaný na aliasy (pole)  
- Navržené hodnoty uložené jako `ai_interactive` s vazbou na Q/A  
- Auditní záznam konverzace

---

## FILL_05

**Název:** Manuální vyplnění  
**Typ:** manuální  
**Spuštění:** při importu/merge sekce nebo při přímém uživatelském vstupu  
**Cíl:** uložit hodnoty, které nebylo možné automaticky doplnit, a zpřístupnit je jako kontext pro další doplnění

Pole vyplněná uživatelem se označí stavem `manual_fill` (nebo `user_input`) a uloží se s auditem. Tyto hodnoty lze následně použít jako kontext pro další AI nebo deterministické vyplnění.

---

# Příklady (generické, s aliasy)

## EX_CELL_FILL
**Název:** Doplnení buňky do tabulky  
**Kdo může generovat:** FILL_01, FILL_02, FILL_03, FILL_04, FILL_05  
**Použití:** doplnit jednu konkrétní buňku v existující tabulce.  

## EX_ROW_FILL
**Název:** Doplnení řádku do tabulky  
**Kdo může generovat:** FILL_02, FILL_03, FILL_04, FILL_05  
**Použití:** vyplnit celý řádek včetně více polí. AI může navrhnout řádek, zbytek ponechat na manuální doplnění.  

## EX_TABLE_PUBLIC
**Název:** Vygenerování celé tabulky z veřejných zdrojů  
**Kdo může generovat:** primárně FILL_02
**Použití:** sestavit tabulku úplně z externích datasetů pokud pokrývají obsah.  
**Klíčové poznámky:** každé pole má source + confidence; generovat jako návrh s možností postupného potvrzení.

## EX_TABLE_CONTEXT
**Název:** Vygenerování celé tabulky z kontextu (včetně veřejných zdrojů)  
**Kdo může generovat:** primárně FILL_03 
**Použití:** tabulka odvíjející se od hodnot v jiných sekcích nebo kapitolách.  

## EX_TEXT_PUBLIC
**Název:** Vygenerování textové sekce z veřejných zdrojů  
**Kdo může generovat:** FILL_02, podporne FILL_03  
**Použití:** formální texty nebo citace odvoditelné z legislativy či oficiálních materiálů.  

## EX_TEXT_CONTEXT
**Název:** Vygenerování textové sekce z kontextu  
**Kdo může generovat:** FILL_03 (hlavní), FILL_02 podporne  
**Použití:** texty odvozené z potvrzených tabulek nebo jiných sekcí.  

## EX_SECTION_SUMMARY
**Název:** Shrnutí sekce / kapitoly  
**Kdo může generovat:** FILL_03 (hlavní), podporne FILL_02  
**Použití:** shrnutí obsahu sekce nebo kapitoly na základě potvrzených dat.  
