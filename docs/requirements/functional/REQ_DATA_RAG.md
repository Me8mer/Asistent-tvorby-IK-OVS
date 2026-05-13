# REQ_DATA_RAG

**Název:** Mechanismus pro dohledávání a filtrování relevantního kontextu (RAG)
**Typ:** funkční / infrastrukturní
**Stav:** koncept
**Jistota:** vysoká
**Priorita:** kritická

## Požadavek
Systém musí obsahovat mechanismus pro inteligentní dohledávání, filtrování a správu kontextu (Retrieval-Augmented Generation – RAG) z externích i interních zdrojů. Pro každý cílový prvek (pole, tabulka, sekce) musí být asistent schopen identifikovat nejrelevantnější úryvky z dostupných zdrojů (legislativa, metodika, web úřadu, registry) a tyto úryvky předat AI modelu jako kontext pro generování návrhu. Mechanismy musí zajistit, aby kontext nebyl zahlcen nerelevantními daty a aby byla zachována přímá vazba mezi úryvkem a původním zdrojem pro zajištění trasovatelnosti.

## Odůvodnění
Kvalita AI návrhů přímo závisí na kvalitě a relevanci poskytnutého kontextu. Samotné „stažení“ velkého množství textu nestačí; asistent musí umět vybrat pouze ty části, které skutečně pomáhají vyplnit danou část dokumentu. Tento mechanismus minimalizuje riziko halucinací (vymýšlení faktů) a zajišťuje, že návrhy jsou podložené konkrétními a dohledatelnými zdroji.

## Předpoklady
- Jsou definovány zdroje v `sources.md`.
- Existuje indexovací mechanismus (např. vektorová databáze nebo fulltextové vyhledávání) pro efektivní prohledávání textů.
- Pole v interním modelu mají definovány klíčová slova nebo parametry, které usnadňují cílené vyhledávání kontextu.

## Následný stav
- Systém pro každý prvek připravený k AI doplnění automaticky dohledá sadu kontextových úryvků (snippets).
- Každý úryvek nese metadata: `source_alias`, přesné umístění ve zdroji (např. URL, kapitola, odstavec) a skóre relevance.
- AI model dostává v promptu strukturovaný kontext s jasným oddělením jednotlivých zdrojů a instrukcí k jejich citování.

## Akceptační kritéria
- Systém úspěšně dohledá relevantní texty pro pole typu `AI_fill`.
- Kontext předávaný LLM obsahuje pouze relevantní úryvky (omezení celkové délky promptu a prahová hodnota relevance).
- Každý vygenerovaný návrh `ai_proposal` obsahuje přímé reference na konkrétní úryvky použité k jeho vytvoření.
- Uživatel má možnost v rozhraní nahlédnout do použitého kontextu u každého návrhu.

## Vstupy
- Identifikátor pole/sekce a jeho metadata (popis, očekávaný typ).
- Externí a interní zdroje dat (PDF metodiky, weby úřadů, legislativa v textové podobě).
- Pravidla pro filtrování a chunking (rozčlenění textu na zpracovatelné kousky).

## Výstupy
- Sada relevantních kontextových úryvků s metadaty připravená pro LLM prompt.
- Log o úspěšnosti dohledání (počet nalezených vs. skutečně použitých úryvků).

## Závislosti
- `REQ_02`, `REQ_03`, `REQ_04` – tyto požadavky přímo využívají RAG pro generování návrhů.
- `sources.md` – definice a parametry datových zdrojů.

## Poznámky
- Implementace by měla podporovat hybridní vyhledávání (kombinace sémantického vyhledávání pomocí embeddingů a klasického vyhledávání podle klíčových slov).
