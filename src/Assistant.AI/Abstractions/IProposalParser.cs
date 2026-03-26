using Assistant.AI.Models;
using Assistant.Core.Model;
using Assistant.Core.Model.AIProposals;

namespace Assistant.AI.Abstractions
{
    public interface IProposalParser
    {
        Proposal ParseProposal(GenerationRequest request, ModelRouting routing, LlmRawResponse response);
    }
}
