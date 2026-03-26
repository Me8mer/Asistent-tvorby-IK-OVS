/// <summary>
/// Abstraction for loading template definitions
/// Keeps model builder independent of storage.
/// </summary>  
namespace Assistant.Core.Model.Templates
{
    public interface ITemplateDefinitionProvider
    {
        bool TryGetDefinition(
            string templateVersion,
            out TemplateDefinition definition,
            out string? error);
    }
}