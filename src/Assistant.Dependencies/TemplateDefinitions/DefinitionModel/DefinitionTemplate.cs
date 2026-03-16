namespace Assistant.Dependencies.TemplateDefinitions;

internal sealed class DefinitionTemplate
{
    public string Key { get; set; } = "";
    public string? Label { get; set; }

    public List<DefinitionSection>? Sections { get; set; }
    public List<DefinitionField>? Fields { get; set; }
}