using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Assistant.Core.Model;

namespace Assistant.Parser.OpenXml
{
    public interface ITemplateSdtParser
    {
        Task<TemplateInstance> ParseAsync(
            Stream docxStream,
            CancellationToken cancellationToken = default);
    }

   
}