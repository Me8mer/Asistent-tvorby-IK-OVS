using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Assistant.Core.Model;
using Assistant.Core.Model.Aliases;
using Assistant.Core.Model.Enums;
using Assistant.Core.Model.Fields;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Assistant.Parser.OpenXml
{
    public sealed class OpenXmlTemplateSdtParser : ITemplateSdtParser
    {
        public Task<TemplateInstance> ParseAsync(Stream docxStream, CancellationToken cancellationToken = default)
        {
            var bindings = new List<FieldBinding>();
            var diagnostics = new List<SdtParseDiagnostic>();
            var occurrenceByAlias = new Dictionary<string, int>();

            string? templateVersion = null;

            using (WordprocessingDocument wordDocument = WordprocessingDocument.Open(docxStream, false))
            {
                Body? body = wordDocument.MainDocumentPart?.Document?.Body;

                if (body is null)
                {
                    diagnostics.Add(new SdtParseDiagnostic(
                        code: "DOCX_NO_BODY",
                        message: "Main document body is missing.",
                        sdtId: null));

                    return Task.FromResult(new TemplateInstance(templateVersion, bindings, diagnostics));
                }

                IEnumerable<SdtElement> allSdts = body.Descendants<SdtElement>();

                foreach (SdtElement sdtElement in allSdts)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string? sdtId = ReadSdtId(sdtElement);
                    string? tagValue = ReadTagValue(sdtElement);

                    if (string.IsNullOrWhiteSpace(tagValue))
                    {
                        diagnostics.Add(new SdtParseDiagnostic(
                            code: "SDT_MISSING_TAG",
                            message: "SDT has no tag value.",
                            sdtId: sdtId));
                        continue;
                    }

                    if (!FieldAlias.TryCreate(tagValue, out FieldAlias fieldAlias, out string? aliasError))
                    {
                        diagnostics.Add(new SdtParseDiagnostic(
                            code: "SDT_INVALID_ALIAS",
                            message: $"SDT tag is not a valid alias. {aliasError}",
                            sdtId: sdtId));
                        continue;
                    }

                    int occurrenceIndex = NextOccurrenceIndex(occurrenceByAlias, fieldAlias.Value);

                    bindings.Add(new FieldBinding(
                        alias: fieldAlias,
                        sdtId: sdtId,
                        occurrenceIndex: occurrenceIndex,
                        locationHint: null,
                        contentKind: FieldContentKind.Unknown));
                }
            }

            return Task.FromResult(new TemplateInstance(templateVersion, bindings, diagnostics));
        }

        private static string? ReadSdtId(SdtElement sdtElement)
        {
            SdtProperties? properties = sdtElement.SdtProperties;
            SdtId? sdtId = properties?.GetFirstChild<SdtId>();
            return sdtId?.Val?.Value.ToString();
        }

        private static string? ReadTagValue(SdtElement sdtElement)
        {
            SdtProperties? properties = sdtElement.SdtProperties;
            Tag? tag = properties?.GetFirstChild<Tag>();
            return tag?.Val?.Value;
        }

        private static int NextOccurrenceIndex(Dictionary<string, int> occurrenceByAlias, string aliasValue)
        {
            if (!occurrenceByAlias.TryGetValue(aliasValue, out int currentIndex))
            {
                occurrenceByAlias[aliasValue] = 0;
                return 0;
            }

            int nextIndex = currentIndex + 1;
            occurrenceByAlias[aliasValue] = nextIndex;
            return nextIndex;
        }
    }
}