using System;
using System.Collections.Generic;

namespace Assistant.AI.Models
{
    public sealed class LlmPrompt
    {
        public string SystemText { get; }
        public string UserText { get; }
        public IReadOnlyList<string> ContextReferences { get; }

        public LlmPrompt(string systemText, string userText, IReadOnlyList<string>? contextReferences = null)
        {
            if (systemText is null)
            {
                throw new ArgumentNullException(nameof(systemText));
            }

            if (userText is null)
            {
                throw new ArgumentNullException(nameof(userText));
            }

            SystemText = systemText;
            UserText = userText;
            ContextReferences = contextReferences ?? Array.Empty<string>();
        }
    }
}
