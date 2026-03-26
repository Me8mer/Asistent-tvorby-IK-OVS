using System;
using Assistant.Core.Merge;

/// <summary>
/// Applies proposals to fields using merge rules.
/// 
/// Flow:
/// 1. Record proposal
/// 2. Evaluate via MergeRules
/// 3. Update field if allowed
/// 
/// Core integration point between AI and model.
/// </summary>
namespace Assistant.Core.Model.AIProposals
{
    public sealed class ProposalApplier
    {
        private readonly FieldStore fieldStore;

        public ProposalApplier(FieldStore fieldStore)
        {
            this.fieldStore = fieldStore ?? throw new ArgumentNullException(nameof(fieldStore));
        }

        public MergeDecision ApplyProposal(Proposal proposal)
        {
            if (!fieldStore.TryGetField(proposal.Alias, out FieldNode fieldNode))
            {
                return MergeDecision.Deny(
                    $"Field with alias '{proposal.Alias}' does not exist.",
                    currentStatus: FieldStatus.Empty);
            }

            fieldNode.AddProposal(proposal);

            FieldValue currentValue = fieldNode.CurrentValue;
            MergeDecision decision = MergeRules.DecideApply(currentValue, proposal);

            if (!decision.IsAllowed)
            {
                return decision;
            }

            FieldValue updatedValue = currentValue.With(
                status: decision.ResultingStatus,
                value: proposal.ProposedValue,
                source: proposal.Source,
                confidence: proposal.Confidence);

            fieldNode.SetCurrentValue(updatedValue);

            return decision;
        }
    }
}