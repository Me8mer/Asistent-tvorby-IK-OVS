using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

/// <summary>
/// Holds mutable runtime state for a field.
/// 
/// Responsibilities:
/// - current value
/// - proposal history
/// 
/// Separation exists because:
/// FieldValue is immutable, FieldState manages lifecycle.
/// </summary>
namespace Assistant.Core.Model.Fields
{
    public sealed class FieldState
    {
        private readonly List<Proposal> proposalHistory;

        public FieldValue CurrentValue { get; private set; }
        public IReadOnlyList<Proposal> ProposalHistory { get; }

        public FieldState(FieldAlias alias)
            : this(FieldValue.CreateEmpty(alias))
        {
        }

        public FieldState(FieldValue initialValue)
        {
            if (initialValue is null)
            {
                throw new ArgumentNullException(nameof(initialValue));
            }

            CurrentValue = initialValue;

            proposalHistory = new List<Proposal>();
            ProposalHistory = new ReadOnlyCollection<Proposal>(proposalHistory);
        }

        public void AddProposal(Proposal proposal, FieldAlias expectedAlias)
        {
            if (proposal is null)
            {
                throw new ArgumentNullException(nameof(proposal));
            }

            if (!proposal.Alias.Equals(expectedAlias))
            {
                throw new InvalidOperationException("Proposal alias does not match field alias.");
            }

            proposalHistory.Add(proposal);
        }

        public void SetCurrentValue(FieldValue newValue, FieldAlias expectedAlias)
        {
            if (newValue is null)
            {
                throw new ArgumentNullException(nameof(newValue));
            }

            if (!newValue.Alias.Equals(expectedAlias))
            {
                throw new InvalidOperationException("New value alias does not match field alias.");
            }

            CurrentValue = newValue;
        }
    }
}