using System;
using System.Collections.Generic;
using Assistant.Core.Model;
using Assistant.Core.Model.Fields;

namespace Assistant.Parser.OpenXml
{
    public sealed class TemplateInstance
    {
        public string? TemplateVersion { get; }
        public IReadOnlyList<FieldBinding> Bindings { get; }
        public IReadOnlyList<SdtParseDiagnostic> Diagnostics { get; }

        public TemplateInstance(
            string? templateVersion,
            IReadOnlyList<FieldBinding> bindings,
            IReadOnlyList<SdtParseDiagnostic> diagnostics)
        {
            TemplateVersion = templateVersion;
            Bindings = bindings;
            Diagnostics = diagnostics;
        }
    }

    public sealed class SdtParseDiagnostic
    {
        public string Code { get; }
        public string Message { get; }
        public string? SdtId { get; }

        public SdtParseDiagnostic(string code, string message, string? sdtId)
        {
            Code = code;
            Message = message;
            SdtId = sdtId;
        }
    }
}