using Assistant.AI.Models;

namespace Assistant.AI.Abstractions
{
    public interface IPromptBuilder
    {
        LlmPrompt BuildPrompt(GenerationRequest request);
    }
}
