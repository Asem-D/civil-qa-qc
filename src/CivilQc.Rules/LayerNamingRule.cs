// ──────────────────────────────────────────────────────────────────────────────
// LayerNamingRule.cs — LAYER-001
//
// Validates that every user-created layer starts with an allowed prefix.
//
// YAML parameters:
//   allowed_prefixes  — list of prefixes to accept (case-insensitive)
//   separator         — delimiter between prefix and suffix (default: "-")
//   require_prefix    — if true, layers without a prefix are violations (default: true)
//
// System layers "0" and "Defpoints" are always skipped.
//
// Example YAML:
//   - id: LAYER-001
//     name: Layer naming convention
//     check_type: LayerNaming
//     severity: Warning
//     parameters:
//       allowed_prefixes: [CIVIL, SURF, ROAD, PIPE, ALGN, CORR, SITE, UTIL]
//       require_prefix: true
//       separator: "-"
// ──────────────────────────────────────────────────────────────────────────────
using Autodesk.AutoCAD.DatabaseServices;
using CivilQc.Engine;

namespace CivilQc.Rules;

public class LayerNamingRule : IRule
{
    public string RuleId => "LAYER-001";
    public string Name => "Layer naming convention";

    // System layers that are always exempt from naming checks.
    private static readonly HashSet<string> SystemLayers = new(StringComparer.OrdinalIgnoreCase)
    {
        "0", "Defpoints"
    };

    public List<CheckResult> Execute(RuleDefinition rule, DrawingContext context)
    {
        // ── Parameters ───────────────────────────────────────────────────────
        var allowedPrefixes = GetStringListParam(rule, "allowed_prefixes");
        var separator = GetStringParam(rule, "separator", "-");
        var requirePrefix = GetBoolParam(rule, "require_prefix", true);

        // ── Query layers ─────────────────────────────────────────────────────
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

                // Extract the prefix: everything before the first separator.
                var sepIdx = layer.Name.IndexOf(separator, StringComparison.Ordinal);
                var prefix = sepIdx > 0 ? layer.Name[..sepIdx] : string.Empty;

                if (requirePrefix &&
                    (string.IsNullOrEmpty(prefix) ||
                     !allowedPrefixes.Contains(prefix, StringComparer.OrdinalIgnoreCase)))
                {
                    violations.Add(layer.Name);
                }
            }

            tr.Commit();
        }

        // ── Build result ─────────────────────────────────────────────────────
        var results = new List<CheckResult>();

        if (violations.Count > 0)
        {
            // Show up to 10 sample names so the message stays readable.
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
                Message = $"{violations.Count} layer(s) with non-standard names: {sample}{more}",
                ObjectType = ObjectType.Layer,
                Details = new Dictionary<string, string>
                {
                    ["violation_count"] = violations.Count.ToString(),
                    ["total_user_layers"] = userLayerCount.ToString(),
                    ["allowed_prefixes"] = string.Join(", ", allowedPrefixes)
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
                Message = $"All {userLayerCount} user layer(s) follow the naming convention.",
                ObjectType = ObjectType.Layer
            });
        }

        return results;
    }

    // ── Parameter helpers ────────────────────────────────────────────────────
    // YamlDotNet deserializes YAML lists as List<object>, so we need conversion.

    private static List<string> GetStringListParam(RuleDefinition rule, string key)
    {
        if (!rule.Parameters.TryGetValue(key, out var raw) || raw is not System.Collections.IList list)
            return new List<string>();

        return list.Cast<object>()
                   .Select(o => o.ToString() ?? string.Empty)
                   .Where(s => s.Length > 0)
                   .ToList();
    }

    private static string GetStringParam(RuleDefinition rule, string key, string defaultValue)
    {
        if (rule.Parameters.TryGetValue(key, out var raw))
            return raw.ToString() ?? defaultValue;
        return defaultValue;
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
