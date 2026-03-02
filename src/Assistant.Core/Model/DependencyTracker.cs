using System;

namespace Assistant.Core.Model
{
    public sealed class DependencyTracker
    {
        public DependencySnapshot? Snapshot { get; private set; }

        public DependencyTracker()
        {
        }

        public void SetSnapshot(DependencySnapshot? dependencySnapshot)
        {
            Snapshot = dependencySnapshot;
        }

        public void Clear()
        {
            Snapshot = null;
        }
    }
}