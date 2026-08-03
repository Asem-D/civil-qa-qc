using CivilQc.Engine;

namespace CivilQc.Ai;

/// <summary>
/// Generates actionable fix suggestions for failed QA/QC rules using an LLM.
/// Helps users understand how to resolve violations in Civil 3D.
/// </summary>
public class FixSuggestionService
{
    private readonly OpenAiClient _client;

    public FixSuggestionService(OpenAiClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    /// Generate fix suggestions for a batch of failed check results.
    /// Returns a dictionary mapping RuleId to suggested fix text.
    /// </summary>
    public async Task<Dictionary<string, string>> GenerateFixSuggestionsAsync(
        IEnumerable<CheckResult> failedResults)
    {
        var results = failedResults.ToList();
        if (results.Count == 0)
            return new Dictionary<string, string>();

        // Group by RuleId to avoid duplicate suggestions for the same rule type
        var grouped = results.GroupBy(r => r.RuleId);
        var suggestions = new Dictionary<string, string>();

        foreach (var group in grouped)
        {
            var ruleId = group.Key;
            var ruleResults = group.ToList();

            try
            {
                var suggestion = await GenerateForRuleAsync(ruleId, ruleResults);
                suggestions[ruleId] = suggestion;
            }
            catch (AiApiException)
            {
                // AI unavailable - skip suggestions gracefully
                suggestions[ruleId] = "AI suggestion unavailable. Check Civil 3D documentation for this rule type.";
            }
        }

        return suggestions;
    }

    /// <summary>
    /// Generate a fix suggestion for a single rule type with multiple violations.
    /// </summary>
    private async Task<string> GenerateForRuleAsync(string ruleId, List<CheckResult> violations)
    {
        var systemPrompt = BuildSystemPrompt();
        var userMessage = BuildUserMessage(ruleId, violations);
        return await _client.ChatAsync(systemPrompt, userMessage);
    }

    private static string BuildUserMessage(string ruleId, List<CheckResult> violations)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Rule {ruleId} failed with {violations.Count} violation(s):");
        sb.AppendLine();

        // Include up to 5 sample violations for context
        foreach (var v in violations.Take(5))
        {
            sb.AppendLine($"- {v.Message}");
            if (v.Details.Count > 0)
            {
                foreach (var detail in v.Details.Take(3))
                    sb.AppendLine($"  {detail.Key}: {detail.Value}");
            }
        }

        if (violations.Count > 5)
            sb.AppendLine($"  ... and {violations.Count - 5} more");

        sb.AppendLine();
        sb.AppendLine("Provide a concise, actionable fix guide for a Civil 3D user.");

        return sb.ToString();
    }

    private static string BuildSystemPrompt()
    {
        return @"You are a Civil 3D QA/QC expert. When a rule fails, you provide concise, actionable steps to fix the issue in Civil 3D.

## Guidelines
- Be specific: mention Civil 3D commands, dialogs, and menu paths
- Be concise: 2-4 sentences maximum
- Focus on the fix, not the problem description
- Use Civil 3D terminology (Layer Properties Manager, Block Editor, etc.)
- If multiple fixes exist, mention the most common one first
- For critical issues, mention potential consequences of not fixing

## Common Fix Patterns
- Layer issues: Layer Properties Manager (LA command)
- Block issues: Block Editor (BE command) or BEDIT
- Xref issues: XREF command, check path resolution
- Text/annotation: Properties palette, Annotative Scaling dialog
- Drawing units: UNITS command
- Proxy objects: PROXYSHOW system variable, remove third-party dependencies

Output ONLY the fix suggestion text. No headers, no markdown formatting, no prefixes.";
    }
}
