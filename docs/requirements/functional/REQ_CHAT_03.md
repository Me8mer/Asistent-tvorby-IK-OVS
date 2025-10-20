# REQ_CHAT_03

**Název:** Paměť (cache) pro zapamatování klíčových odpovědí  
**Typ:** funkční / podpůrný  
**Stav:** koncept  
**Priorita:** vysoká

## Požadavek
Asistent ukládá potvrzené, důležité informace z proběhlých konverzací do paměti (cache), aby je mohl později interně využít při dalším vyplňování tabulek nebo sekcí a při generování otázek.

## Odůvodnění
Některé odpovědi se opakují nebo jsou důležité pro více částí dokumentu. Zapamatování těchto informací zjednodušuje workflow a pomůže asistentovi správně nastavit další kroky (např. které doplňující otázky položit).

## Předpoklady
- Je dostupné úložiště pro cache (technická implementace není zde specifikována).  
- Každá uložená položka bude mít identifikátor (např. alias sekce/pole), hodnotu a časové razítko; uložená informace by měla být spojitelná s konverzačním záznamem, který ji potvrdil. 
- Uživatelské rozhraní umožní zobrazit a případně vymazat uložené položky (správa paměti).

## Následný stav
- Důležité potvrzené informace jsou uloženy s metadaty (klíč, hodnota, čas, potvrzující uživatel, případná poznámka).  
- Asistent může při interním rozhodování čerpat z cache jako z kontextu (např. při sestavování dalších otázek nebo při vyhodnocení, které části dokumentu jsou už pokryty).  

## Akceptační kritéria
- Systém uloží potvrzenou informaci se základními metadaty: {klíč, hodnota, čas, potvrzující uživatel}.  
- Uložené položky jsou dostupné pro interní dotazy asistenta (retrieval) jako kontext pro další kroky.  
- Každá operace uložení/aktualizace/mazání položky je auditována (čas, uživatel, akce).

## Vstupy
- Potvrzené odpovědi z konverzace: {klíč (alias), hodnota, potvrzující uživatel, poznámka}  
- 
## Výstupy
- Uložené záznamy v cache s metadaty  
- Auditní log operací nad cache

## Závislosti

## Poznámky
- Cache je koncipována jako pomocný kontext pro asistenta;
