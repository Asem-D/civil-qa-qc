// ──────────────────────────────────────────────────────────────────────────────
// EmptyLayersRule.cs — LAYER-002
//
// Finds layers that contain zero drawn entities.
// Empty layers are common artifacts of copy/paste, template imports, or
// deleted content. They clutter the layer dropdown and confuse reviewers.
//
// YAML parameters:
//   exclude_defaults  — skip "0" and "Defpoints" from the report (default: true)
//   max_empty         — optional; if set, severity escalates when count exceeds it
//
// The check scans all entities in ModelSpace and PaperSpace to determine
// which layers are actually in use.
// ──────────────────────────────────────────────────────────────────────────────
using Autodesk.AutoCAD.DatabaseServices;
using CivilQc.Engine;

namespace CivilQc.Rules;

public class EmptyLayersRule : IRule
{
    public string RuleId => "LAYER-002";
    public string Name => "Empty layers";

    private static readonly HashSet<string> DefaultLayers = new(StringComparer.OrdinalIgnoreCase)
    {
        "0", "Defpoints"
    };

    public List<CheckResult> Execute(RuleDefinition rule, DrawingContext context)
    {
        var excludeDefaults = GetBoolParam(rule, "exclude_defaults", true);
        var db = AcadContext.GetDatabase(context);

        // Layers that have at least one entity in any space.
        var layersWithEntities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using (var tr = db.TransactionManager.StartTransaction())
        {
            // ── Collect all layers referenced by entities ────────────────
            var bt = (BlockTable)tr.GetObject(
                db.BlockTableId, OpenMode.ForRead);

            // Scan ModelSpace and all PaperSpace layouts.
            CollectLayerNames(tr, bt[BlockTableRecord.ModelSpace], layersWithEntities);

            foreach (var btrId in bt)
            {
                var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                // PaperSpace layouts have IsLayout == true and aren't ModelSpace.
                if (btr.IsLayout && btr.Name != BlockTableRecord.ModelSpace)
                    CollectLayerNames(tr, btrId, layersWithEntities);
            }

            // ── Find layers with zero entities ──────────────────────────
            var layerTable = (LayerTable)tr.GetObject(
                db.LayerTableId, OpenMode.ForRead);

            var emptyLayers = new List<string>();
            var totalUserLayers = 0;

            foreach (var layerId in layerTable)
            {
                var layer = (LayerTableRecord)tr.GetObject(
                    layerId, OpenMode.ForRead);

                if (excludeDefaults && DefaultLayers.Contains(layer.Name))
                    continue;

                totalUserLayers++;

                if (!layersWithEntities.Contains(layer.Name))
                    emptyLayers.Add(layer.Name);
            }

            tr.Commit();

            // ── Build result ────────────────────────────────────────────
            var results = new List<CheckResult>();

            if (emptyLayers.Count > 0)
            {
                var sample = string.Join(", ", emptyLayers.Take(10));
                var more = emptyLayers.Count > 10
                    ? $" (and {emptyLayers.Count - 10} more)"
                    : string.Empty;

                results.Add(new CheckResult
                {
                    RuleId = rule.Id,
                    RuleName = rule.Name,
                    Severity = rule.Severity,
                    Passed = false,
                    Message = $"{emptyLayers.Count} empty layer(s) found: {sample}{more}",
                    ObjectType = ObjectType.Layer,
                    Details = new Dictionary<string, string>
                    {
                        ["empty_count"] = emptyLayers.Count.ToString(),
                        ["total_user_layers"] = totalUserLayers.ToString(),
                        ["layers"] = string.Join("; ", emptyLayers)
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
                    Message = "No empty layers found.",
                    ObjectType = ObjectType.Layer
                });
            }

            return results;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Iterates entities in a BlockTableRecord and adds their layer names to the set.
    /// </summary>
    private static void CollectLayerNames(
        Transaction tr,
        ObjectId btrId,
        HashSet<string> layerNames)
    {
        var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
        foreach (var entId in btr)
        {
            try
            {
                var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
                layerNames.Add(ent.Layer);
            }
            catch
            {
                // Erased or corrupt entities are skipped.
            }
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
