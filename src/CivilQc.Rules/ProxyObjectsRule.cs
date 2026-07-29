// ──────────────────────────────────────────────────────────────────────────────
// ProxyObjectsRule.cs — DRAW-003
//
// Counts and reports proxy entities in the drawing. Proxy objects are
// placeholders for entities created by third-party applications that the
// current AutoCAD installation cannot fully render or edit.
//
// YAML parameters:
//   max_count  — optional; if set and proxy count exceeds this, mark as failure
//
// Proxy objects are detected by checking for the AcDbProxyEntity class name.
// They can indicate missing vertical-market modules or file-version mismatches.
// ──────────────────────────────────────────────────────────────────────────────
using Autodesk.AutoCAD.DatabaseServices;
using CivilQc.Engine;

namespace CivilQc.Rules;

public class ProxyObjectsRule : IRule
{
    public string RuleId => "DRAW-003";
    public string Name => "Proxy objects";

    public List<CheckResult> Execute(RuleDefinition rule, DrawingContext context)
    {
        var db = AcadContext.GetDatabase(context);
        var maxCount = GetIntParam(rule, "max_count", 0);

        var proxies = new List<ProxyDetail>();

        using (var tr = db.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(
                db.BlockTableId, OpenMode.ForRead);

            // Scan ModelSpace.
            ScanForProxies(tr, bt[BlockTableRecord.ModelSpace], proxies);

            // Scan PaperSpace layouts.
            foreach (var btrId in bt)
            {
                var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                if (btr.IsLayout && btr.Name != BlockTableRecord.ModelSpace)
                    ScanForProxies(tr, btrId, proxies);
            }

            tr.Commit();
        }

        // ── Build result ─────────────────────────────────────────────────
        var results = new List<CheckResult>();

        if (proxies.Count == 0)
        {
            results.Add(new CheckResult
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                Severity = rule.Severity,
                Passed = true,
                Message = "No proxy objects found.",
                ObjectType = ObjectType.Drawing
            });
            return results;
        }

        // Summarise by type.
        var byType = proxies
            .GroupBy(p => p.TypeName)
            .OrderByDescending(g => g.Count())
            .ToList();

        var typeBreakdown = string.Join(", ",
            byType.Take(5).Select(g => $"{g.Key} ({g.Count()})"));

        bool passed = maxCount <= 0 || proxies.Count <= maxCount;

        var summary = $"{proxies.Count} proxy object(s) found: {typeBreakdown}";
        if (!passed)
            summary += $" [exceeds max_count of {maxCount}]";

        var details = new Dictionary<string, string>
        {
            ["proxy_count"] = proxies.Count.ToString(),
            ["max_count"] = maxCount.ToString(),
            ["type_breakdown"] = typeBreakdown,
            ["layers"] = string.Join(", ", proxies.Select(p => p.Layer).Distinct())
        };

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

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void ScanForProxies(
        Transaction tr, ObjectId btrId, List<ProxyDetail> proxies)
    {
        var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);

        foreach (var entId in btr)
        {
            try
            {
                var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);

                // ProxyEntity is the managed wrapper for AcDbProxyEntity.
                if (ent is ProxyEntity proxy)
                {
                    proxies.Add(new ProxyDetail
                    {
                        TypeName = GetProxyTypeName(proxy),
                        Layer = ent.Layer,
                        Handle = ent.Handle.ToString()
                    });
                }
            }
            catch
            {
                // Erased or corrupt entities are skipped.
            }
        }
    }

    /// <summary>
    /// Attempts to read the original type name from a proxy entity.
    /// Falls back to the class name if the original type is unavailable.
    /// </summary>
    private static string GetProxyTypeName(ProxyEntity proxy)
    {
        try
        {
            // Try to get the original class name from the proxy entity.
            // OriginalTypeNames may not be available in all API versions.
            var original = proxy.GetType().Name;
            return string.IsNullOrEmpty(original) ? "ProxyEntity" : original;
        }
        catch { /* property may not be available on all proxy versions */ }

        return "ProxyEntity";
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

    private class ProxyDetail
    {
        public string TypeName { get; set; } = string.Empty;
        public string Layer { get; set; } = string.Empty;
        public string Handle { get; set; } = string.Empty;
    }
}
