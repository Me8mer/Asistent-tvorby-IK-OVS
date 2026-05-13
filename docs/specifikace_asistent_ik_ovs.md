# **Specifikace projektu Asistent tvorby IK OVS**

# **Úvod**

Asistent tvorby IK OVS je návrh systému pro podporu vyplňování Informační koncepce orgánu veřejné správy. Jeho cílem je usnadnit práci s rozsáhlou šablonou, ve které je potřeba doplňovat faktické údaje, textové části, tabulky a informace závislé na konkrétním úřadu. Asistent nemá dokument vyplnit zcela samostatně, ale má připravovat podklady, vytvářet návrhy a pomáhat uživateli rozhodnout, které části lze převzít, upravit nebo doplnit ručně.

Základní úlohou asistenta je rozlišit, jakým způsobem lze jednotlivé části dokumentu doplnit. Některé údaje je možné dohledat v důvěryhodných veřejných registrech, jiné lze navrhnout pomocí AI na základě veřejně dostupného kontextu a další je nutné získat přímo od pracovníků úřadu, například řízeným rozhovorem. U návrhů je důležité, aby bylo zřejmé, z jakého zdroje vycházejí, a aby uživatel měl možnost je zkontrolovat, přijmout, upravit nebo odmítnout.

Tato specifikace nejprve popisuje problémy, které musí asistent řešit, a základní typy údajů, se kterými pracuje. Následně představuje referenční workflow vycházející z našeho prototypu. Dokument se proto nesoustředí pouze na konkrétní implementaci, ale hlavně na obecné principy, které by měly být použitelné i při jiném technickém řešení asistenta.

# **Problémy, které asistent řeší**

1\. **Rozpoznání částí dokumentu, které je třeba doplnit.** Šablona IK OVS obsahuje mnoho míst, která je potřeba doplnit, upravit nebo ověřit. Tato místa mohou mít podobu krátkých polí, tabulek, textových bloků nebo celých sekcí. Asistent musí umět s těmito částmi pracovat jako se samostatnými jednotkami, ke kterým se váže aktuální hodnota, stav vyplnění, použitelný zdroj dat a případný návrh dalšího postupu. Konkrétní technické označení těchto míst závisí na implementaci. V mém prototypu se používají aliasy a SDT prvky v DOCX šabloně, ale obecně jde pouze o jednu možnou reprezentaci vyplnitelných částí dokumentu. Důležité je, aby asistent rozuměl tomu, co se má doplnit, jaký typ informace se očekává a jaké další části dokumentu mohou být s daným místem obsahově propojené.

2\. **Různé způsoby získávání informací.** Hodnoty v dokumentu nevznikají jedním jednotným způsobem. Některé údaje lze získat přímo z důvěryhodných registrů nebo jiných strukturovaných veřejných zdrojů, například název úřadu, IČO nebo adresa sídla. Jiné části dokumentu vyžadují zpracování veřejně dostupných textů, například informací z webu úřadu, metodických dokumentů nebo jiných relevantních zdrojů. Další údaje se ve veřejných zdrojích vůbec nacházet nemusí, protože závisí na interních procesech, rozhodnutích nebo plánech konkrétního úřadu. Asistent proto musí rozlišovat, zda má hodnotu dohledat deterministicky, navrhnout ji pomocí AI na základě dostupného kontextu, získat ji od uživatele řízeným rozhovorem, nebo ji odvodit až později z již potvrzených částí dokumentu.

3\. **Kvalita výstupu a transparentnost.** Asistent by měl u návrhů zobrazovat použité zdroje, například konkrétní registr, webovou stránku, metodický dokument nebo odpověď získanou od pracovníka úřadu. Návrhy musí vycházet z aktuálních a důvěryhodných datových zdrojů, které jsou uvedeny u návrhu. Pokud vhodný zdroj neexistuje nebo je informace nejistá, asistent by neměl hodnotu prezentovat jako hotovou, ale měl by ji označit jako návrh, otázku k ověření nebo nevyplněné místo.

