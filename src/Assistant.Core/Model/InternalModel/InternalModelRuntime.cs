using System;

/// <summary>
/// Combines static model + runtime components.
/// 
/// Contains:
/// - InternalModel (structure)
/// - FieldStore (state access)
/// - ProposalApplier (AI integration)
/// 
/// This is what the assistant actually uses.
/// </summary>
namespace Assistant.Core.Model.InternalModel
{
    public sealed class InternalModelRuntime
    {
        public InternalModel Model { get; }
        public FieldStore Fields { get; }
        public ProposalApplier Proposals { get; }

        internal InternalModelRuntime(InternalModel model, FieldStore fields, ProposalApplier proposals)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
            Proposals = proposals ?? throw new ArgumentNullException(nameof(proposals));
        }
    }
}