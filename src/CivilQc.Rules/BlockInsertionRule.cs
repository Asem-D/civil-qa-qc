// ──────────────────────────────────────────────────────────────────────────────
// BlockInsertionRule.cs — BLOCK-003
//
// Detects BlockReference entities inserted at or near the origin (0,0,0).
// Origin insertions are usually copy/paste errors or incomplete blocks.
//
// YAML parameters:
//   origin_threshold  — max distance from origin to flag (default: 0.001)
//   skip_annotative   — skip annotative blocks (default: false)
//   skip_xrefs        — skip blocks from external references (default: true)
// ──────────────────────────────────────────────────────────────────────────────
using Autodesk.AutoCAD.DatabaseServices;
using CivilQc.Engine;

namespace CivilQc.Rules;

public class BlockInsertionRule : IRule
{
    public string RuleId => "BLOCK-003";
    public string Name => "Block insertion point";

    public List<CheckResult> Execute(RuleDefinition rule, DrawingContext context)
    {
        var threshold = GetDoubleParam(rule, "origin_threshold", 0.001);
        var skipXrefs = GetBoolParam(rule, "skip_xrefs", true);

        var db = AcadContext.GetDatabase(context);
        var violations = new List<string>();
        int totalBlocks = 0;

        using (var tr = db.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(
                db.BlockTableId, OpenMode.ForRead);

            // Only scan ModelSpace for insertion points.
            var modelSpace = (BlockTableRecord)tr.GetObject(
                bt[BlockTableRecord.ModelSpace], OpenMode.ForRead);

            foreach (var entId in modelSpace)
            {
                try
                {
                    var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);

                    if (ent is not BlockReference blkRef)
                        continue;

                    totalBlocks++;

                    // Optionally skip xref blocks.
                    if (skipXrefs)
                    {
                        var btr = (BlockTableRecord)tr.GetObject(
                            blkRef.BlockTableRecord, OpenMode.ForRead);
                        if (btr.IsFromExternalReference)
                            continue;
                    }

                    var pos = blkRef.Position;
                    var distanceFromOrigin = Math.Sqrt(
                        pos.X * pos.X + pos.Y * pos.Y + pos.Z * pos.Z);

                    if (distanceFromOrigin <= threshold)
                    {
                        var name = "anonymous";
                        try
                        {
                            var btr = (BlockTableRecord)tr.GetObject(
                                blkRef.BlockTableRecord, OpenMode.ForRead);
                            name = btr.Name;
                        }
                        catch { }

                        violations.Add(name);
                    }
                }
                catch
                {
                    // Skip corrupt entities.
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
                Message = $"{violations.Count} block(s) inserted at origin: {sample}{more}",
                ObjectType = ObjectType.BlockReference,
                Details = new Dictionary<string, string>
                {
                    ["violation_count"] = violations.Count.ToString(),
                    ["total_blocks"] = totalBlocks.ToString(),
                    ["origin_threshold"] = threshold.ToString()
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
                Message = $"All {totalBlocks} block(s) have valid insertion points.",
                ObjectType = ObjectType.BlockReference
            });
        }

        return results;
    }

    // ── Parameter helpers ────────────────────────────────────────────────────

    private static double GetDoubleParam(RuleDefinition rule, string key, double defaultValue)
    {
        if (!rule.Parameters.TryGetValue(key, out var raw))
            return defaultValue;
        return raw is double d ? d
             : double.TryParse(raw.ToString(), out var parsed) ? parsed
             : defaultValue;
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
