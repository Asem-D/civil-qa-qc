namespace CivilQc.Ai;

/// <summary>
/// Generates QA/QC rule YAML configurations from natural language descriptions
/// or standards documents using an LLM.
/// </summary>
public class RuleGeneratorService
{
    private readonly OpenAiClient _client;

    public RuleGeneratorService(OpenAiClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <summary>
    /// Generate a rule YAML config from a text description.
    /// </summary>
    public async Task<string> GenerateFromDescriptionAsync(string description)
    {
        var systemPrompt = BuildSystemPrompt();
        var userMessage = $"Generate a QA/QC rule configuration based on this description:\n\n{description}";
        return await _client.ChatAsync(systemPrompt, userMessage);
    }

    /// <summary>
    /// Generate a rule YAML config from a standards document (read from file).
    /// </summary>
    public async Task<string> GenerateFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Standards file not found: {filePath}");

        var content = await File.ReadAllTextAsync(filePath);

        // Truncate very large files to fit in context
        if (content.Length > 30_000)
            content = content[..30_000] + "\n\n[... truncated, file too long ...]";

        var systemPrompt = BuildSystemPrompt();
        var userMessage = $"Based on the following CAD/BIM standards document, generate a QA/QC rule configuration:\n\n---\n{content}\n---";
        return await _client.ChatAsync(systemPrompt, userMessage);
    }

    private static string BuildSystemPrompt()
    {
        return @"You are a Civil 3D QA/QC rule generator. You produce valid YAML rule configuration files for a batch QA/QC tool.

Output ONLY the YAML content. No explanations, no markdown fences, no extra text.

## YAML Schema

The file must have a top-level `rules:` key containing a list of rule objects. Each rule has:

```yaml
rules:
  - id: UNIQUE-ID
    name: Human-readable name
    check_type: CheckType
    severity: Info|Warning|Error|Critical
    description: What this rule checks
    parameters:
      key: value
```

## Available Check Types

1. **FileSize** — Reports drawing file size
   - Parameters: `warning_mb` (int), `error_mb` (int)

2. **LayerNaming** — Validates layer names against allowed prefixes
   - Parameters: `allowed_prefixes` (list of strings), `separator` (string, default ""-""), `require_prefix` (bool)

3. **EmptyLayers** — Finds layers with zero entities
   - Parameters: `exclude_defaults` (bool)

4. **UnusedLayers** — Finds frozen/off layers with zero entities
   - Parameters: `exclude_defaults` (bool)

5. **DrawingUnits** — Checks INSUNITS system variable
   - Parameters: `expected` (string: ""Meters"", ""Feet"", ""Inches"", etc.)

6. **XrefStatus** — Reports external reference status
   - Parameters: `fail_on_missing` (bool), `warn_on_overlay` (bool)

7. **ProxyObjects** — Counts proxy entities from third-party apps
   - Parameters: `max_count` (int, 0 = report only)

## Rules

- Use unique IDs like LAYER-004, DRAW-004, PERF-002, etc.
- Set severity appropriately: Critical for show-stoppers, Warning for standards violations, Info for informational
- Provide clear descriptions
- Use snake_case for parameter keys
- Output ONLY the YAML, nothing else";
    }
}