4\. **Iterativní práce a spolupráce.** Dokument se nevytváří lineárně. Některé sekce se vyplňují současně, k jiným se uživatel vrací. Asistent by měl podporovat opakované generování návrhů, ruční úpravy, kombinaci automatického a manuálního vyplnění a možnost exportovat či importovat části dokumentu při spolupráci více lidí. Průběžně je třeba přepočítávat závislosti a nabízet nové návrhy na základě potvrzených údajů.

# **Důležitost správného získávání kontextu**

Při návrhu asistenta se ukázalo, že samotné napojení generativního modelu nestačí. Kvalita AI návrhů závisí hlavně na tom, jaký kontext model dostane před generováním. Pokud je získaný kontext neúplný, příliš obecný nebo nerelevantní, kvalita návrhu se výrazně snižuje a zvyšuje se riziko halucinací.

Proto je získávání správného kontextu jedním z nejdůležitějších problémů celého asistenta. Pro jednotlivé části IK OVS je nutné dohledat vhodné zdroje, například registry, metodické dokumenty, legislativu nebo webové stránky konkrétního úřadu, a z nich vybrat pouze ty úryvky, které skutečně pomáhají vyplnit dané pole, tabulku nebo sekci. Nestačí tedy jen stáhnout velké množství textu. Asistent musí umět kontext filtrovat, rozdělit na použitelné části a vybrat ty, které odpovídají právě řešené části dokumentu.

# **Kategorie polí a datových požadavků**

Analýza šablony vede k identifikaci čtyř skupin polí podle toho, jak se jejich hodnota získává. Tyto kategorie představují logický rámec, který musí podporovat jakýkoli asistent:

1\. **Deterministicky vyplnitelná pole.** Faktické identifikační údaje lze jednoznačně dohledat ve veřejných registrech. Systém je načte a uloží jen tehdy, je-li hodnota jednoznačná.

2\. **Pole generovatelná z veřejných informací.** Po dokončení deterministického doplnění zůstane řada textových polí prázdná. Zde nastupuje generativní AI. Model dostane kontext z veřejně dostupných zdrojů (weby úřadů, metodiky, legislativa) a navrhne hodnotu včetně popisu použitého zdroje a odvození. Tyto návrhy jsou uloženy zvlášť, například jako ai\_proposal, a nikdy automaticky nepřepisují potvrzené či deterministické hodnoty.

3\. **Pole vyžadující interaktivní rozhovor.** Některé informace nejsou dohledatelné ani na webu, ani v registrech. V takových případech se asistent ptá uživatele pomocí připravených otázek. Tento dialog může vést AI chatbot, který odpovědi zaznamená, validuje a uloží do interního modelu pro další použití.

4\. **Pole odvozovaná z potvrzených hodnot.** Některé sekce lze vyplnit až poté, co jsou potvrzené určité tabulky nebo části dokumentu. Asistent poté generuje odvozené texty nebo shrnutí na základě potvrzených dat. Návrhy jsou opět ukládány jako ai\_proposal s jednoznačnými referencemi na zdrojová data. Tento krok navazuje na předchozí tři a umožňuje například automaticky vygenerovat popis stávajícího stavu na základě vyplněných tabulek.

# **Navrhované workflow**

Specifikace popisuje referenční workflow, které náš prototyp implementuje. Jiné implementace asistenta mohou zvolit odlišné technické prostředky, ale měly by podporovat dvě obecné fáze: inicializační a iterativní.

## **Inicializační fáze**

1\. Načtení a validace šablony. Uživatel zadá identifikátor úřadu; systém načte správnou verzi šablony a zkontroluje, že všechny SDT (content controls) obsahují očekávané aliasy. Pokud chybí či jsou duplicitní, asistování se přeruší a vrátí seznam chyb.

