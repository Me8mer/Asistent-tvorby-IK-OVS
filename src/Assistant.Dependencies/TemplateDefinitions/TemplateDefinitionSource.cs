
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Assistant.Dependencies.TemplateDefinitions;

internal sealed class TemplateDefinitionSource
{
    public List<DefinitionTemplate> Load(string yamlPath)
    {
        if (!File.Exists(yamlPath))
            throw new FileNotFoundException($"Template definition YAML not found: {yamlPath}");

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        var yaml = File.ReadAllText(yamlPath);

        var root = deserializer.Deserialize<DefinitionFile>(yaml);

        return root.Templates ?? new();
    }
}