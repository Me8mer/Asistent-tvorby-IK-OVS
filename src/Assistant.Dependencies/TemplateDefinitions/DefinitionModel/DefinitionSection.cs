namespace Assistant.Dependencies.TemplateDefinitions;

internal sealed class DefinitionSection
{
    public string Alias { get; set; } = "";
    public string? Parent { get; set; }

    public string? LabelCs { get; set; }

    public List<string>? QueryHints { get; set; }

    public int? OrderIndex { get; set; }
}