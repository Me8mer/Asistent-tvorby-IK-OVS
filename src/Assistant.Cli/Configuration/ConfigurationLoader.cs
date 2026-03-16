using Microsoft.Extensions.Configuration;

namespace Assistant.Cli.Configuration;

public static class ConfigurationLoader
{
    public static ApiKeysOptions Load()
    {
        IConfiguration configuration =
            new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddIniFile("config.local.ini", optional: true)
                .AddEnvironmentVariables()
                .Build();

        return new ApiKeysOptions
        {
            OpenAiApiKey = configuration["OpenAI:ApiKey"],
            ApifyApiToken = configuration["Apify:ApiToken"]
        };
    }

    public static void Validate(ApiKeysOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.OpenAiApiKey))
        {
            throw new InvalidOperationException(
                "Missing OpenAI API key. Provide it in config.local.ini under [OpenAI] ApiKey.");
        }

        if (string.IsNullOrWhiteSpace(options.ApifyApiToken))
        {
            throw new InvalidOperationException(
                "Missing Apify API token. Provide it in config.local.ini under [Apify] ApiToken.");
        }
    }
}