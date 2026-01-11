using System;
using System.Collections.Generic;

namespace Assistant.Core.Model
{
    public sealed class DependencySnapshot
    {
        public bool IsBlocked { get; }
        public IReadOnlyList<FieldAlias> BlockedBy { get; }
        public IReadOnlyList<string> Reasons { get; }

        public DependencySnapshot(
            bool isBlocked,
            IReadOnlyList<FieldAlias> blockedBy,
            IReadOnlyList<string> reasons)
        {
            if (blockedBy is null)
            {
                throw new ArgumentNullException(nameof(blockedBy));
            }

            if (reasons is null)
            {
                throw new ArgumentNullException(nameof(reasons));
            }

            if (blockedBy.Count != reasons.Count)
            {
                throw new ArgumentException("BlockedBy and Reasons must have the same length.");
            }

            IsBlocked = isBlocked;
            BlockedBy = blockedBy;
            Reasons = reasons;
        }
    }
}
