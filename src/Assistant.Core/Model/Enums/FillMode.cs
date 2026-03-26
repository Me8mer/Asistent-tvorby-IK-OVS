/// <summary>
/// Defines how a field can be populated.
/// These are flags → a field can support multiple modes.
/// 
/// - Deterministic: filled by rules/code
/// - Ai: filled automatically by AI
/// - Chatbot: filled via interactive conversation
/// 
/// Used mainly by descriptors to control orchestration.
/// </summary>
namespace Assistant.Core.Model.Enums
{
    [System.Flags]
    public enum FillMode
    {
        None = 0,
        Deterministic = 1,
        Ai = 2,
        Chatbot = 4
    }
}
