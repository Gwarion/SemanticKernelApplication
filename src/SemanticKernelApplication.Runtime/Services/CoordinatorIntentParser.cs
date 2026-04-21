using System.Text.RegularExpressions;

namespace SemanticKernelApplication.Runtime.Services;

internal static partial class CoordinatorIntentParser
{
    [GeneratedRegex(@"\b(create|add|make|register|spin up)\b.*\bagent\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CreateAgentIntentRegex();

    [GeneratedRegex(@"(?:(?:create|add|make|register|spin up)\s+(?:an?\s+)?)agent(?:\s+(?:that|who|to))?\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CreateAgentPrefixRegex();

    public static bool TryExtractAgentDescription(string userMessage, out string description)
    {
        description = string.Empty;
        if (string.IsNullOrWhiteSpace(userMessage) || !CreateAgentIntentRegex().IsMatch(userMessage))
        {
            return false;
        }

        description = CreateAgentPrefixRegex().Replace(userMessage.Trim(), string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(description))
        {
            description = userMessage.Trim();
        }

        return true;
    }
}
