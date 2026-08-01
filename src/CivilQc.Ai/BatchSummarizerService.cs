using System.Text;
using System.Text.Json;

namespace CivilQc.Ai;

/// <summary>
/// Analyzes batch QA/QC results and produces an executive summary
/// identifying common issues, problem drawings, and recommendations.
/// </summary>
public class BatchSummarizerService
{
    private readonly OpenAiClient _client;

    public BatchSummarizerService(OpenAiClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    /// Summarize all JSON result files in a directory.
    /// </summary>
    public async Task<string> SummarizeDirectoryAsync(string resultsDir)
    {
        if (!Directory.Exists(resultsDir))
            throw new DirectoryNotFoundException($"Results directory not found: {resultsDir}");

        var jsonFiles = Directory.GetFiles(resultsDir, "*.json", SearchOption.AllDirectories);
        if (jsonFiles.Length == 0)
            throw new InvalidOperationException($"No JSON files found in {resultsDir}");

        var combinedJson = await LoadAndCombineResults(jsonFiles);

        const int MaxContextChars = 80_000;
        if (combinedJson.Length > MaxContextChars)
        {
            Console.Error.WriteLine(
                $"WARNING: Combined results ({combinedJson.Length:N0} chars) exceed {MaxContextChars:N0} character context limit. " +
                $"Truncating to fit.");
            combinedJson = combinedJson[..MaxContextChars] + "\n]";
        }

        return await GenerateSummaryAsync(combinedJson);
    }

    /// <summary>
    /// Summarize a single batch results JSON file.
    /// </summary>
    public async Task<string> SummarizeFileAsync(string resultsFile)
    {
        if (!File.Exists(resultsFile))
            throw new FileNotFoundException($"Results file not found: {resultsFile}");

        var content = await File.ReadAllTextAsync(resultsFile);

        // If it's a single report, wrap it as an array
        var trimmed = content.TrimStart();
        if (!trimmed.StartsWith('['))
            content = $"[{content}]";

        return await GenerateSummaryAsync(content);
    }

    private async Task<string> GenerateSummaryAsync(string resultsJson)
    {
        var systemPrompt = BuildSystemPrompt();
        var userMessage = $"Analyze these QA/QC results and produce an executive summary:\n\n{resultsJson}";
        return await _client.ChatAsync(systemPrompt, userMessage);
    }

    private static async Task<string> LoadAndCombineResults(string[] jsonFiles)
    {
        var allResults = new StringBuilder();
        allResults.AppendLine("[");

        bool first = true;
        foreach (var file in jsonFiles)
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);
                var trimmed = content.TrimStart();

                // Handle both single objects and arrays
                if (trimmed.StartsWith('['))
                {
                    // Strip outer brackets and add items
                    var inner = trimmed[1..^1].Trim();
                    if (inner.Length > 0)
                    {
                        if (!first) allResults.AppendLine(",");
                        first = false;
                        allResults.Append(inner);
                    }
                }
                else
                {
                    if (!first) allResults.AppendLine(",");
                    first = false;
                    allResults.Append(content.Trim());
                }
            }
            catch
            {
                // Skip unreadable files
            }
        }

        allResults.AppendLine("]");
        return allResults.ToString();
    }

    private static string BuildSystemPrompt()
    {
        return @"You are a Civil 3D QA/QC analyst. You analyze batch QA/QC check results across multiple drawings and produce an executive summary.

Output a clean Markdown summary. Use headings, bullet points, and tables where helpful.

## Required Sections

### 1. Overview
- Total drawings checked
- Overall pass/fail rates
- Severity breakdown (Critical, Error, Warning, Info)

### 2. Most Common Issues
- Top issues by frequency across all drawings
- Group by check type (layer naming, empty layers, xrefs, etc.)

### 3. Problem Drawings
- Drawings with the most failures (top 5-10)
- List the specific issues for each

### 4. Patterns & Trends
- Cross-drawing patterns (e.g., ""all drawings from Consultant X fail layer naming"")
- Recurring parameter issues
- Systemic problems

### 5. Prioritized Recommendations
- Ranked list of what to fix first
- Group by: Quick wins, High impact, Long-term improvements
- Estimate effort where possible

## Rules

- Be specific — cite actual drawing names, rule IDs, and counts
- Focus on actionable insights, not just data regurgitation
- Identify root causes, not just symptoms
- Write for a project manager who needs to assign fixes";
    }
}
