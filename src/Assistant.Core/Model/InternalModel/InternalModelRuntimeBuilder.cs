using System;
using System.Collections.Generic;

/// <summary>
/// High-level builder that produces a fully usable runtime.
/// 
/// Combines:
/// - model building
/// - store creation
/// - proposal system wiring
/// 
/// Entry point for execution.
/// </summary>
namespace Assistant.Core.Model.InternalModel
{
    public sealed class InternalModelRuntimeBuilder
    {
        private readonly InternalModelBuilder modelBuilder = new InternalModelBuilder();

        public InternalModelRuntime Build(
            string templateVersion,
            IEnumerable<FieldAlias> aliases,
            IReadOnlyDictionary<FieldAlias, FieldBinding>? bindingsByAlias,
            IReadOnlyDictionary<FieldAlias, FieldDescriptor>? descriptorsByAlias,
            IEnumerable<SectionDescriptor>? sections)
        {
            InternalModel model = modelBuilder.Build(
                templateVersion,
                aliases,
                bindingsByAlias,
                descriptorsByAlias,
                sections);

            var fieldStore = new FieldStore(model.FieldsByAlias);
            var proposalApplier = new ProposalApplier(fieldStore);

            return new InternalModelRuntime(model, fieldStore, proposalApplier);
        }
    }
}