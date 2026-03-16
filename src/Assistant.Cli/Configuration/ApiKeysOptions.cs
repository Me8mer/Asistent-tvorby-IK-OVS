namespace Assistant.Cli.Configuration
{
    public sealed class ApiKeysOptions
    {
        public string? OpenAiApiKey { get; init; }

        public string? ApifyApiToken { get; init; }
    }
}