2\. Parsování šablony do interního modelu. Všechny SDT se převedou do interního JSON modelu, který kopíruje strukturu dokumentu (kapitola, sekce, pole). Každé pole má stabilní identifikátor, alias, datový typ, výchozí hodnotu (prázdnou) a stav empty. Tato struktura je základem pro další kroky.

3\. Výpočet závislostí. Na základě metadat se vytvoří jednoduchý graf závislostí mezi poli. Graf určuje, která pole jsou blokována a která lze vyplnit hned. Při každé změně se graf aktualizuje.

4\. Deterministické doplnění. Systém načte všechna pole označená pro deterministické vyplnění a zkusí je naplnit z registrů a veřejných zdrojů. Přitom se zaznamenávají informace o zdroji. Pokud jsou data nejednoznačná, pole zůstává prázdné a je označeno pro validaci.

5\. AI doplnění. Pro pole, která zůstala prázdná, systém generuje návrhy pomocí získaného kontextu. Každý návrh obsahuje hodnotu, použitý kontext, metodu a úroveň jistoty. Návrhy se ukládají jako ai\_proposal a čekají na potvrzení.

6\. Inicializační chatbot. Pro vybranou podmnožinu polí, která jsou označena jako interaktivní a nemají hodnotu, se spustí řízený dialog s uživatelem. Otázky jsou předem definovány, odpovědi se validují a ukládají do modelu pro pozdější použití.

## **Iterativní fáze**

Po inicializaci je dokument připraven k iterativnímu vyplňování:

1\. Navigace kapitol a sekcí. Uživatelské rozhraní zobrazuje seznam kapitol se stavem vyplnění. Uživatel vybírá kapitoly a sekce, ve kterých pracuje.

2\. Generování a revize návrhů. V každé sekci může uživatel požádat o vygenerování nových návrhů (AI nebo deterministických). Systém zároveň zobrazuje přehled blokovaných polí a jejich závislostí. Uživatel může návrhy regenerovat, upravit, přijmout nebo odmítnout. Přijetí změní stav na confirmed, odmítnutí na rejected; historie se uchová.

3\. Další chatbot dotazy. Pokud jsou v sekci interaktivní pole, uživatel může spustit chatbot, který položí předem připravené otázky a uloží odpovědi.

4\. Export a merge (volitelně). Uživatel může exportovat sekci či kapitolu do Word nebo JSON formátu, vyplnit ji offline a poté ji sloučit zpět. Při merge se spárují aliasy, aktualizují hodnoty, přepočítají se závislosti a zvýrazní odblokovaná pole.

5\. Opakovaná AI generace. Po každé ruční editaci nebo merge se mohou spustit další AI návrhy nebo odvozené texty, které využívají potvrzená data. Tento cyklus se opakuje, dokud není sekce dokončena.

Tento cyklus se opakuje, dokud není sekce nebo celý dokument dokončen.

# **Technické a systémové požadavky**

1\. **Reprezentace vyplnitelných částí dokumentu.** Asistent by měl mít interní reprezentaci částí dokumentu, které je možné doplnit, upravit nebo ověřit. Těmito částmi mohou být jednotlivá pole, buňky tabulek, celé tabulky, textové bloky nebo sekce. Každá taková část by měla mít alespoň jednoznačný identifikátor, popis očekávaného obsahu, aktuální hodnotu, stav vyplnění a informaci o tom, jakým způsobem může být doplněna. Konkrétní technické řešení závisí na implementaci. V mém prototypu jsou použity SDT prvky a aliasy v DOCX šabloně, ale jiná implementace může použít jiný způsob označení a mapování vyplnitelných míst.

2\. **Rozlišení způsobu doplnění hodnot.** Systém by měl umět rozlišit, jakým způsobem lze jednotlivé části dokumentu doplnit. Některé hodnoty se získávají deterministicky z registrů nebo jiných strukturovaných zdrojů, jiné se generují z veřejně dostupného textového kontextu, další je vhodné získat od uživatele řízeným rozhovorem a některé vznikají až později odvozením z již potvrzených hodnot. Toto rozlišení je důležité pro správné pořadí kroků, pro výběr zdrojů a pro rozhodnutí, zda může systém hodnotu navrhnout automaticky, nebo se musí zeptat uživatele.

