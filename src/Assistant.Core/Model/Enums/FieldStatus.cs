/// <summary>
/// Represents the current state of a field value (runtime).
/// Drives proposal merge logic.
/// </summary>
namespace Assistant.Core.Model.Enums
{
    public enum FieldStatus
    {
        Empty = 0,
        Deterministic = 1,
        NeedsValidation = 2,
        AiProposal = 3,
        AiInteractive = 4,
        Confirmed = 5,
        Rejected = 6,
        UserInput = 7
    }
}
