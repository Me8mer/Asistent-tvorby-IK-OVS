using System;
using System.Collections.Generic;

namespace Assistant.Core.Model
{
    public sealed class InternalModel
    {
        private readonly Dictionary<FieldAlias, FieldNode> fieldsByAlias;

        public string TemplateVersion { get; }
        public IReadOnlyDictionary<FieldAlias, FieldNode> FieldsByAlias => fieldsByAlias;

        public InternalModel(string templateVersion, IEnumerable<FieldAlias> aliases)
            : this(templateVersion, aliases, bindingsByAlias: null, descriptorsByAlias: null)
        {
        }

        public InternalModel(
            string templateVersion,
            IEnumerable<FieldAlias> aliases,
            IReadOnlyDictionary<FieldAlias, FieldBinding>? bindingsByAlias,
            IReadOnlyDictionary<FieldAlias, FieldDescriptor>? descriptorsByAlias)
        {
            if (string.IsNullOrWhiteSpace(templateVersion))
            {
                throw new ArgumentException("Template version must not be null, empty, or whitespace.", nameof(templateVersion));
            }

            if (aliases is null)
            {
                throw new ArgumentNullException(nameof(aliases));
            }

            TemplateVersion = templateVersion;
            fieldsByAlias = new Dictionary<FieldAlias, FieldNode>();

            foreach (FieldAlias alias in aliases)
            {
                if (fieldsByAlias.ContainsKey(alias))
                {
                    throw new InvalidOperationException($"Duplicate alias detected in internal model: {alias}.");
                }

                FieldBinding? binding = null;
                if (bindingsByAlias is not null)
                {
                    bindingsByAlias.TryGetValue(alias, out binding);
                }

                FieldDescriptor? descriptor = null;
                if (descriptorsByAlias is not null)
                {
                    descriptorsByAlias.TryGetValue(alias, out descriptor);
                }

                fieldsByAlias.Add(alias, new FieldNode(alias, binding, descriptor));
            }
        }

        public bool TryGetField(FieldAlias alias, out FieldNode fieldNode)
        {
            return fieldsByAlias.TryGetValue(alias, out fieldNode!);
        }

        public FieldNode GetField(FieldAlias alias)
        {
            if (!fieldsByAlias.TryGetValue(alias, out FieldNode? fieldNode))
            {
                throw new KeyNotFoundException($"Field with alias '{alias}' was not found in the internal model.");
            }

            return fieldNode;
        }

        public IEnumerable<FieldNode> GetAllFields()
        {
            return fieldsByAlias.Values;
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

        public bool TryUpdateDependencySnapshot(
            FieldAlias alias,
            DependencySnapshot snapshot,
            out string? rejectionReason)
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
