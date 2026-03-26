using System;
using System.Threading;
using System.Threading.Tasks;
using Assistant.AI.Abstractions;
using Assistant.AI.Models;
using Assistant.Core.Model;
using Assistant.Core.Model.AIProposals;

namespace Assistant.AI.Generators
{
    public sealed class DefaultProposalGenerator : IProposalGenerator
    {
        private readonly IRoutingPolicy routingPolicy;
        private readonly IPromptBuilder promptBuilder;
        private readonly ILlmProvider llmProvider;
        private readonly IProposalParser proposalParser;

        public DefaultProposalGenerator(
            IRoutingPolicy routingPolicy,
            IPromptBuilder promptBuilder,
            ILlmProvider llmProvider,
            IProposalParser proposalParser)
        {
            this.routingPolicy = routingPolicy ?? throw new ArgumentNullException(nameof(routingPolicy));
            this.promptBuilder = promptBuilder ?? throw new ArgumentNullException(nameof(promptBuilder));
            this.llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
            this.proposalParser = proposalParser ?? throw new ArgumentNullException(nameof(proposalParser));
        }

        public async Task<Proposal> GenerateAsync(GenerationRequest request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            cancellationToken.ThrowIfCancellationRequested();

            ModelRouting routing = routingPolicy.ResolveRouting(request);
            LlmPrompt prompt = promptBuilder.BuildPrompt(request);

            LlmRawResponse response = await llmProvider.GenerateAsync(routing, prompt, cancellationToken);

            Proposal proposal = proposalParser.ParseProposal(request, routing, response);
            return proposal;
        }
    }
}
