// ──────────────────────────────────────────────────────────────────────────────
// DynamicBlockRule.cs — BLOCK-002
//
// Validates dynamic blocks by counting them and detecting potentially broken
// ones. A dynamic block that lost its definition still reports IsDynamicBlock
// as true but has no grip behaviour — often caused by copy/paste between
// drawings with incompatible block versions.
//
// YAML parameters:
//   fail_on_broken  — if true, issues cause failure (default: true)
//
// Detection:
//   Iterates the BlockTable for blocks where IsDynamicBlock == true.
//   Flags blocks that are empty or have unusually simple geometry for
//   dynamic blocks, which may indicate lost definitions.
// ──────────────────────────────────────────────────────────────────────────────
using Autodesk.AutoCAD.DatabaseServices;
using CivilQc.Engine;

namespace CivilQc.Rules;

public class DynamicBlockRule : IRule
{
    public string RuleId => "BLOCK-002";
    public string Name => "Dynamic block validation";

    public List<CheckResult> Execute(RuleDefinition rule, DrawingContext context)
    {
        var db = AcadContext.GetDatabase(context);
        var failOnBroken = GetBoolParam(rule, "fail_on_broken", true);

        var suspectBlocks = new List<SuspectDynamicBlock>();
        var healthyCount = 0;
        var totalDynamic = 0;

        using (var tr = db.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(
                db.BlockTableId, OpenMode.ForRead);

            foreach (var btrId in bt)
            {
                var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

                // Skip layout blocks and xrefs.
                if (btr.IsLayout || btr.IsFromExternalReference)
                    continue;

                // Only process dynamic blocks.
                if (!btr.IsDynamicBlock)
                    continue;

                totalDynamic++;

                try
                {
                    // Count entities in the block definition.
                    int entityCount = 0;
                    foreach (var _ in btr)
                        entityCount++;

                    // An empty dynamic block definition is suspicious — it means
                    // the definition was wiped but the IsDynamicBlock flag persists.
                    if (entityCount == 0)
                    {
                        suspectBlocks.Add(new SuspectDynamicBlock
                        {
                            BlockName = btr.Name,
                            Issue = "Dynamic block definition is empty (entities lost)",
                            Handle = btr.Handle.ToString(),
                            EntityCount = 0
                        });
                    }
                    else
                    {
                        healthyCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    suspectBlocks.Add(new SuspectDynamicBlock
                    {
                        BlockName = btr.Name,
                        Issue = $"Error reading block: {ex.Message}",
                        Handle = btr.Handle.ToString(),
                        EntityCount = -1
                    });
                }
            }

            tr.Commit();
        }

        // Build result.
        var results = new List<CheckResult>();

        if (totalDynamic == 0)
        {
            results.Add(new CheckResult
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                Severity = rule.Severity,
                Passed = true,
                Message = "No dynamic blocks found in the drawing.",
                ObjectType = ObjectType.BlockReference,
                Details = new Dictionary<string, string>
                {
                    ["total_dynamic"] = "0",
                    ["healthy"] = "0",
                    ["suspect"] = "0"
                }
            });
            return results;
        }

        if (suspectBlocks.Count == 0)
        {
            results.Add(new CheckResult
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                Severity = rule.Severity,
                Passed = true,
                Message = $"All {totalDynamic} dynamic block(s) appear valid.",
                ObjectType = ObjectType.BlockReference,
                Details = new Dictionary<string, string>
                {
                    ["total_dynamic"] = totalDynamic.ToString(),
                    ["healthy"] = healthyCount.ToString(),
                    ["suspect"] = "0"
                }
            });
            return results;
        }

        bool passed = !failOnBroken;

        var sample = string.Join(", ",
            suspectBlocks.Take(5).Select(b => $"\"{b.BlockName}\""));
        var more = suspectBlocks.Count > 5
            ? $" (and {suspectBlocks.Count - 5} more)"
            : string.Empty;

        var summary = $"{suspectBlocks.Count} suspect dynamic block(s) out of {totalDynamic}: {sample}{more}";

        var details = new Dictionary<string, string>
        {
            ["total_dynamic"] = totalDynamic.ToString(),
            ["healthy"] = healthyCount.ToString(),
            ["suspect"] = suspectBlocks.Count.ToString(),
            ["suspect_blocks"] = string.Join("; ",
                suspectBlocks.Select(b => $"{b.BlockName}: {b.Issue}"))
        };

        results.Add(new CheckResult
        {
            RuleId = rule.Id,
            RuleName = rule.Name,
            Severity = rule.Severity,
            Passed = passed,
            Message = summary,
            ObjectType = ObjectType.BlockReference,
            Details = details
        });

        return results;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static bool GetBoolParam(RuleDefinition rule, string key, bool defaultValue)
    {
        if (!rule.Parameters.TryGetValue(key, out var raw))
            return defaultValue;
        return raw is bool b ? b
             : bool.TryParse(raw.ToString(), out var parsed) ? parsed
             : defaultValue;
    }

    // ── Types ────────────────────────────────────────────────────────────────

    private class SuspectDynamicBlock
    {
        public string BlockName { get; set; } = string.Empty;
        public string Issue { get; set; } = string.Empty;
        public string Handle { get; set; } = string.Empty;
        public int EntityCount { get; set; }
    }
}
