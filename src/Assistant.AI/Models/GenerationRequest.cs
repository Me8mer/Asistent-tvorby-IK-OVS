using System;
using System.Collections.Generic;
using Assistant.AI.Models;
using Assistant.Core.Model;
using Assistant.Core.Model.AIProposals;
using Assistant.Core.Model.Aliases;
using Assistant.Core.Model.Fields;

namespace Assistant.AI.Models
{
    public sealed class GenerationRequest
    {
        public Guid RequestId { get; }
        public GenerationMode Mode { get; }
        public FieldAlias Alias { get; }

        public FieldDescriptor? Descriptor { get; }
        public FieldValue CurrentValue { get; }
        public IReadOnlyList<Proposal> ExistingProposals { get; }

        public string? DocumentContextText { get; }
        public IReadOnlyList<string> ContextReferences { get; }

        public GenerationRequest(
            Guid requestId,
            GenerationMode mode,
            FieldAlias alias,
            FieldDescriptor? descriptor,
            FieldValue currentValue,
            IReadOnlyList<Proposal>? existingProposals,
            string? documentContextText,
            IReadOnlyList<string>? contextReferences)
        {
            RequestId = requestId;
            Mode = mode;
            Alias = alias;
            Descriptor = descriptor;
            CurrentValue = currentValue;
            ExistingProposals = existingProposals ?? Array.Empty<Proposal>();
            DocumentContextText = documentContextText;
            ContextReferences = contextReferences ?? Array.Empty<string>();
        }

        public static GenerationRequest Create(
            GenerationMode mode,
            FieldAlias alias,
            FieldDescriptor? descriptor,
            FieldValue currentValue,
            IReadOnlyList<Proposal>? existingProposals = null,
            string? documentContextText = null,
            IReadOnlyList<string>? contextReferences = null)
        {
            return new GenerationRequest(
                Guid.NewGuid(),
                mode,
                alias,
                descriptor,
                currentValue,
                existingProposals,
                documentContextText,
                contextReferences);
        }
    }
}
