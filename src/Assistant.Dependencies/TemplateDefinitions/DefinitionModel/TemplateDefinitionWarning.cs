namespace Assistant.Dependencies.TemplateDefinitions;

public sealed class TemplateDefinitionWarning
{
    public string Code { get; }
    public string Message { get; }
    public string? TemplateKey { get; }

    public TemplateDefinitionWarning(string code, string message, string? templateKey = null)
    {
        Code = code;
        Message = message;
        TemplateKey = templateKey;
    }

    public override string ToString()
    {
        return TemplateKey is null
            ? $"{Code}: {Message}"
            : $"{Code} [{TemplateKey}]: {Message}";
    }
}