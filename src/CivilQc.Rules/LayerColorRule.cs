// ──────────────────────────────────────────────────────────────────────────────
// LayerColorRule.cs — LAYER-004
//
// Validates that layers use approved ACI (AutoCAD Color Index) values.
// Non-standard colors indicate copy/paste from other projects or
// uncontrolled template imports.
//
// YAML parameters:
//   allowed_colors      — list of allowed ACI color indices (default: all)
//   forbid_bylayer_zero — if true, flag layers using color 0 (default: false)
//
// ACI color indices range from 1 (red) to 255, with 0 meaning "ByBlock"
// and 256 meaning "ByLayer". The "ByLayer" default (256) is always allowed.
// ──────────────────────────────────────────────────────────────────────────────
using Autodesk.AutoCAD.DatabaseServices;
using CivilQc.Engine;

namespace CivilQc.Rules;

public class LayerColorRule : IRule
{
    public string RuleId => "LAYER-004";
    public string Name => "Layer color standards";

    private static readonly HashSet<string> SystemLayers = new(StringComparer.OrdinalIgnoreCase)
    {
        "0", "Defpoints"
    };

    public List<CheckResult> Execute(RuleDefinition rule, DrawingContext context)
    {
        var allowedColors = GetIntListParam(rule, "allowed_colors");
        var forbidByBlock = GetBoolParam(rule, "forbid_bylayer_zero", false);

        var db = AcadContext.GetDatabase(context);
        var violations = new List<string>();
        int userLayerCount = 0;

        using (var tr = db.TransactionManager.StartTransaction())
        {
            var layerTable = (LayerTable)tr.GetObject(
                db.LayerTableId, OpenMode.ForRead);

            foreach (var layerId in layerTable)
            {
                var layer = (LayerTableRecord)tr.GetObject(
                    layerId, OpenMode.ForRead);

                if (SystemLayers.Contains(layer.Name))
                    continue;

                userLayerCount++;
                var colorIndex = layer.Color.ColorIndex;

                // 256 = ByLayer (always OK), 0 = ByBlock (check flag)
                if (colorIndex == 256)
                    continue;

                if (colorIndex == 0 && !forbidByBlock)
                    continue;

                // If an allowed list is specified, check against it.
                if (allowedColors.Count > 0 && !allowedColors.Contains(colorIndex))
                {
                    violations.Add($"{layer.Name} (color {colorIndex})");
                }
                // If no list, only flag ByBlock if forbidden.
                else if (allowedColors.Count == 0 && colorIndex == 0 && forbidByBlock)
                {
                    violations.Add($"{layer.Name} (ByBlock)");
                }
            }

            tr.Commit();
        }

        var results = new List<CheckResult>();

        if (violations.Count > 0)
        {
            var sample = string.Join(", ", violations.Take(10));
            var more = violations.Count > 10
                ? $" (and {violations.Count - 10} more)"
                : string.Empty;

            results.Add(new CheckResult
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                Severity = rule.Severity,
                Passed = false,
                Message = $"{violations.Count} layer(s) with non-standard colors: {sample}{more}",
                ObjectType = ObjectType.Layer,
                Details = new Dictionary<string, string>
                {
                    ["violation_count"] = violations.Count.ToString(),
                    ["total_user_layers"] = userLayerCount.ToString(),
                    ["allowed_colors"] = allowedColors.Count > 0
                        ? string.Join(", ", allowedColors)
                        : "any"
                }
            });
        }
        else
        {
            results.Add(new CheckResult
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                Severity = rule.Severity,
                Passed = true,
                Message = $"All {userLayerCount} user layer(s) have standard colors.",
                ObjectType = ObjectType.Layer
            });
        }

        return results;
    }

    // ── Parameter helpers ────────────────────────────────────────────────────

    private static List<int> GetIntListParam(RuleDefinition rule, string key)
    {
        if (!rule.Parameters.TryGetValue(key, out var raw) || raw is not System.Collections.IList list)
            return new List<int>();

        return list.Cast<object>()
                   .Select(o => int.TryParse(o.ToString(), out var val) ? val : -1)
                   .Where(v => v >= 0)
                   .ToList();
    }

    private static bool GetBoolParam(RuleDefinition rule, string key, bool defaultValue)
    {
        if (!rule.Parameters.TryGetValue(key, out var raw))
            return defaultValue;

        return raw is bool b ? b
             : bool.TryParse(raw.ToString(), out var parsed) ? parsed
             : defaultValue;
    }
}
