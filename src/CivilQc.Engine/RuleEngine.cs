using System.Text.Json;

namespace CivilQc.Engine;

/// <summary>
/// Orchestrates rule execution against a Civil 3D drawing.
/// This runs OUTSIDE of AutoCAD as part of the CLI process.
/// The actual drawing checks happen inside the plugin via accoreconsole.
/// </summary>
public static class RuleEngine
{
    /// <summary>
    /// Writes plugin arguments to a temp JSON file and returns its path.
    /// The plugin reads from this file during CIVILQC_CHECK execution.
    /// </summary>
    public static string WritePluginArguments(RuleConfig config, string drawingPath, string outputPath, string screenshotDir)
    {
        var enabledRules = config.Rules.Where(r => r.Enabled).ToList();
        var payload = new
        {
            rules = enabledRules,
            drawing = drawingPath,
            output = outputPath,
            screenshots = screenshotDir
        };

        var argsJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        return AccoreHost.WriteArgsFile(argsJson);
    }

    /// <summary>
    /// Parse plugin JSON output into ReportData.
    /// Called by CLI after accoreconsole exits.
    /// </summary>
    public static ReportData ParseResults(string drawingPath, string outputPath)
    {
        if (!File.Exists(outputPath))
        {
            return new ReportData
            {
                DrawingPath = drawingPath,
                ToolVersion = "0.1.0",
                Results = new List<CheckResult>
                {
                    new()
                    {
                        RuleId = "SYSTEM",
                        RuleName = "Plugin execution",
                        Severity = Severity.Critical,
                        Passed = false,
                        Message = "Plugin did not produce output. accoreconsole may have failed."
                    }
                }
            };
        }

        var json = File.ReadAllText(outputPath);
        var results = JsonSerializer.Deserialize<List<CheckResult>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return new ReportData
        {
            DrawingPath = drawingPath,
            ToolVersion = "0.1.0",
            Results = results ?? new List<CheckResult>()
        };
    }
}
