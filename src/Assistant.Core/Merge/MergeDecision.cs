namespace Assistant.Core.Merge
{
    public sealed class MergeDecision
    {
        public bool IsAllowed { get; }
        public string? DenyReason { get; }
        public FieldStatus ResultingStatus { get; }

        private MergeDecision(bool isAllowed, string? denyReason, FieldStatus resultingStatus)
        {
            IsAllowed = isAllowed;
            DenyReason = denyReason;
            ResultingStatus = resultingStatus;
        }

        public static MergeDecision Allow(FieldStatus resultingStatus)
        {
            return new MergeDecision(isAllowed: true, denyReason: null, resultingStatus: resultingStatus);
        }

        public static MergeDecision Deny(string reason, FieldStatus currentStatus)
        {
            return new MergeDecision(isAllowed: false, denyReason: reason, resultingStatus: currentStatus);
        }
    }
}
