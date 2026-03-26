using System;
using System.Collections.Generic;
using Assistant.Core.Model.Fields;

/// <summary>
/// Immutable snapshot of dependency state for a field.
/// 
/// Represents:
/// - whether field is blocked
/// - which fields block it
/// 
/// Produced by dependency evaluation logic.
/// </summary>
namespace Assistant.Core.Model.Dependencies
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
