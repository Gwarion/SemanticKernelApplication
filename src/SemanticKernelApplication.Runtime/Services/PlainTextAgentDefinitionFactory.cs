using System.Text.RegularExpressions;
using SemanticKernelApplication.Abstractions.Agents;
using SemanticKernelApplication.Abstractions.Workbench;
using SemanticKernelApplication.Tools.Providers;

namespace SemanticKernelApplication.Runtime.Services;

public sealed class PlainTextAgentDefinitionFactory
{
    private readonly IAiProviderCatalog _providerCatalog;

    public PlainTextAgentDefinitionFactory(IAiProviderCatalog providerCatalog)
    {
        _providerCatalog = providerCatalog;
    }

    public AgentDefinition Create(PlainTextAgentCreationRequest request)
    {
        var description = request.Description.Trim();
        var name = BuildName(description);
        var role = BuildRole(description);
        var provider = _providerCatalog.GetProvider(request.PreferredProviderId);

        var goals = description
            .Split(['.', ';', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(3)
            .ToArray();

        var instructions = new AgentInstructionSet(
            $"You are {name}. {description}",
            goals,
            ["Keep responses concise, actionable, and safe for display in the activity panel."]);

        return new AgentDefinition(
            Guid.NewGuid().ToString("N"),
            name,
            AgentKind.UserDefined,
            role,
            instructions,
            provider?.Id,
            Tags: ExtractTags(description),
            Metadata: new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["source"] = "plaintext",
                ["rawDescription"] = description
            });
    }

    private static string BuildName(string description)
    {
        var lowered = description.ToLowerInvariant();

        if (lowered.Contains("test") || lowered.Contains("qa"))
        {
            return "QA Sentinel";
        }

        if (lowered.Contains("security") || lowered.Contains("safe"))
        {
            return "Safety Analyst";
        }

        if (lowered.Contains("frontend") || lowered.Contains("ui") || lowered.Contains("design"))
        {
            return "Interface Designer";
        }

        if (lowered.Contains("backend") || lowered.Contains(".net") || lowered.Contains("api"))
        {
            return "Backend Specialist";
        }

        if (lowered.Contains("data") || lowered.Contains("analytics"))
        {
            return "Data Interpreter";
        }

        var firstWords = Regex.Replace(description, "[^a-zA-Z0-9 ]", " ")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(3)
            .ToArray();

        return firstWords.Length == 0
            ? "General Specialist"
            : string.Join(' ', firstWords.Select(Capitalize));
    }

    private static string BuildRole(string description)
    {
        return description.Split(['.', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault()
            ?? "General-purpose specialist";
    }

    private static IReadOnlyCollection<string> ExtractTags(string description)
    {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var token in description.Split([' ', ',', '.', ';', ':', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (token.Length >= 4)
            {
                tags.Add(token.ToLowerInvariant());
            }
        }

        return tags.Take(6).ToArray();
    }

    private static string Capitalize(string value) =>
        string.IsNullOrWhiteSpace(value) ? value : char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
}
