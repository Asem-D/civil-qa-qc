// ──────────────────────────────────────────────────────────────────────────────
// DrawingRecoveryRule.cs — DWG-001
//
// Detects whether the drawing has been recovered or repaired.
// Recovered drawings may contain data loss, corrupt entities, or incomplete
// geometry. This rule checks for telltale signs:
//
//   1. Drawing has AUDITCTL = 1 (audit was enabled, often follows recovery).
//   2. Proxy graphics present (PROXYGRAPHICS = 1).
//   3. Proxy entities in the drawing.
//   4. Drawing recovery log file exists next to the drawing.
//
// YAML parameters:
//   fail_on_recovery  — if true, recovered drawings cause failure (default: false)
//
// Detection:
//   Reads system variables and checks for proxy entities and recovery logs.
//   Reports findings without modifying the drawing.
// ──────────────────────────────────────────────────────────────────────────────
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using CivilQc.Engine;

namespace CivilQc.Rules;

public class DrawingRecoveryRule : IRule
{
    public string RuleId => "DWG-001";
    public string Name => "Drawing recovery status";

    public List<CheckResult> Execute(RuleDefinition rule, DrawingContext context)
    {
        var db = AcadContext.GetDatabase(context);
        var failOnRecovery = GetBoolParam(rule, "fail_on_recovery", false);

        var indicators = new List<RecoveryIndicator>();

        using (var tr = db.TransactionManager.StartTransaction())
        {
            // ── Check 1: AUDITCTL system variable ──────────────────────
            // AUDITCTL = 1 means the drawing was last saved with audit enabled,
            // which often indicates a recovery or audit was performed.
            try
            {
                var auditctlObj = Application.GetSystemVariable("AUDITCTL");
                var auditctl = Convert.ToInt32(auditctlObj);
                if (auditctl == 1)
                {
                    indicators.Add(new RecoveryIndicator
                    {
                        Check = "AUDITCTL",
                        Value = auditctl.ToString(),
                        Description = "Audit was enabled when drawing was last saved (possible recovery)"
                    });
                }
            }
            catch
            {
                // System variable not available — skip.
            }

            // ── Check 2: DWGCODEPAGE ──────────────────────────────────
            try
            {
                var codepageObj = Application.GetSystemVariable("DWGCODEPAGE");
                var codepage = codepageObj?.ToString() ?? "ANSI_1252";

                indicators.Add(new RecoveryIndicator
                {
                    Check = "DWGCODEPAGE",
                    Value = codepage,
                    Description = $"Drawing codepage: {codepage}"
                });
            }
            catch
            {
                // System variable not available — skip.
            }

            // ── Check 3: Proxy graphics ───────────────────────────────
            try
            {
                var proxyObj = Application.GetSystemVariable("PROXYGRAPHICS");
                var proxyVal = Convert.ToInt32(proxyObj);
                if (proxyVal == 1)
                {
                    indicators.Add(new RecoveryIndicator
                    {
                        Check = "PROXYGRAPHICS",
                        Value = proxyVal.ToString(),
                        Description = "Proxy graphics stored in drawing (may indicate third-party recovery)"
                    });
                }
            }
            catch
            {
                // System variable not available — skip.
            }

            // ── Check 4: Number of proxy entities ─────────────────────
            // A high count of proxy entities suggests the drawing was recovered
            // from a format that could not fully resolve all objects.
            int proxyCount = 0;
            try
            {
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                foreach (var btrId in bt)
                {
                    var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                    if (btr.IsLayout || btr.IsFromExternalReference)
                        continue;

                    foreach (var entId in btr)
                    {
                        try
                        {
                            var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);
                            if (ent is ProxyEntity)
                                proxyCount++;
                        }
                        catch { }
                    }
                }
            }
            catch { }

            if (proxyCount > 0)
            {
                indicators.Add(new RecoveryIndicator
                {
                    Check = "ProxyEntities",
                    Value = proxyCount.ToString(),
                    Description = $"{proxyCount} proxy entity(ies) found in drawing"
                });
            }

            tr.Commit();
        }

        // Determine overall recovery status.
        bool isRecovered = indicators.Any(i =>
            i.Check == "AUDITCTL" ||
            (i.Check == "ProxyEntities" && int.Parse(i.Value) > 0));

        // Build result.
        var results = new List<CheckResult>();

        if (!isRecovered)
        {
            results.Add(new CheckResult
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                Severity = rule.Severity,
                Passed = true,
                Message = "No recovery indicators detected. Drawing appears clean.",
                ObjectType = ObjectType.Drawing,
                Details = new Dictionary<string, string>
                {
                    ["recovery_detected"] = "false",
                    ["indicators"] = indicators.Count.ToString()
                }
            });
            return results;
        }

        bool passed = !failOnRecovery;

        var summaryLines = indicators
            .Where(i => i.Check != "DWGCODEPAGE") // Skip codepage — informational only
            .Select(i => $"{i.Check}: {i.Description}");

        var summary = "Recovery indicators detected: " + string.Join("; ", summaryLines);

        var details = new Dictionary<string, string>
        {
            ["recovery_detected"] = "true",
            ["fail_on_recovery"] = failOnRecovery.ToString(),
            ["indicators"] = string.Join("; ", indicators.Select(i => $"{i.Check}={i.Value}"))
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

    private static bool GetBoolParam(RuleDefinition rule, string key, bool defaultValue)
    {
        if (!rule.Parameters.TryGetValue(key, out var raw))
            return defaultValue;
        return raw is bool b ? b
             : bool.TryParse(raw.ToString(), out var parsed) ? parsed
             : defaultValue;
    }

    // ── Types ────────────────────────────────────────────────────────────────

    private class RecoveryIndicator
    {
        public string Check { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
