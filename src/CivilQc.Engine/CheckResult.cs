namespace CivilQc.Engine;

public class CheckResult
{
    public required string RuleId { get; set; }
    public required string RuleName { get; set; }
    public Severity Severity { get; set; }
    public bool Passed { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ScreenshotPath { get; set; }
    public string? ObjectName { get; set; }
    public ObjectType ObjectType { get; set; } = ObjectType.Unknown;
    public string? LayerName { get; set; }
    public Dictionary<string, string> Details { get; set; } = new();
    public string? SuggestedFix { get; set; }
}
