using Assistant.AI.Models;

namespace Assistant.AI.Abstractions
{
    public interface IRoutingPolicy
    {
        ModelRouting ResolveRouting(GenerationRequest request);
    }
}
