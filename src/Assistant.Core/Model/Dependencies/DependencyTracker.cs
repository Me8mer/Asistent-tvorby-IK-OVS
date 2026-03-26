using System;

/// <summary>
/// Holds current dependency state for a field.
/// 
/// Thin wrapper over DependencySnapshot.
/// 
/// Exists so dependency state can evolve independently.
/// </summary>
namespace Assistant.Core.Model.Dependencies
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