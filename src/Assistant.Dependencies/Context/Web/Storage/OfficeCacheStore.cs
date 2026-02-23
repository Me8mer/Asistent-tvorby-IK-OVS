using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using System.Text.Encodings.Web;


namespace Assistant.Dependencies.Context.Web.Storage
{
    
    public sealed class OfficeCacheStore
    {
        private readonly string cacheRootPath;
        private readonly JsonSerializerOptions jsonOptions =
            new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

        public OfficeCacheStore(string cacheRootPath)
        {
            this.cacheRootPath = cacheRootPath;
        }

        private string GetOfficeDirectory(string officeKey)
        {
            return Path.Combine(cacheRootPath, officeKey);
        }

        private string GetCorpusPath(string officeKey)
        {
            return Path.Combine(GetOfficeDirectory(officeKey), "corpus.json");
        }

        private string GetContextPackPath(string officeKey)
        {
            return Path.Combine(GetOfficeDirectory(officeKey), "context-pack.json");
        }

        private string GetChunkCorpusPath(string officeKey)
        {
            return Path.Combine(GetOfficeDirectory(officeKey), "chunk-corpus.json");
        }


        public async Task SaveCorpusAsync(WebCorpus corpus)
        {
            string officeDir = GetOfficeDirectory(corpus.OfficeKey);
            Directory.CreateDirectory(officeDir);

            string path = GetCorpusPath(corpus.OfficeKey);

            string json = JsonSerializer.Serialize(corpus, jsonOptions);
            await File.WriteAllTextAsync(path, json);
        }

        public async Task<WebCorpus?> LoadCorpusAsync(string officeKey)
        {
            string path = GetCorpusPath(officeKey);

            if (!File.Exists(path))
                return null;

            string json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<WebCorpus>(json, jsonOptions);
        }

        public async Task SaveContextPackAsync(WebContextPack pack)
        {
            string officeDir = GetOfficeDirectory(pack.OfficeKey);
            Directory.CreateDirectory(officeDir);

            string path = GetContextPackPath(pack.OfficeKey);

            string json = JsonSerializer.Serialize(pack, jsonOptions);
            await File.WriteAllTextAsync(path, json);
        }

        public async Task<WebContextPack?> LoadContextPackAsync(string officeKey)
        {
            string path = GetContextPackPath(officeKey);

            if (!File.Exists(path))
                return null;

            string json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<WebContextPack>(json, jsonOptions);
        }

        public async Task SaveChunkCorpusAsync(WebChunkCorpus corpus)
        {
            string officeDir = GetOfficeDirectory(corpus.OfficeKey);
            Directory.CreateDirectory(officeDir);

            string path = GetChunkCorpusPath(corpus.OfficeKey);

            string json = JsonSerializer.Serialize(corpus, jsonOptions);
            await File.WriteAllTextAsync(path, json);
        }

        public async Task<WebChunkCorpus?> LoadChunkCorpusAsync(string officeKey)
        {
            string path = GetChunkCorpusPath(officeKey);

            if (!File.Exists(path))
                return null;

            string json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<WebChunkCorpus>(json, jsonOptions);
        }

        private string GetSectionPackPath(string officeKey, string sectionKey)
        {
            string safeSectionKey = ToSafeFileToken(sectionKey);
            return Path.Combine(GetOfficeDirectory(officeKey), $"section-pack-{safeSectionKey}.json");
        }

        private static string ToSafeFileToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "empty";

            var builder = new StringBuilder(value.Length);

            foreach (char character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                    continue;
                }

                if (builder.Length == 0 || builder[builder.Length - 1] == '_')
                    continue;

                builder.Append('_');
            }

            string token = builder.ToString().Trim('_');
            return string.IsNullOrWhiteSpace(token) ? "empty" : token;
        }

        public async Task SaveSectionPackAsync(WebSectionPack sectionPack)
        {
            string officeDir = GetOfficeDirectory(sectionPack.OfficeKey);
            Directory.CreateDirectory(officeDir);

            string path = GetSectionPackPath(sectionPack.OfficeKey, sectionPack.SectionKey);

            string json = JsonSerializer.Serialize(sectionPack, jsonOptions);
            await File.WriteAllTextAsync(path, json);
        }

        public async Task<WebSectionPack?> LoadSectionPackAsync(string officeKey, string sectionKey)
        {
            string path = GetSectionPackPath(officeKey, sectionKey);

            if (!File.Exists(path))
                return null;

            string json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<WebSectionPack>(json, jsonOptions);
        }
    }
}
