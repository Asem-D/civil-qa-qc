# Writing Custom Rules

This guide walks you through creating a new QA/QC rule for civil-qa-qc.

## How Rules Work

Each rule is a C# class that implements the `IRule` interface. When you run `civil-qc check`, the plugin:

1. Loads your YAML configuration
2. For each enabled rule, finds the matching `IRule` implementation by `CheckType`
3. Calls `Execute()` on each rule
4. Collects results into the report

Rules are **auto-discovered via reflection**. You don't need to register your class anywhere. Just add it to the `CivilQc.Rules` project and it will be found.

## Step 1: Create the Rule Class

Create a new `.cs` file in `src/CivilQc.Rules/`:

```csharp
// SurfacePointCountRule.cs — SURF-001
//
// Reports the number of points in a surface. Surfaces with too few points
// may be incomplete; surfaces with too many may cause performance issues.
//
// YAML parameters:
//   min_points  — minimum expected points (default: 100)
//   max_points  — maximum expected points (default: 1000000)

using Autodesk.AutoCAD.DatabaseServices;
using CivilQc.Engine;

namespace CivilQc.Rules;

public class SurfacePointCountRule : IRule
{
    public string RuleId => "SURF-001";
    public string Name => "Surface point count";

    public List<CheckResult> Execute(RuleDefinition rule, DrawingContext context)
    {
        var db = AcadContext.GetDatabase(context);
        var minPoints = GetIntParam(rule, "min_points", 100);
        var maxPoints = GetIntParam(rule, "max_points", 1_000_000);

        var results = new List<CheckResult>();

        // Your check logic here
        // Access the drawing database via `db`
        // Return results with Passed = true/false

        return results;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static int GetIntParam(RuleDefinition rule, string key, int defaultValue)
    {
        if (!rule.Parameters.TryGetValue(key, out var raw))
            return defaultValue;
        return raw is int i ? i
             : int.TryParse(raw.ToString(), out var parsed) ? parsed
             : defaultValue;
    }
}
```

## Step 2: Add YAML Configuration

Add your rule to `rules/default.yaml`:

```yaml
  # ─── Surface Checks ──────────────────────────────────────────────────
  - id: SURF-001
    name: Surface point count
    check_type: SurfacePointCount
    severity: Warning
    description: >-
      Reports the number of points in a surface. Surfaces with too few
      points may be incomplete; too many may cause performance issues.
    parameters:
      min_points: 100
      max_points: 1000000
```

The `check_type` value must match your class name with "Rule" stripped off. `SurfacePointCountRule` maps to `check_type: SurfacePointCount`.

## Step 3: Write Tests

Add tests in `tests/CivilQc.Tests/`:

```csharp
using CivilQc.Engine;
using Xunit;

namespace CivilQc.Tests;

public class SurfacePointCountRuleTests
{
    [Fact]
    public void LoadDefault_ContainsSurfacePointCount()
    {
        var config = RuleLoader.LoadDefault();
        Assert.Contains(config.Rules, r => r.Id == "SURF-001");
    }

    [Fact]
    public void LoadDefault_SurfacePointCountHasValidSeverity()
    {
        var config = RuleLoader.LoadDefault();
        var rule = config.Rules.First(r => r.Id == "SURF-001");
        Assert.True(Enum.IsDefined(typeof(Severity), rule.Severity));
    }
}
```

Run tests with:

```bash
dotnet test
```

## Step 4: Submit a PR

1. Fork the repo
2. Create a feature branch (`git checkout -b rule/surface-point-count`)
3. Commit your changes
4. Push and open a PR

See [CONTRIBUTING.md](../CONTRIBUTING.md) for details.

## The IRule Interface

```csharp
public interface IRule
{
    string RuleId { get; }          // e.g., "SURF-001"
    string Name { get; }            // e.g., "Surface point count"
    List<CheckResult> Execute(RuleDefinition rule, DrawingContext context);
}
```

## DrawingContext

The `DrawingContext` object provides access to the active Civil 3D drawing:

| Property | Type | Description |
|----------|------|-------------|
| `DrawingPath` | `string` | Full path to the `.dwg` file |
| `ScreenshotDir` | `string` | Directory for screenshot output |
| `Document` | `object?` | AutoCAD `Document` (cast via `AcadContext`) |
| `Database` | `object?` | AutoCAD `Database` (cast via `AcadContext`) |

Use `AcadContext.GetDatabase(context)` to get a typed `Database` reference.

## CheckResult

Each rule returns a list of `CheckResult` objects:

```csharp
new CheckResult
{
    RuleId = rule.Id,
    RuleName = rule.Name,
    Severity = rule.Severity,
    Passed = false,                              // true = no issues found
    Message = "Surface has only 50 points",      // Human-readable description
    ObjectType = ObjectType.Surface,              // What was checked
    ObjectName = "EG-Surface",                   // Optional: name of the object
    LayerName = "TOPO",                          // Optional: layer name
    Details = new Dictionary<string, string>      // Optional extra data
    {
        ["point_count"] = "50",
        ["expected_min"] = "100"
    }
}
```

## Accessing AutoCAD Objects

Most rules need to read data from the drawing database. The pattern is:

```csharp
var db = AcadContext.GetDatabase(context);

using (var tr = db.TransactionManager.StartTransaction())
{
    var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

    foreach (var btrId in bt)
    {
        var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
        // Process block table records...
    }

    tr.Commit();
}
```

See the existing rules in `src/CivilQc.Rules/` for complete examples of:
- Iterating layers (`EmptyLayersRule.cs`)
- Reading system variables (`DrawingUnitsRule.cs`)
- Checking xrefs (`XrefStatusRule.cs`)
- Scanning entities (`ProxyObjectsRule.cs`)
- Validating names (`BlockNamingRule.cs`, `TextStyleRule.cs`)

## Parameter Helpers

Rules typically include helper methods for reading YAML parameters:

```csharp
private static bool GetBoolParam(RuleDefinition rule, string key, bool defaultValue)
{
    if (!rule.Parameters.TryGetValue(key, out var raw))
        return defaultValue;
    return raw is bool b ? b
         : bool.TryParse(raw.ToString(), out var parsed) ? parsed
         : defaultValue;
}

private static int GetIntParam(RuleDefinition rule, string key, int defaultValue)
{
    if (!rule.Parameters.TryGetValue(key, out var raw))
        return defaultValue;
    return raw is int i ? i
         : int.TryParse(raw.ToString(), out var parsed) ? parsed
         : defaultValue;
}

private static List<string> GetStringListParam(RuleDefinition rule, string key)
{
    if (!rule.Parameters.TryGetValue(key, out var raw))
        return new List<string>();
    if (raw is List<object> list)
        return list.Select(o => o.ToString()!).ToList();
    if (raw is string str)
        return str.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                  .Select(s => s.Trim())
                  .ToList();
    return new List<string>();
}
```
