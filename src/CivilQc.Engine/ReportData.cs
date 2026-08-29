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

public class BatchReportData
{
    public required string DirectoryPath { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public required string ToolVersion { get; set; }
    public List<BatchDrawingResult> Results { get; set; } = new();

    public int TotalDrawings => Results.Count;
    public int TotalPassed => Results.Count(r => r.Success && r.Failed == 0);
    public int TotalFailed => Results.Count(r => r.Success && r.Failed > 0);
    public int TotalErrors => Results.Count(r => !r.Success);
}

public class BatchDrawingResult
{
    public required string DrawingPath { get; set; }
    public int Passed { get; set; }
    public int Failed { get; set; }
    public int CriticalCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public bool Success { get; set; }
}
