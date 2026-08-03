// ──────────────────────────────────────────────────────────────────────────────
// BlockNamingRule.cs — BLOCK-001
//
// Checks block names against configurable prefix/suffix/naming rules.
// Enforcing block naming conventions prevents collisions across projects,
// makes block libraries searchable, and avoids the infamous "A$XXXX" and
// "*Unnn" anonymous/dynamic block names cluttering the block list.
//
// YAML parameters:
//   allowed_prefixes   — list of allowed name prefixes (e.g. ["PRJ-", "STD-"])
//   forbidden_prefixes — list of forbidden prefixes (default: ["A$", "*U", "*D", "*X"])
//   require_prefix     — if true, every block must start with an allowed prefix (default: false)
//   max_name_length    — optional; flag blocks with names longer than this
//
// Detection:
//   Iterates the BlockTable, skipping ModelSpace, PaperSpace, and xref blocks.
//   Checks each block name against the configured rules.
// ──────────────────────────────────────────────────────────────────────────────
using Autodesk.AutoCAD.DatabaseServices;
using CivilQc.Engine;

namespace CivilQc.Rules;

public class BlockNamingRule : IRule
{
    public string RuleId => "BLOCK-001";
    public string Name => "Block naming conventions";

    // Anonymous and dynamic blocks that always violate naming standards.
    private static readonly string[] DefaultForbiddenPrefixes = { "A$", "*U", "*D", "*X" };

    public List<CheckResult> Execute(RuleDefinition rule, DrawingContext context)
    {
        var db = AcadContext.GetDatabase(context);
        var allowedPrefixes = GetStringListParam(rule, "allowed_prefixes");
        var forbiddenPrefixes = GetStringListParam(rule, "forbidden_prefixes");
        var requirePrefix = GetBoolParam(rule, "require_prefix", false);
        var maxLength = GetIntParam(rule, "max_name_length", 0);

        // Use default forbidden prefixes if none specified.
        if (forbiddenPrefixes.Count == 0)
            forbiddenPrefixes = DefaultForbiddenPrefixes.ToList();

        var violations = new List<BlockViolation>();
        var totalBlocks = 0;

        using (var tr = db.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(
                db.BlockTableId, OpenMode.ForRead);

            foreach (var btrId in bt)
            {
                var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

                // Skip ModelSpace, PaperSpace, and external references.
                if (btr.IsLayout || btr.IsFromExternalReference)
                    continue;

                // Skip anonymous blocks (names starting with "*") unless we're
                // specifically checking for forbidden prefixes.
                // Anonymous blocks are AutoCAD-internal and not user-created.
                if (btr.Name.StartsWith("*"))
                {
                    // Only flag if it matches a forbidden prefix pattern.
                    if (forbiddenPrefixes.Any(fp =>
                        btr.Name.StartsWith(fp, StringComparison.OrdinalIgnoreCase)))
                    {
                        violations.Add(new BlockViolation
                        {
                            BlockName = btr.Name,
                            Issue = "Anonymous/dynamic block (forbidden prefix)",
                            Handle = btr.Handle.ToString()
                        });
                    }
                    continue;
                }

                totalBlocks++;

                // Check forbidden prefixes.
                var matchedForbidden = forbiddenPrefixes.FirstOrDefault(fp =>
                    btr.Name.StartsWith(fp, StringComparison.OrdinalIgnoreCase));

                if (matchedForbidden != null)
                {
                    violations.Add(new BlockViolation
                    {
                        BlockName = btr.Name,
                        Issue = $"Starts with forbidden prefix \"{matchedForbidden}\"",
                        Handle = btr.Handle.ToString()
                    });
                    continue;
                }

                // Check required prefix.
                if (requirePrefix && allowedPrefixes.Count > 0)
                {
                    var hasValidPrefix = allowedPrefixes.Any(ap =>
                        btr.Name.StartsWith(ap, StringComparison.OrdinalIgnoreCase));

                    if (!hasValidPrefix)
                    {
                        violations.Add(new BlockViolation
                        {
                            BlockName = btr.Name,
                            Issue = "Does not start with any allowed prefix",
                            Handle = btr.Handle.ToString()
                        });
                        continue;
                    }
                }

                // Check max name length.
                if (maxLength > 0 && btr.Name.Length > maxLength)
                {
                    violations.Add(new BlockViolation
                    {
                        BlockName = btr.Name,
                        Issue = $"Name length {btr.Name.Length} exceeds max {maxLength}",
                        Handle = btr.Handle.ToString()
                    });
                }
            }

            tr.Commit();
        }

        // Build result.
        var results = new List<CheckResult>();

        if (violations.Count == 0)
        {
            results.Add(new CheckResult
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                Severity = rule.Severity,
                Passed = true,
                Message = $"All {totalBlocks} block name(s) comply with naming conventions.",
                ObjectType = ObjectType.BlockReference,
                Details = new Dictionary<string, string>
                {
                    ["total_blocks"] = totalBlocks.ToString(),
                    ["violation_count"] = "0"
                }
            });
            return results;
        }

        var sample = string.Join(", ",
            violations.Take(5).Select(v => $"\"{v.BlockName}\""));
        var more = violations.Count > 5
            ? $" (and {violations.Count - 5} more)"
            : string.Empty;

        var details = new Dictionary<string, string>
        {
            ["total_blocks"] = totalBlocks.ToString(),
            ["violation_count"] = violations.Count.ToString(),
            ["violating_blocks"] = string.Join("; ",
                violations.Select(v => $"{v.BlockName}: {v.Issue}"))
        };

        results.Add(new CheckResult
        {
            RuleId = rule.Id,
            RuleName = rule.Name,
            Severity = rule.Severity,
            Passed = false,
            Message = $"{violations.Count} block(s) with naming violations: {sample}{more}",
            ObjectType = ObjectType.BlockReference,
            Details = details
        });

        return results;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static List<string> GetStringListParam(RuleDefinition rule, string key)
    {
        if (!rule.Parameters.TryGetValue(key, out var raw))
            return new List<string>();

        if (raw is List<object> list)
            return list.Select(o => o.ToString() ?? string.Empty)
                       .Where(s => !string.IsNullOrEmpty(s))
                       .ToList();

        if (raw is string str)
            return str.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                      .ToList();

        return new List<string>();
    }

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

    // ── Types ────────────────────────────────────────────────────────────────

    private class BlockViolation
    {
        public string BlockName { get; set; } = string.Empty;
        public string Issue { get; set; } = string.Empty;
        public string Handle { get; set; } = string.Empty;
    }
}
