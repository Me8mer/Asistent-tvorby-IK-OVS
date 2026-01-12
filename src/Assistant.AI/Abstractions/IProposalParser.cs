using Assistant.AI.Models;
using Assistant.Core.Model;

namespace Assistant.AI.Abstractions
{
    public interface IProposalParser
    {
        Proposal ParseProposal(GenerationRequest request, ModelRouting routing, LlmRawResponse response);
    }
}
