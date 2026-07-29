namespace CivilQc.Engine;

/// <summary>
/// Interface that all QA/QC rules must implement.
/// Rules are loaded as plugins from the /rules folder.
/// </summary>
public interface IRule
{
    /// <summary>Unique rule ID matching the YAML config (e.g., "LAYER-001").</summary>
    string RuleId { get; }

    /// <summary>Human-readable name.</summary>
    string Name { get; }

    /// <summary>Runs the check against the current drawing and returns results.</summary>
    List<CheckResult> Execute(RuleDefinition rule, DrawingContext context);
}

/// <summary>
/// Provides access to the active Civil 3D document and objects.
/// Wraps AutoCAD/Civil 3D API access to keep rules testable.
/// </summary>
public class DrawingContext
{
    // These are set by the plugin host before rules execute.
    // They wrap the AutoCAD DocumentManager and Civil 3D Database.
    public string DrawingPath { get; set; } = string.Empty;
    public string ScreenshotDir { get; set; } = string.Empty;

    // Will be populated by the plugin with actual API handles
    // For now, placeholder for architecture
    public object? Document { get; set; }
    public object? Database { get; set; }
    public object? CivilDocument { get; set; }
}
