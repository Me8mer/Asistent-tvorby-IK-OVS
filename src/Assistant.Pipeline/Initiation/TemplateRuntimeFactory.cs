using System;
using System.Collections.Generic;
using Assistant.Core.Model;
using Assistant.Parser.OpenXml;

namespace Assistant.Pipeline.Initiation
{
    public sealed class TemplateRuntimeFactory
    {
        private readonly InternalModelRuntimeBuilder runtimeBuilder;

        public TemplateRuntimeFactory(InternalModelRuntimeBuilder runtimeBuilder)
        {
            this.runtimeBuilder = runtimeBuilder ?? throw new ArgumentNullException(nameof(runtimeBuilder));
        }

        public TemplateRuntimeBuildResult BuildRuntime(TemplateInstance instance, TemplateDefinition definition)
        {
            if (instance is null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (definition is null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            var diagnostics = new List<SdtParseDiagnostic>();
            diagnostics.AddRange(instance.Diagnostics);

            var bindingsByAlias = new Dictionary<FieldAlias, FieldBinding>();
            var seenAliases = new HashSet<FieldAlias>();

            bool hasDuplicateAlias = false;

            foreach (FieldBinding binding in instance.Bindings)
            {
                if (!definition.DescriptorsByAlias.ContainsKey(binding.Alias))
                {
                    diagnostics.Add(new SdtParseDiagnostic(
                        code: "DOCX_UNKNOWN_ALIAS",
                        message: $"Template contains alias '{binding.Alias}', but it is not present in the definition.",
                        sdtId: binding.SdtId));
                    continue;
                }

                if (!seenAliases.Add(binding.Alias))
                {
                    hasDuplicateAlias = true;
                    diagnostics.Add(new SdtParseDiagnostic(
                        code: "DOCX_DUPLICATE_ALIAS",
                        message: $"Template contains duplicate alias '{binding.Alias}'.",
                        sdtId: binding.SdtId));
                    continue;
                }

                bindingsByAlias.Add(binding.Alias, binding);
            }

            if (hasDuplicateAlias)
            {
                return new TemplateRuntimeBuildResult(runtime: null, diagnostics: diagnostics);
            }

            InternalModelRuntime runtime = runtimeBuilder.Build(
                templateVersion: definition.TemplateVersion,
                aliases: definition.FieldAliases,
                bindingsByAlias: bindingsByAlias,
                descriptorsByAlias: definition.DescriptorsByAlias,
                sections: definition.Sections);

            return new TemplateRuntimeBuildResult(runtime, diagnostics);
        }
    }
}