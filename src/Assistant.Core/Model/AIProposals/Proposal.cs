using System;
using System.Collections.Generic;

/// <summary>
/// Represents a suggestion for a field value.
/// 
/// Contains:
/// - proposed value
/// - confidence
/// - explanation
/// - context references
/// 
/// Immutable.
/// </summary>
namespace Assistant.Core.Model.AIProposals
{
    public sealed class Proposal
    {
        public Guid ProposalId { get; }
        public FieldAlias Alias { get; }
        public FieldStatus ProposedStatus { get; }
        public object? ProposedValue { get; }
        public string Source { get; }
        public double? Confidence { get; }
        public string? Explanation { get; }
        public IReadOnlyList<string> ContextReferences { get; }
        public DateTimeOffset Timestamp { get; }

        public Proposal(
            Guid proposalId,
            FieldAlias alias,
            FieldStatus proposedStatus,
            object? proposedValue,
            string source,
            double? confidence,
            string? explanation,
            IReadOnlyList<string> contextReferences,
            DateTimeOffset timestamp)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new ArgumentException("Source must not be null, empty, or whitespace.", nameof(source));
            }

            ProposalId = proposalId;
            Alias = alias;
            ProposedStatus = proposedStatus;
            ProposedValue = proposedValue;
            Source = source;
            Confidence = confidence;
            Explanation = explanation;
            ContextReferences = contextReferences ?? Array.Empty<string>();
            Timestamp = timestamp;
        }

        public static Proposal Create(
            FieldAlias alias,
            FieldStatus proposedStatus,
            object? proposedValue,
            string source,
            double? confidence = null,
            string? explanation = null,
            IReadOnlyList<string>? contextReferences = null)
        {
            return new Proposal(
                Guid.NewGuid(),
                alias,
                proposedStatus,
                proposedValue,
                source,
                confidence,
                explanation,
                contextReferences ?? Array.Empty<string>(),
                DateTimeOffset.UtcNow);
        }
    }
}
