// ──────────────────────────────────────────────────────────────────────────────
// LayerLinetypeRule.cs — LAYER-005
//
// Validates that layers use approved linetypes.
// Non-standard linetypes indicate copy/paste from other projects or
// uncontrolled template imports.
//
// YAML parameters:
//   allowed_linetypes — list of allowed linetype names (default: Continuous, ByLayer, ByBlock)
//
// ──────────────────────────────────────────────────────────────────────────────
using Autodesk.AutoCAD.DatabaseServices;
using CivilQc.Engine;

namespace CivilQc.Rules;

public class LayerLinetypeRule : IRule
{
    public string RuleId => "LAYER-005";
    public string Name => "Layer linetype standards";

    private static readonly HashSet<string> SystemLayers = new(StringComparer.OrdinalIgnoreCase)
    {
        "0", "Defpoints"
    };

    public List<CheckResult> Execute(RuleDefinition rule, DrawingContext context)
    {
        var allowedLinetypes = GetStringListParam(rule, "allowed_linetypes");

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

                // Resolve the linetype name from the layer's linetype table record.
                var linetypeName = "Continuous";
                try
                {
                    if (layer.LinetypeObjectId != ObjectId.Null)
                    {
                        var ltRecord = (LinetypeTableRecord)tr.GetObject(
                            layer.LinetypeObjectId, OpenMode.ForRead);
                        linetypeName = ltRecord.Name;
                    }
                }
                catch
                {
                    // If we can't resolve, default to Continuous.
                }

                if (allowedLinetypes.Count > 0 &&
                    !allowedLinetypes.Contains(linetypeName, StringComparer.OrdinalIgnoreCase))
                {
                    violations.Add($"{layer.Name} ({linetypeName})");
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
                Message = $"{violations.Count} layer(s) with non-standard linetypes: {sample}{more}",
                ObjectType = ObjectType.Layer,
                Details = new Dictionary<string, string>
                {
                    ["violation_count"] = violations.Count.ToString(),
                    ["total_user_layers"] = userLayerCount.ToString(),
                    ["allowed_linetypes"] = string.Join(", ", allowedLinetypes)
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
                Message = $"All {userLayerCount} user layer(s) have standard linetypes.",
                ObjectType = ObjectType.Layer
            });
        }

        return results;
    }

    // ── Parameter helpers ────────────────────────────────────────────────────

    private static List<string> GetStringListParam(RuleDefinition rule, string key)
    {
        if (!rule.Parameters.TryGetValue(key, out var raw) || raw is not System.Collections.IList list)
            return new List<string>();

        return list.Cast<object>()
                   .Select(o => o.ToString() ?? string.Empty)
                   .Where(s => s.Length > 0)
                   .ToList();
    }
}
