using CivilQc.Engine;

namespace CivilQc.Rules;

/// <summary>
/// Reports drawing file size and warns if excessive.
/// </summary>
public class FileSizeRule : IRule
{
    public string RuleId => "PERF-001";
    public string Name => "File size";

    public List<CheckResult> Execute(RuleDefinition rule, DrawingContext context)
    {
        var warningMb = 100;
        var errorMb = 500;

        if (rule.Parameters.ContainsKey("warning_mb"))
            warningMb = Convert.ToInt32(rule.Parameters["warning_mb"]);
        if (rule.Parameters.ContainsKey("error_mb"))
            errorMb = Convert.ToInt32(rule.Parameters["error_mb"]);

        var results = new List<CheckResult>();

        if (File.Exists(context.DrawingPath))
        {
            var fileInfo = new FileInfo(context.DrawingPath);
            var sizeMb = fileInfo.Length / (1024.0 * 1024.0);
            var passed = sizeMb < warningMb;
            var message = $"File size: {sizeMb:F1} MB";

            if (sizeMb >= errorMb)
                message += $" (exceeds {errorMb} MB error threshold)";
            else if (sizeMb >= warningMb)
                message += $" (exceeds {warningMb} MB warning threshold)";

            results.Add(new CheckResult
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                Severity = rule.Severity,
                Passed = passed,
                Message = message,
                ObjectType = ObjectType.Drawing,
                Details = new Dictionary<string, string>
                {
                    ["size_bytes"] = fileInfo.Length.ToString(),
                    ["size_mb"] = $"{sizeMb:F1}"
                }
            });
        }

        return results;
    }
}
