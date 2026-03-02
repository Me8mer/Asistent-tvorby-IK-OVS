using System;
using System.Collections.Generic;

namespace Assistant.Core.Model
{
    public sealed class FieldStore
    {
        private readonly IReadOnlyDictionary<FieldAlias, FieldNode> fieldsByAlias;

        public FieldStore(IReadOnlyDictionary<FieldAlias, FieldNode> fieldsByAlias)
        {
            this.fieldsByAlias = fieldsByAlias ?? throw new ArgumentNullException(nameof(fieldsByAlias));
        }

        public bool TryGetField(FieldAlias alias, out FieldNode fieldNode)
        {
            return fieldsByAlias.TryGetValue(alias, out fieldNode!);
        }

        public FieldNode GetField(FieldAlias alias)
        {
            if (!fieldsByAlias.TryGetValue(alias, out FieldNode? fieldNode))
            {
                throw new KeyNotFoundException($"Field with alias '{alias}' was not found.");
            }

            return fieldNode;
        }

        public IEnumerable<FieldNode> GetAllFields()
        {
            return fieldsByAlias.Values;
        }

        public bool TrySetCurrentValue(FieldValue newValue, out string? rejectionReason)
        {
            if (!fieldsByAlias.TryGetValue(newValue.Alias, out FieldNode? fieldNode))
            {
                rejectionReason = $"Field with alias '{newValue.Alias}' does not exist.";
                return false;
            }

            fieldNode.SetCurrentValue(newValue);
            rejectionReason = null;
            return true;
        }

        public bool TryAddProposal(Proposal proposal, out string? rejectionReason)
        {
            if (!fieldsByAlias.TryGetValue(proposal.Alias, out FieldNode? fieldNode))
            {
                rejectionReason = $"Field with alias '{proposal.Alias}' does not exist.";
                return false;
            }

            fieldNode.AddProposal(proposal);
            rejectionReason = null;
            return true;
        }

        public bool TryUpdateDependencySnapshot(FieldAlias alias, DependencySnapshot snapshot, out string? rejectionReason)
        {
            if (!fieldsByAlias.TryGetValue(alias, out FieldNode? fieldNode))
            {
                rejectionReason = $"Field with alias '{alias}' does not exist.";
                return false;
            }

            fieldNode.SetDependencySnapshot(snapshot);
            rejectionReason = null;
            return true;
        }
    }
}