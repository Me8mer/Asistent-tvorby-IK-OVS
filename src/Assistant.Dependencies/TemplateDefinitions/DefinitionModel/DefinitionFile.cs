namespace Assistant.Dependencies.TemplateDefinitions;

internal sealed class DefinitionFile
{
    public int Version { get; set; }
    public List<DefinitionTemplate>? Templates { get; set; }
}