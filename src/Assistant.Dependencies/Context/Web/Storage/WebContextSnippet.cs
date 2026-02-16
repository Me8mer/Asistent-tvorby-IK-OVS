using System.Text.Json.Serialization;

namespace Assistant.Dependencies.Context.Web.Storage
{
    public sealed class WebContextSnippet
    {
        public string Intent { get; }
        public string ProviderKind { get; }
        public string ReferenceId { get; }
        public string Text { get; }

        [JsonConstructor]
        public WebContextSnippet(
            string intent,
            string providerKind,
            string referenceId,
            string text)
        {
            Intent = intent;
            ProviderKind = providerKind;
            ReferenceId = referenceId;
            Text = text;
        }
    }
}
