using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Assistant.Core.Model
{
    public sealed class FieldNode
    {
        private readonly List<Proposal> proposalHistory;

        public FieldAlias Alias { get; }
        public FieldValue CurrentValue { get; private set; }
        public IReadOnlyList<Proposal> ProposalHistory { get; }

        public FieldBinding? Binding { get; private set; }
        public FieldDescriptor? Descriptor { get; private set; }
        public DependencySnapshot? Dependency { get; private set; }

        public FieldNode(FieldAlias alias, FieldBinding? binding = null, FieldDescriptor? descriptor = null)
        {
            if (binding is not null && !binding.Alias.Equals(alias))
            {
                throw new InvalidOperationException("Binding alias does not match field alias.");
            }

            if (descriptor is not null && !descriptor.Alias.Equals(alias))
            {
                throw new InvalidOperationException("Descriptor alias does not match field alias.");
            }

            Alias = alias;
            Binding = binding;
            Descriptor = descriptor;

            CurrentValue = FieldValue.CreateEmpty(alias);
            proposalHistory = new List<Proposal>();
            ProposalHistory = new ReadOnlyCollection<Proposal>(proposalHistory);
        }

        public FieldNode(FieldValue initialValue, FieldBinding? binding = null, FieldDescriptor? descriptor = null)
        {
            if (binding is not null && !binding.Alias.Equals(initialValue.Alias))
            {
                throw new InvalidOperationException("Binding alias does not match field alias.");
            }

            if (descriptor is not null && !descriptor.Alias.Equals(initialValue.Alias))
            {
                throw new InvalidOperationException("Descriptor alias does not match field alias.");
            }

            Alias = initialValue.Alias;
            Binding = binding;
            Descriptor = descriptor;

            CurrentValue = initialValue;
            proposalHistory = new List<Proposal>();
            ProposalHistory = new ReadOnlyCollection<Proposal>(proposalHistory);
        }

        internal void AddProposal(Proposal proposal)
        {
            if (!proposal.Alias.Equals(Alias))
            {
                throw new InvalidOperationException("Proposal alias does not match field alias.");
            }

            proposalHistory.Add(proposal);
        }

        internal void SetCurrentValue(FieldValue newValue)
        {
            if (!newValue.Alias.Equals(Alias))
            {
                throw new InvalidOperationException("New value alias does not match field alias.");
            }

            CurrentValue = newValue;
        }

        internal void SetBinding(FieldBinding? binding)
        {
            if (binding is not null && !binding.Alias.Equals(Alias))
            {
                throw new InvalidOperationException("Binding alias does not match field alias.");
            }

            Binding = binding;
        }

        internal void SetDescriptor(FieldDescriptor? descriptor)
        {
            if (descriptor is not null && !descriptor.Alias.Equals(Alias))
            {
                throw new InvalidOperationException("Descriptor alias does not match field alias.");
            }

            Descriptor = descriptor;
        }

        internal void SetDependencySnapshot(DependencySnapshot? dependencySnapshot)
        {
            Dependency = dependencySnapshot;
        }
    }
}
