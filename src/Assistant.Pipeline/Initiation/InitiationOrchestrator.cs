using System;
using System.Threading;
using System.Threading.Tasks;
using Assistant.AI.Abstractions;
using Assistant.AI.Models;
using Assistant.Core.Merge;
using Assistant.Core.Model;

namespace Assistant.Pipeline.Initiation
{
    public sealed class InitiationOrchestrator
    {
        private readonly IProposalGenerator proposalGenerator;

        public InitiationOrchestrator(IProposalGenerator proposalGenerator)
        {
            this.proposalGenerator = proposalGenerator ?? throw new ArgumentNullException(nameof(proposalGenerator));
        }

        public async Task<MergeDecision> GenerateAndApplyAsync(
            InternalModel internalModel,
            FieldAlias alias,
            string? documentContextText,
            CancellationToken cancellationToken)
        {
            if (internalModel is null)
            {
                throw new ArgumentNullException(nameof(internalModel));
            }

            FieldNode fieldNode = internalModel.GetField(alias);

            GenerationRequest request = GenerationRequest.Create(
                mode: GenerationMode.InitialPrefill,
                alias: alias,
                descriptor: fieldNode.Descriptor,
                currentValue: fieldNode.CurrentValue,
                existingProposals: fieldNode.ProposalHistory,
                documentContextText: documentContextText,
                contextReferences: Array.Empty<string>());

            Proposal proposal = await proposalGenerator.GenerateAsync(request, cancellationToken);

            MergeDecision decision = internalModel.ApplyProposal(proposal);
            return decision;
        }
    }
}
