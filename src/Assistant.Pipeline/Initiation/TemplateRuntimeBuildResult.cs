using System;
using System.Collections.Generic;
using Assistant.Core.Model;
using Assistant.Parser.OpenXml;

namespace Assistant.Pipeline.Initiation
{
    public sealed class TemplateRuntimeBuildResult
    {
        public InternalModelRuntime? Runtime { get; }
        public IReadOnlyList<SdtParseDiagnostic> Diagnostics { get; }
        public bool IsSuccess => Runtime is not null;

        public TemplateRuntimeBuildResult(
            InternalModelRuntime? runtime,
            IReadOnlyList<SdtParseDiagnostic> diagnostics)
        {
            Runtime = runtime;
            Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        }
    }
}