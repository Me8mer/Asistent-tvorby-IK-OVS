using System;
using System.Collections.Generic;
using Assistant.AI.Abstractions;
using Assistant.AI.Models;
using Assistant.Core.Model;
using Assistant.Core.Model.Aliases;

namespace Assistant.AI.Routing
{
    public sealed class DefaultRoutingPolicy : IRoutingPolicy
    {
        private readonly RoutingDefaults defaults;
        private readonly IReadOnlyDictionary<FieldAlias, ModelRouting> perAliasOverrides;

        public DefaultRoutingPolicy(
            RoutingDefaults defaults,
            IReadOnlyDictionary<FieldAlias, ModelRouting>? perAliasOverrides = null)
        {
            this.defaults = defaults ?? throw new ArgumentNullException(nameof(defaults));
            this.perAliasOverrides = perAliasOverrides ?? new Dictionary<FieldAlias, ModelRouting>();
        }

        public ModelRouting ResolveRouting(GenerationRequest request)
        {
            if (perAliasOverrides.TryGetValue(request.Alias, out ModelRouting? overrideRouting))
            {
                return overrideRouting;
            }

            return request.Mode switch
            {
                GenerationMode.InitialPrefill => defaults.InitialPrefill,
                GenerationMode.Regenerate => defaults.Regenerate,
                GenerationMode.Improve => defaults.Improve,
                _ => defaults.InitialPrefill
            };
        }
    }
}
