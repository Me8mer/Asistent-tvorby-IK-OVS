namespace Assistant.Dependencies.TemplateDefinitions;

internal sealed class DefinitionField
{
    public string Alias { get; set; } = "";

    public string? Parent { get; set; }

    public string? LabelCs { get; set; }

    public DefinitionFill? Fill { get; set; }

    public bool? Required { get; set; }

    public string? ValueType { get; set; }

    public List<string>? QueryHints { get; set; }

    public string? Instructions { get; set; }
}