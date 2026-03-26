using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Assistant.AI.Abstractions;
using Assistant.AI.Models;
using Assistant.Core.Model;
using Assistant.Core.Model.AIProposals;
using Assistant.Core.Model.Aliases;
using Assistant.Core.Model.Fields;
using Assistant.Core.Model.InternalModel;
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

            // If running in openai-web mode skip external web context entirely. This
            // allows the underlying model to perform its own web search via
            // OpenAI's Responses API. We detect the mode by checking the
            // ASSISTANT_CONTEXT_MODE environment variable. When set to
            // "openai-web" we do not call the CompositeContextProvider and
            // instead call the minimal GenerateWithoutWebContext path.
            bool useOpenAiWebSearch = string.Equals(
                Environment.GetEnvironmentVariable("ASSISTANT_CONTEXT_MODE"),
                "openai-web",
                StringComparison.OrdinalIgnoreCase);

            InternalModel model = internalModelRuntime.Model;

            FieldNode fieldNode = model.GetField(alias);

            // If configured to use OpenAI web search, avoid retrieving any external
            // web context. Use the provided documentContextText directly.
            if (useOpenAiWebSearch)
            {
                string augmentedContext = BuildOfficeContext(officeIdentifier, documentContextText);

                return await GenerateWithoutWebContext(
                        internalModelRuntime,
                        fieldNode,
                        alias,
                        documentContextText,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

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

            // Determine if openai-web mode is enabled. When true, skip web context
            // retrieval entirely and use the provided sectionContextText for each
            // field.
            bool useOpenAiWebSearch = string.Equals(
                Environment.GetEnvironmentVariable("ASSISTANT_CONTEXT_MODE"),
                "openai-web",
                StringComparison.OrdinalIgnoreCase);

            InternalModel model = internalModelRuntime.Model;

            FieldAlias firstAlias = sectionFieldAliases[0];

            if (useOpenAiWebSearch)
            {
                // In openai-web mode simply call GenerateWithoutWebContext for each
                // field in the section using the provided sectionContextText. This
                // avoids any external crawling or retrieval.
                foreach (FieldAlias alias in sectionFieldAliases)
                {
                    string augmentedContext = BuildOfficeContext(officeIdentifier, sectionContextText);
                    
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

            string combinedContext = webText;
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
        
        private static string BuildOfficeContext(string officeIdentifier, string? existingContext)
        {
            // HARD CODED FOR NOW (your example)
            const string officeName = "Ministerstvo pro místní rozvoj České republiky";
            const string officeIcoHint = "IČO can be found on official website";

            string forcedContext =
                $"You are filling data for the following public office:\n" +
                $"Name: {officeName}\n" +
                $"Website: {officeIdentifier}\n\n" +
                $"You MUST use web search to find missing official data such as IČO, address, contacts.\n" +
                $"Do NOT leave fields empty if the information can be found online.\n";

            if (!string.IsNullOrWhiteSpace(existingContext))
            {
                return forcedContext + "\n" + existingContext;
            }

            return forcedContext;
        }
    }
}