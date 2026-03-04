namespace Assistant.Core.Model
{
    public interface ITemplateDefinitionProvider
    {
        bool TryGetDefinition(
            string templateVersion,
            out TemplateDefinition definition,
            out string? error);
    }
}