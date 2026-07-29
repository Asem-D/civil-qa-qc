namespace CivilQc.Engine;

public class ReportData
{
    public required string DrawingPath { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public required string ToolVersion { get; set; }
    public List<CheckResult> Results { get; set; } = new();

    public int TotalChecks => Results.Count;
    public int Passed => Results.Count(r => r.Passed);
    public int Failed => Results.Count(r => !r.Passed);
    public int CriticalCount => Results.Count(r => !r.Passed && r.Severity == Severity.Critical);
    public int ErrorCount => Results.Count(r => !r.Passed && r.Severity == Severity.Error);
    public int WarningCount => Results.Count(r => !r.Passed && r.Severity == Severity.Warning);
    public int InfoCount => Results.Count(r => !r.Passed && r.Severity == Severity.Info);
}
