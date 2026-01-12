using System.Threading;
using System.Threading.Tasks;
using Assistant.AI.Models;

namespace Assistant.AI.Abstractions
{
    public interface ILlmProvider
    {
        Task<LlmRawResponse> GenerateAsync(
            ModelRouting routing,
            LlmPrompt prompt,
            CancellationToken cancellationToken);
    }
}
