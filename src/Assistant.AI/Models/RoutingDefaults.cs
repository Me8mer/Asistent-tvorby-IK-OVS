using System;

namespace Assistant.AI.Models
{
    public sealed class RoutingDefaults
    {
        public ModelRouting InitialPrefill { get; }
        public ModelRouting Regenerate { get; }
        public ModelRouting Improve { get; }

        public RoutingDefaults(ModelRouting initialPrefill, ModelRouting regenerate, ModelRouting improve)
        {
            InitialPrefill = initialPrefill ?? throw new ArgumentNullException(nameof(initialPrefill));
            Regenerate = regenerate ?? throw new ArgumentNullException(nameof(regenerate));
            Improve = improve ?? throw new ArgumentNullException(nameof(improve));
        }
    }
}