3\. **Správa kontextu pro generování návrhů.** Asistent by měl mít mechanismus pro získávání, filtrování a ukládání relevantního kontextu. Kontext může pocházet z veřejných registrů, metodických dokumentů, legislativy, webových stránek úřadu nebo z dříve potvrzených hodnot v dokumentu. Nestačí pouze stáhnout velké množství textu. Systém by měl vybírat takové části, které jsou relevantní pro konkrétní pole, tabulku nebo sekci. Kvalita tohoto kroku přímo ovlivňuje kvalitu AI návrhů.

4\. **Dohledatelnost zdrojů.** Každý návrh vytvořený asistentem by měl být spojen se zdrojem, ze kterého vychází. U deterministicky doplněných hodnot to může být konkrétní registr nebo datový záznam. U AI návrhů to mohou být použité textové úryvky, webové stránky nebo metodické dokumenty. U hodnot získaných rozhovorem to může být odpověď uživatele. Pokud systém nemá dostatečný zdroj, neměl by hodnotu prezentovat jako hotovou, ale jako návrh k ověření nebo jako nevyplněné místo.

5\. **Možnost navazujícího zpracování po inicializaci.** Inicializační fáze je v navrženém workflow jednorázový krok, který připraví pracovní stav dokumentu a vytvoří první návrhy. Po této fázi by měl asistent umožnit další práci s dokumentem, například úpravu návrhů, potvrzování hodnot, ruční doplnění a případné opakované generování pro vybrané části. Ideální řešení by mohlo podporovat širší iterativní práci nad celým dokumentem, ale pro základní specifikaci je důležité hlavně to, aby další kroky mohly navazovat na aktuální stav dokumentu a využívat již potvrzené hodnoty jako kontext.

6\. **Oddělení obecné logiky asistenta od konkrétní implementace šablony.** Specifikace nemá pevně vázat asistenta na jeden technický způsob označení šablony. Náš prototyp používá DOCX, SDT prvky, aliasy a interní model, ale obecná logika asistenta by měla být přenositelná. Důležité je, aby systém uměl identifikovat vyplnitelné části, přiřadit jim typ doplnění, dohledat zdroje, vytvořit návrhy a zapsat potvrzené hodnoty zpět do pracovní verze dokumentu.

7\. **Možnost rozšiřování zdrojů a pravidel.** Asistent by měl být navržen tak, aby bylo možné postupně přidávat nové datové zdroje, nové typy polí, nové otázkové scénáře a nová pravidla pro generování nebo odvozování hodnot. To je důležité hlavně proto, že veřejné zdroje, metodiky i požadavky na dokument se mohou měnit.

8\. **Export a zápis výsledků do dokumentu.** Na konci práce by mělo být možné zapsat potvrzené hodnoty zpět do pracovní šablony nebo vytvořit export dokumentu. Export by měl zachovat strukturu dokumentu a promítnout do něj hodnoty, které uživatel potvrdil nebo ručně doplnil. U návrhů, které nejsou potvrzené, je potřeba rozhodnout, zda se do exportu vůbec nezapíšou, nebo se zapíšou pouze jako označené návrhy.

# **Shrnutí**

Specifikace popisuje asistenta jako systém, který má pomáhat s doplňováním šablony IK OVS. Důraz je kladen na rozpoznání částí dokumentu, výběr vhodného způsobu doplnění hodnot, získávání kvalitního kontextu, dohledatelnost zdrojů a možnost uživatelské kontroly nad návrhy. Popsané workflow vychází z našeho prototypu a představuje jeden možný způsob řešení. Jednotlivé principy by ale měly být použitelné i pro jiné implementace asistenta.