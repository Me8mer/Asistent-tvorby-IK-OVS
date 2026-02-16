using Assistant.Parser.OpenXml;

var path = Path.Combine(AppContext.BaseDirectory, "working_template.docx");
await using var stream = File.OpenRead(path);

var parser = new OpenXmlTemplateSdtParser();
var result = await parser.ParseAsync(stream);

Console.WriteLine($"Bindings: {result.Bindings.Count}");
Console.WriteLine($"Diagnostics: {result.Diagnostics.Count}");

foreach (var binding in result.Bindings)
{
    Console.WriteLine(
        $"Alias={binding.Alias.Value} | " +
        $"Occ={binding.OccurrenceIndex} | " +
        $"SdtId={binding.SdtId ?? "null"} | " +
        $"LocationHint={(binding.LocationHint ?? "null")} | " +
        $"ContentKind={binding.ContentKind}"
    );
}


foreach (var diag in result.Diagnostics)
{
    Console.WriteLine($"[{diag.Code}] {diag.Message}");
}
