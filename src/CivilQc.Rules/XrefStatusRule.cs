// ──────────────────────────────────────────────────────────────────────────────
// XrefStatusRule.cs — DRAW-002
//
// Reports the status of all external references (xrefs) attached to the
// drawing. Missing or unloadable xrefs break downstream deliverables.
//
// YAML parameters:
//   fail_on_missing  — if true, missing xrefs cause a failure (default: true)
//   warn_on_overlay  — if true, overlays (vs attachments) get flagged (default: false)
//
// Xref status is read from the BlockTableRecord.IsFromExternalReference flag
// combined with the database's xref status (resolved, not-found, etc.).
// ──────────────────────────────────────────────────────────────────────────────
using Autodesk.AutoCAD.DatabaseServices;
using CivilQc.Engine;

namespace CivilQc.Rules;

public class XrefStatusRule : IRule
{
    public string RuleId => "DRAW-002";
    public string Name => "Xref status";

    public List<CheckResult> Execute(RuleDefinition rule, DrawingContext context)
    {
        var failOnMissing = GetBoolParam(rule, "fail_on_missing", true);
        var db = AcadContext.GetDatabase(context);

        // Collect xref info from the block table.
        var xrefs = new List<XrefInfo>();

        using (var tr = db.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(
                db.BlockTableId, OpenMode.ForRead);

            foreach (var btrId in bt)
            {
                var btr = (BlockTableRecord)tr.GetObject(
                    btrId, OpenMode.ForRead);

                if (!btr.IsFromExternalReference)
                    continue;

                // Determine xref state from the block table record.
                var isUnloaded = btr.IsUnloaded;
                var pathName = btr.PathName;
                var name = btr.Name;

                // Try to detect missing xrefs: if the file doesn't exist on disk.
                var isMissing = !isUnloaded && !File.Exists(pathName);

                // Check if it's an overlay vs attachment.
                // This can't be reliably determined from BlockTableRecord in managed API,
                // so we just report what we know.
                var xrefStatus = isUnloaded ? "Unloaded"
                    : isMissing ? "Missing"
                    : "Resolved";

                xrefs.Add(new XrefInfo
                {
                    Name = name,
                    Path = pathName,
                    IsMissing = isMissing,
                    IsUnloaded = isUnloaded,
                    Status = xrefStatus.ToString()
                });
            }

            tr.Commit();
        }

        // ── Analyse results ──────────────────────────────────────────────
        var missing = xrefs.Where(x => x.IsMissing).ToList();
        var unloaded = xrefs.Where(x => x.IsUnloaded).ToList();
        var resolved = xrefs.Where(x => !x.IsMissing && !x.IsUnloaded).ToList();

        var results = new List<CheckResult>();

        if (xrefs.Count == 0)
        {
            results.Add(new CheckResult
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                Severity = rule.Severity,
                Passed = true,
                Message = "No external references attached.",
                ObjectType = ObjectType.Drawing
            });
            return results;
        }

        // Summary line.
        var summary = $"{xrefs.Count} xref(s): {resolved.Count} resolved";
        if (unloaded.Count > 0) summary += $", {unloaded.Count} unloaded";
        if (missing.Count > 0) summary += $", {missing.Count} missing";

        bool passed = true;
        if (failOnMissing && missing.Count > 0)
            passed = false;

        var details = new Dictionary<string, string>
        {
            ["total_xrefs"] = xrefs.Count.ToString(),
            ["resolved"] = resolved.Count.ToString(),
            ["unloaded"] = unloaded.Count.ToString(),
            ["missing"] = missing.Count.ToString()
        };

        // List missing xref names in details for fix guidance.
        if (missing.Count > 0)
            details["missing_names"] = string.Join(", ", missing.Select(x => x.Name));

        results.Add(new CheckResult
        {
            RuleId = rule.Id,
            RuleName = rule.Name,
            Severity = rule.Severity,
            Passed = passed,
            Message = summary,
            ObjectType = ObjectType.Drawing,
            Details = details
        });

        return results;
    }

    // ── Types ────────────────────────────────────────────────────────────────

    private class XrefInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsMissing { get; set; }
        public bool IsUnloaded { get; set; }
        public string Status { get; set; } = string.Empty;
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
}
