using System;

namespace Assistant.Core.Model
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