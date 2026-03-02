using System;

namespace Assistant.Core.Model
{
    public sealed class FieldNode
    {
        public FieldAlias Alias { get; }
        public SectionAlias? ParentSectionAlias { get; internal set; }

        public FieldState State { get; }
        public FieldMetadata Metadata { get; }
        public DependencyTracker Dependency { get; }

        public FieldValue CurrentValue => State.CurrentValue;
        public System.Collections.Generic.IReadOnlyList<Proposal> ProposalHistory => State.ProposalHistory;

        public FieldBinding? Binding => Metadata.Binding;
        public FieldDescriptor? Descriptor => Metadata.Descriptor;

        public DependencySnapshot? DependencySnapshot => Dependency.Snapshot;

        public FieldNode(FieldAlias alias, FieldBinding? binding = null, FieldDescriptor? descriptor = null)
            : this(alias, new FieldState(alias), binding, descriptor)
        {
        }

        public FieldNode(FieldValue initialValue, FieldBinding? binding = null, FieldDescriptor? descriptor = null)
            : this(initialValue.Alias, new FieldState(initialValue), binding, descriptor)
        {
        }

        private FieldNode(FieldAlias alias, FieldState state, FieldBinding? binding, FieldDescriptor? descriptor)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            Alias = alias;

            State = state;
            Metadata = new FieldMetadata(alias, binding, descriptor);
            Dependency = new DependencyTracker();
        }

        internal void AddProposal(Proposal proposal)
        {
            State.AddProposal(proposal, Alias);
        }

        internal void SetCurrentValue(FieldValue newValue)
        {
            State.SetCurrentValue(newValue, Alias);
        }

        internal void SetBinding(FieldBinding? binding)
        {
            Metadata.SetBinding(Alias, binding);
        }

        internal void SetDescriptor(FieldDescriptor? descriptor)
        {
            Metadata.SetDescriptor(Alias, descriptor);
        }

        internal void SetDependencySnapshot(DependencySnapshot? dependencySnapshot)
        {
            Dependency.SetSnapshot(dependencySnapshot);
        }
    }
}