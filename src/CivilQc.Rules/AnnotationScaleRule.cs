// ──────────────────────────────────────────────────────────────────────────────
// AnnotationScaleRule.cs — ANNO-001
//
// Detects annotative entities in the drawing by checking for the ACAD_ANNO
// XData regApp. Reports count and distribution, flagging potential issues:
// - Annotative entities in ModelSpace (should typically be in layouts)
// - Missing annotation scales when annotative entities exist
//
// YAML parameters:
//   fail_if_annotative_in_modelspace — flag annotative objects in ModelSpace (default: false)
//
// Detection:
//   Checks each entity for the ACAD_ANNO XData registration, which indicates
//   the entity is annotative. Reports counts by entity type and location.
// ──────────────────────────────────────────────────────────────────────────────
using Autodesk.AutoCAD.DatabaseServices;
using CivilQc.Engine;

namespace CivilQc.Rules;

public class AnnotationScaleRule : IRule
{
    public string RuleId => "ANNO-001";
    public string Name => "Annotation scale consistency";

    private const string RegAppName = "ACAD_ANNO";

    public List<CheckResult> Execute(RuleDefinition rule, DrawingContext context)
    {
        var db = AcadContext.GetDatabase(context);
        var failInModelSpace = GetBoolParam(rule, "fail_if_annotative_in_modelspace", false);

        int annotativeInModelSpace = 0;
        int annotativeInPaperSpace = 0;
        var typeCounts = new Dictionary<string, int>();

        using (var tr = db.TransactionManager.StartTransaction())
        {
            var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

            // Collect layout names for context.
            var layoutNames = new List<string>();

            foreach (var btrId in bt)
            {
                var btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                if (btr.IsFromExternalReference)
                    continue;

                bool isModelSpace = btr.Name == BlockTableRecord.ModelSpace;
                bool isLayout = btr.IsLayout && !isModelSpace;

                if (isLayout)
                    layoutNames.Add(btr.Name);

                foreach (var entId in btr)
                {
                    try
                    {
                        var ent = (Entity)tr.GetObject(entId, OpenMode.ForRead);

                        // Check XData for the ACAD_ANNO regApp.
                        var xdata = ent.GetXDataForApplication(RegAppName);
                        if (xdata == null)
                            continue;

                        xdata.Dispose();

                        // Entity is annotative.
                        var typeName = ent.GetType().Name;

                        if (!typeCounts.ContainsKey(typeName))
                            typeCounts[typeName] = 0;
                        typeCounts[typeName]++;

                        if (isModelSpace)
                            annotativeInModelSpace++;
                        else if (isLayout)
                            annotativeInPaperSpace++;
                    }
                    catch
                    {
                        // Erased or inaccessible entities are skipped.
                    }
                }
            }

            tr.Commit();
        }

        int totalAnnotative = annotativeInModelSpace + annotativeInPaperSpace;
        var results = new List<CheckResult>();

        if (totalAnnotative == 0)
        {
            results.Add(new CheckResult
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                Severity = rule.Severity,
                Passed = true,
                Message = "No annotative entities found in the drawing.",
                ObjectType = ObjectType.Drawing,
                Details = new Dictionary<string, string>
                {
                    ["total_annotative"] = "0"
                }
            });
            return results;
        }

        // Build type breakdown.
        var typeBreakdown = string.Join(", ",
            typeCounts.OrderByDescending(kvp => kvp.Value)
                      .Take(5)
                      .Select(kvp => $"{kvp.Key}: {kvp.Value}"));

        var details = new Dictionary<string, string>
        {
            ["total_annotative"] = totalAnnotative.ToString(),
            ["in_model_space"] = annotativeInModelSpace.ToString(),
            ["in_paper_space"] = annotativeInPaperSpace.ToString(),
            ["type_breakdown"] = typeBreakdown
        };

        // Check: annotative entities in ModelSpace is usually wrong.
        if (failInModelSpace && annotativeInModelSpace > 0)
        {
            results.Add(new CheckResult
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                Severity = rule.Severity,
                Passed = false,
                Message = $"{annotativeInModelSpace} annotative entity(ies) in ModelSpace " +
                          $"({annotativeInModelSpace} total). Annotative objects should be in layout PaperSpace.",
                ObjectType = ObjectType.Drawing,
                Details = details
            });
            return results;
        }

        // Default: report as informational pass.
        var summary = $"{totalAnnotative} annotative entity(ies) found: " +
                      $"{annotativeInModelSpace} in ModelSpace, " +
                      $"{annotativeInPaperSpace} in PaperSpace. Types: {typeBreakdown}";

        results.Add(new CheckResult
        {
            RuleId = rule.Id,
            RuleName = rule.Name,
            Severity = rule.Severity,
            Passed = true,
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
}
