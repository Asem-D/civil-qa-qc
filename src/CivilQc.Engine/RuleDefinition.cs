namespace CivilQc.Engine;

public enum Severity
{
    Info,
    Warning,
    Error,
    Critical
}

public enum ObjectType
{
    Surface,
    Alignment,
    Corridor,
    PipeNetwork,
    PressureNetwork,
    FeatureLine,
    PointGroup,
    BlockReference,
    Layer,
    Drawing,
    Unknown
}

public class RuleDefinition
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public required string CheckType { get; set; }
    public Severity Severity { get; set; } = Severity.Warning;
    public bool Enabled { get; set; } = true;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public List<string> AppliesTo { get; set; } = new();
}
