// ──────────────────────────────────────────────────────────────────────────────
// UnusedLayersRule.cs — LAYER-003
//
// Identifies layers that are frozen or off AND have zero entities.
// These layers are dead weight: they are explicitly disabled and contain
// nothing. Cleaning them up reduces file bloat and layer-list clutter.
//
// YAML parameters:
//   exclude_defaults  — skip "0" and "Defpoints" (default: true)
//
// Severity is set to Info because frozen/off empty layers are usually
// harmless, just untidy.
// ──────────────────────────────────────────────────────────────────────────────
using Autodesk.AutoCAD.DatabaseServices;
using CivilQc.Engine;

namespace CivilQc.Rules;

public class UnusedLayersRule : IRule
{
    public string RuleId => "LAYER-003";
    public string Name => "Unused layers";

    private static readonly HashSet<string> DefaultLayers = new(StringComparer.OrdinalIgnoreCase)
    {
        "0", "Defpoints"
    };

    public List<CheckResult> Execute(RuleDefinition rule, DrawingContext context)
    {
        var excludeDefaults = GetBoolParam(rule, "exclude_defaults", true);
        var db = AcadContext.GetDatabase(context);

        // First pass: collect layers referenced by entities.
        var layersWithEntities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using (var tr = db.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(
                db.BlockTableId, OpenMode.ForRead);

            // Scan ModelSpace.
            CollectLayerNames(tr, bt[BlockTableRecord.ModelSpace], layersWithEntities);

            // Scan all PaperSpace layouts.
            foreach (var btrId in bt)
            {
                var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                if (btr.IsLayout && btr.Name != BlockTableRecord.ModelSpace)
                    CollectLayerNames(tr, btrId, layersWithEntities);
            }

            // Second pass: find frozen/off layers with zero entities.
            var layerTable = (LayerTable)tr.GetObject(
                db.LayerTableId, OpenMode.ForRead);

            var unusedLayers = new List<string>();
            var totalUserLayers = 0;

            foreach (var layerId in layerTable)
            {
                var layer = (LayerTableRecord)tr.GetObject(
                    layerId, OpenMode.ForRead);

                if (excludeDefaults && DefaultLayers.Contains(layer.Name))
                    continue;

                totalUserLayers++;
                bool isDisabled = layer.IsFrozen || layer.IsOff;
                bool isEmpty = !layersWithEntities.Contains(layer.Name);

                if (isDisabled && isEmpty)
                    unusedLayers.Add(
                        $"{layer.Name} ({(layer.IsFrozen ? "frozen" : "off")})");
            }

            tr.Commit();

            // ── Result ──────────────────────────────────────────────────
            var results = new List<CheckResult>();

            if (unusedLayers.Count > 0)
            {
                var sample = string.Join(", ", unusedLayers.Take(10));
                var more = unusedLayers.Count > 10
                    ? $" (and {unusedLayers.Count - 10} more)"
                    : string.Empty;

                results.Add(new CheckResult
                {
                    RuleId = rule.Id,
                    RuleName = rule.Name,
                    Severity = rule.Severity,
                    Passed = false,
                    Message = $"{unusedLayers.Count} frozen/off empty layer(s): {sample}{more}",
                    ObjectType = ObjectType.Layer,
                    Details = new Dictionary<string, string>
                    {
                        ["unused_count"] = unusedLayers.Count.ToString(),
                        ["total_user_layers"] = totalUserLayers.ToString(),
                        ["layers"] = string.Join("; ", unusedLayers)
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
                    Message = "No frozen/off empty layers found.",
                    ObjectType = ObjectType.Layer
                });
            }

            return results;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void CollectLayerNames(
        Transaction tr, ObjectId btrId, HashSet<string> layerNames)
    {
        var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
        foreach (var entId in btr)
        {
            try
            {
                var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
                layerNames.Add(ent.Layer);
            }
            catch { /* erased or corrupt */ }
        }
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
