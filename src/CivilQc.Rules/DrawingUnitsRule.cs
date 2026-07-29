// ──────────────────────────────────────────────────────────────────────────────
// DrawingUnitsRule.cs — DRAW-001
//
// Reads the INSUNITS system variable and reports the drawing's unit setting.
// Mismatched units are a top cause of scale errors in Civil 3D deliverables.
//
// YAML parameters:
//   expected   — required unit name (e.g. "Meters", "Millimeters", "Feet")
//                If set and the drawing units don't match, severity escalates
//                to the rule's configured level.
//
// Supported unit names (case-insensitive):
//   Unitless, Inches, Feet, Miles, Millimeters, Centimeters, Meters,
//   Kilometers, Microinches, Mils, Yards, Angstroms, Nanometers, Microns,
//   Decimeters, Dectameters, Hectometers, Gigameters, AU, LightYears,
//   Parsecs, SurveyorsFeet
// ──────────────────────────────────────────────────────────────────────────────
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using CivilQc.Engine;

namespace CivilQc.Rules;

public class DrawingUnitsRule : IRule
{
    public string RuleId => "DRAW-001";
    public string Name => "Drawing units";

    public List<CheckResult> Execute(RuleDefinition rule, DrawingContext context)
    {
        var expectedUnit = GetStringParam(rule, "expected", string.Empty);

        string unitName;
        int unitValue;

        // Read INSUNITS via Application.GetSystemVariable (works across all AutoCAD versions).
        var insUnitsObj = Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("INSUNITS");
        unitValue = Convert.ToInt32(insUnitsObj);
        unitName = insUnitsObj?.ToString() ?? "Unknown";

        var results = new List<CheckResult>();

        // If an expected unit was specified, compare against it.
        if (!string.IsNullOrEmpty(expectedUnit))
        {
            var matches = string.Equals(
                unitName, expectedUnit, StringComparison.OrdinalIgnoreCase);

            results.Add(new CheckResult
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                Severity = rule.Severity,
                Passed = matches,
                Message = matches
                    ? $"Drawing units are {unitName} (as expected)."
                    : $"Drawing units are {unitName}, expected {expectedUnit}.",
                ObjectType = ObjectType.Drawing,
                Details = new Dictionary<string, string>
                {
                    ["units"] = unitName,
                    ["units_value"] = unitValue.ToString(),
                    ["expected"] = expectedUnit
                }
            });
        }
        else
        {
            // No expected value set — just report, always pass.
            results.Add(new CheckResult
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                Severity = rule.Severity,
                Passed = true,
                Message = $"Drawing units: {unitName} (INSUNITS = {unitValue}).",
                ObjectType = ObjectType.Drawing,
                Details = new Dictionary<string, string>
                {
                    ["units"] = unitName,
                    ["units_value"] = unitValue.ToString()
                }
            });
        }

        return results;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string GetStringParam(RuleDefinition rule, string key, string defaultValue)
    {
        if (rule.Parameters.TryGetValue(key, out var raw))
            return raw.ToString() ?? defaultValue;
        return defaultValue;
    }
}
