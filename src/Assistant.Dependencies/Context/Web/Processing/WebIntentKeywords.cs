using System.Collections.Generic;

namespace Assistant.Dependencies.Context.Web.Processing
{
    public static class WebIntentKeywords
    {
        public static IReadOnlyDictionary<string, string[]> Create()
        {
            return new Dictionary<string, string[]>
            {
                ["Contacts"] = new[]
                {
                    "kontakt", "kontakty", "telefon", "tel.", "e-mail", "email", "datov", "datová schránka",
                    "podatelna", "adresa", "úřední hodiny", "uredni hodiny",
                    "kontaktovať", "kontaktujte", "telefón", "podateľňa", "úradné hodiny"
                },

                ["SubmissionChannels"] = new[]
                {
                    "podání", "podani", "podat", "podatelna", "datová schránka", "datov", "ds",
                    "elektronicky", "e-mail", "email", "zaručen", "zaručený podpis",
                    "podanie", "podateľňa", "elektronicky", "zaručený", "zaručená"
                },

                ["Fees"] = new[]
                {
                    "poplatek", "poplatky", "správní poplatek", "spravni poplatek", "cena", "sazebník", "sazebnik",
                    "úhrada", "platba", "bankovní účet", "bankovni ucet", "pokladna", "kolky",
                    "poplatok", "poplatky", "sadzobník", "sadzobnik", "úhrada", "platba", "pokladňa"
                },

                ["RequiredForms"] = new[]
                {
                    "formulář", "formular", "žádost", "zadost", "tiskopis", "ke stažení", "ke stazeni",
                    "příloha", "priloha", "vzory", "vzor",
                    "formulár", "žiadosť", "tlačivo", "na stiahnutie", "príloha", "vzor"
                },

                ["OfficeScope"] = new[]
                {
                    "působnost", "pusobnost", "agenda", "kompetence", "oddělení", "odbor", "úsek",
                    "služby", "sluzby", "co vyřizujeme", "co vyrizujeme", "životní situace", "zivotni situace",
                    "pôsobnosť", "agenda", "kompetencie", "oddelenie", "odbor", "úsek", "služby", "životné situácie"
                }
            };
        }
    }
}
