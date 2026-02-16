using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Assistant.Core.Model;

namespace Assistant.Parser.OpenXml
{
    public interface ITemplateSdtParser
    {
        Task<SdtParseResult> ParseAsync(
            Stream docxStream,
            CancellationToken cancellationToken = default);
    }

    public sealed class SdtParseResult
    {
        public IReadOnlyList<FieldBinding> Bindings { get; }
        public IReadOnlyList<SdtParseDiagnostic> Diagnostics { get; }

        public SdtParseResult(
            IReadOnlyList<FieldBinding> bindings,
            IReadOnlyList<SdtParseDiagnostic> diagnostics)
        {
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
