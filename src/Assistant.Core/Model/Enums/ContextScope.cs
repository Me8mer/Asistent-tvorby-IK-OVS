/// <summary>
/// Defines how much surrounding document content should be extracted
/// as context for AI.
/// 
/// This is NOT about web context – only document/template context.
/// </summary>
namespace Assistant.Core.Model.Enums
{
    public enum ContextScope
    {
        None = 0,
        SdtContentOnly = 1,
        SurroundingParagraph = 2,
        SectionByAliasPrefix = 3,
        Custom = 4
    }
}
