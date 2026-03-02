using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Assistant.AI.Abstractions;
using Assistant.AI.Models;
using Assistant.Core.Model;
using Assistant.Core.Merge;
using Assistant.Dependencies.Context;
using Assistant.Dependencies.Context.Web.Storage;

namespace Assistant.Pipeline.Initiation
{
    /// <summary>
    /// Orchestrator that retrieves external web context and injects it
    /// into AI generation requests. All context shaping is delegated
    /// to WebContextPack.
    /// </summary>
    public sealed class ContextAwareInitiationOrchestrator
    {
        private readonly IProposalGenerator proposalGenerator;
        private readonly CompositeContextProvider contextProvider;

        public ContextAwareInitiationOrchestrator(
            IProposalGenerator proposalGenerator,
            CompositeContextProvider contextProvider)
        {
            this.proposalGenerator = proposalGenerator
                ?? throw new ArgumentNullException(nameof(proposalGenerator));

            this.contextProvider = contextProvider
                ?? throw new ArgumentNullException(nameof(contextProvider));
        }

        public async Task<MergeDecision> GenerateAndApplyAsync(
            InternalModelRuntime internalModelRuntime,
            FieldAlias alias,
            string officeIdentifier,
            string? documentContextText,
            CancellationToken cancellationToken)
        {
            if (internalModelRuntime is null)
            {
                throw new ArgumentNullException(nameof(internalModelRuntime));
            }

            InternalModel model = internalModelRuntime.Model;

            FieldNode fieldNode = model.GetField(alias);

            // If no owning section -> no web context
            if (!model.TryGetOwningSectionAlias(alias, out SectionAlias owningSectionAlias))
            {
                return await GenerateWithoutWebContext(
                        internalModelRuntime,
                        fieldNode,
                        alias,
                        documentContextText,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            WebContextPack contextPack = await contextProvider
                .GetOrBuildContextPackAsync(model, officeIdentifier, cancellationToken)
                .ConfigureAwait(false);

            (string webText, IReadOnlyList<string> references) =
                contextPack.GetContextText(owningSectionAlias.Value);

            string combinedContext = CombineContext(documentContextText, webText);

            GenerationRequest request = GenerationRequest.Create(
                mode: GenerationMode.InitialPrefill,
                alias: alias,
                descriptor: fieldNode.Descriptor,
                currentValue: fieldNode.CurrentValue,
                existingProposals: fieldNode.ProposalHistory,
                documentContextText: combinedContext,
                contextReferences: references);

            Proposal proposal = await proposalGenerator
                .GenerateAsync(request, cancellationToken)
                .ConfigureAwait(false);

            return internalModelRuntime.Proposals.ApplyProposal(proposal);
        }

        public async Task GenerateSectionAsync(
            InternalModelRuntime internalModelRuntime,
            IReadOnlyList<FieldAlias> sectionFieldAliases,
            string officeIdentifier,
            string? sectionContextText,
            CancellationToken cancellationToken)
        {
            if (internalModelRuntime is null)
            {
                throw new ArgumentNullException(nameof(internalModelRuntime));
            }

            if (sectionFieldAliases is null)
            {
                throw new ArgumentNullException(nameof(sectionFieldAliases));
            }

            if (sectionFieldAliases.Count == 0)
            {
                return;
            }

            InternalModel model = internalModelRuntime.Model;

            FieldAlias firstAlias = sectionFieldAliases[0];

            if (!model.TryGetOwningSectionAlias(firstAlias, out SectionAlias sectionAlias))
            {
                foreach (FieldAlias alias in sectionFieldAliases)
                {
                    FieldNode fieldNode = model.GetField(alias);

                    await GenerateWithoutWebContext(
                            internalModelRuntime,
                            fieldNode,
                            alias,
                            sectionContextText,
                            cancellationToken)
                        .ConfigureAwait(false);
                }

                return;
            }

            WebContextPack contextPack = await contextProvider
                .GetOrBuildContextPackAsync(model, officeIdentifier, cancellationToken)
                .ConfigureAwait(false);

            (string webText, IReadOnlyList<string> references) =
                contextPack.GetContextText(sectionAlias.Value);

            string combinedContext = CombineContext(sectionContextText, webText);

            foreach (FieldAlias alias in sectionFieldAliases)
            {
                FieldNode fieldNode = model.GetField(alias);

                GenerationRequest request = GenerationRequest.Create(
                    mode: GenerationMode.InitialPrefill,
                    alias: alias,
                    descriptor: fieldNode.Descriptor,
                    currentValue: fieldNode.CurrentValue,
                    existingProposals: fieldNode.ProposalHistory,
                    documentContextText: combinedContext,
                    contextReferences: references);

                Proposal proposal = await proposalGenerator
                    .GenerateAsync(request, cancellationToken)
                    .ConfigureAwait(false);

                internalModelRuntime.Proposals.ApplyProposal(proposal);
            }
        }

        private async Task<MergeDecision> GenerateWithoutWebContext(
            InternalModelRuntime internalModelRuntime,
            FieldNode fieldNode,
            FieldAlias alias,
            string? contextText,
            CancellationToken cancellationToken)
        {
            GenerationRequest request = GenerationRequest.Create(
                mode: GenerationMode.InitialPrefill,
                alias: alias,
                descriptor: fieldNode.Descriptor,
                currentValue: fieldNode.CurrentValue,
                existingProposals: fieldNode.ProposalHistory,
                documentContextText: contextText,
                contextReferences: Array.Empty<string>());

            Proposal proposal = await proposalGenerator
                .GenerateAsync(request, cancellationToken)
                .ConfigureAwait(false);

            return internalModelRuntime.Proposals.ApplyProposal(proposal);
        }

        private static string CombineContext(string? baseContext, string webContext)
        {
            if (!string.IsNullOrWhiteSpace(baseContext) &&
                !string.IsNullOrWhiteSpace(webContext))
            {
                return baseContext.Trim() + "\n\n" + webContext;
            }

            if (!string.IsNullOrWhiteSpace(baseContext))
            {
                return baseContext;
            }

            return webContext ?? string.Empty;
        }
    }
}