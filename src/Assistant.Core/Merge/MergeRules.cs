using System;
using Assistant.Core.Model;

namespace Assistant.Core.Merge
{
    public static class MergeRules
    {
        public static MergeDecision DecideApply(FieldValue currentValue, Proposal proposal)
        {
            if (!proposal.Alias.Equals(currentValue.Alias))
            {
                return MergeDecision.Deny("Proposal alias does not match current value alias.", currentValue.Status);
            }

            if (!IsProposalStatusValid(proposal.ProposedStatus))
            {
                return MergeDecision.Deny($"Proposal status '{proposal.ProposedStatus}' is not a valid proposal status.", currentValue.Status);
            }

            FieldStatus currentStatus = currentValue.Status;
            FieldStatus proposedStatus = proposal.ProposedStatus;

            if (currentStatus == FieldStatus.Confirmed)
            {
                return MergeDecision.Deny("Field is confirmed and cannot be overwritten automatically.", currentStatus);
            }

            if (currentStatus == FieldStatus.UserInput)
            {
                return MergeDecision.Deny("Field contains user input and cannot be overwritten automatically.", currentStatus);
            }

            if (currentStatus == FieldStatus.Deterministic)
            {
                return MergeDecision.Deny("Field is deterministic and cannot be overwritten automatically.", currentStatus);
            }

            if (currentStatus == FieldStatus.AiInteractive)
            {
                return MergeDecision.Deny("Field has an interactive value and cannot be overwritten automatically.", currentStatus);
            }

            if (currentStatus == FieldStatus.Rejected)
            {
                return MergeDecision.Deny("Field is rejected and cannot be overwritten automatically.", currentStatus);
            }

            if (currentStatus == FieldStatus.Empty || currentStatus == FieldStatus.NeedsValidation)
            {
                return MergeDecision.Allow(resultingStatus: proposedStatus);
            }

            if (currentStatus == FieldStatus.AiProposal)
            {
                if (proposedStatus == FieldStatus.AiInteractive)
                {
                    return MergeDecision.Allow(resultingStatus: FieldStatus.AiInteractive);
                }

                return MergeDecision.Deny("AI proposal cannot be overwritten automatically, except by interactive input.", currentStatus);
            }

            return MergeDecision.Deny($"Unhandled current status '{currentStatus}'.", currentStatus);
        }

        private static bool IsProposalStatusValid(FieldStatus proposedStatus)
        {
            return proposedStatus == FieldStatus.Deterministic
                   || proposedStatus == FieldStatus.NeedsValidation
                   || proposedStatus == FieldStatus.AiProposal
                   || proposedStatus == FieldStatus.AiInteractive;
        }
    }
}
