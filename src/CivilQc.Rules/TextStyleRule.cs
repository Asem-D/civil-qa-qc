// ──────────────────────────────────────────────────────────────────────────────
// TextStyleRule.cs — ANNO-002
//
// Validates text styles against a configurable list of allowed styles.
// Organisations typically enforce standard text styles (font, height, width
// factor) for consistency across deliverables. Non-standard styles indicate
// copy/paste from other projects or uncontrolled template imports.
//
// YAML parameters:
//   allowed_styles  — list of allowed text style names (e.g. ["Standard", "Arial"])
//   check_fonts     — if true, also validates the font file for each style (default: false)
//   allowed_fonts   — list of allowed font files (e.g. ["romans.shx", "Arial.ttf"])
//
// Detection:
//   Iterates the TextStyleTable and checks each style name against the allowed list.
//   Reports non-compliant styles with their font information.
// ──────────────────────────────────────────────────────────────────────────────
using Autodesk.AutoCAD.DatabaseServices;
using CivilQc.Engine;

namespace CivilQc.Rules;

public class TextStyleRule : IRule
{
    public string RuleId => "ANNO-002";
    public string Name => "Text style standards";

    public List<CheckResult> Execute(RuleDefinition rule, DrawingContext context)
    {
        var db = AcadContext.GetDatabase(context);
        var allowedStyles = GetStringListParam(rule, "allowed_styles");
        var checkFonts = GetBoolParam(rule, "check_fonts", false);
        var allowedFonts = GetStringListParam(rule, "allowed_fonts");

        var violations = new List<StyleViolation>();
        var totalStyles = 0;

        using (var tr = db.TransactionManager.StartTransaction())
        {
            var styleTable = (TextStyleTable)tr.GetObject(
                db.TextStyleTableId, OpenMode.ForRead);

            foreach (var styleId in styleTable)
            {
                var style = (TextStyleTableRecord)tr.GetObject(
                    styleId, OpenMode.ForRead);

                totalStyles++;

                // Check style name against allowed list.
                bool nameAllowed = allowedStyles.Count == 0 ||
                    allowedStyles.Contains(style.Name, StringComparer.OrdinalIgnoreCase);

                // Check font against allowed list.
                bool fontAllowed = true;
                if (checkFonts && allowedFonts.Count > 0)
                {
                    var fontFile = style.FileName;
                    fontAllowed = allowedFonts.Contains(fontFile, StringComparer.OrdinalIgnoreCase);
                }

                if (!nameAllowed || !fontAllowed)
                {
                    var issues = new List<string>();
                    if (!nameAllowed) issues.Add($"name \"{style.Name}\" not in allowed list");
                    if (!fontAllowed) issues.Add($"font \"{style.FileName}\" not in allowed list");

                    violations.Add(new StyleViolation
                    {
                        StyleName = style.Name,
                        FontFile = style.FileName,
                        Height = style.TextSize,
                        Issue = string.Join("; ", issues)
                    });
                }
            }

            tr.Commit();
        }

        // Build result.
        var results = new List<CheckResult>();

        if (violations.Count == 0)
        {
            var message = allowedStyles.Count > 0
                ? $"All {totalStyles} text style(s) comply with allowed list."
                : $"Found {totalStyles} text style(s). No allowed_styles filter configured.";

            results.Add(new CheckResult
            {
                RuleId = rule.Id,
                RuleName = rule.Name,
                Severity = rule.Severity,
                Passed = true,
                Message = message,
                ObjectType = ObjectType.Drawing,
                Details = new Dictionary<string, string>
                {
                    ["total_styles"] = totalStyles.ToString(),
                    ["violation_count"] = "0"
                }
            });
            return results;
        }

        var sample = string.Join(", ",
            violations.Take(5).Select(v => $"\"{v.StyleName}\""));
        var more = violations.Count > 5
            ? $" (and {violations.Count - 5} more)"
            : string.Empty;

        var details = new Dictionary<string, string>
        {
            ["total_styles"] = totalStyles.ToString(),
            ["violation_count"] = violations.Count.ToString(),
            ["violating_styles"] = string.Join("; ",
                violations.Select(v => $"{v.StyleName} ({v.Issue})"))
        };

        results.Add(new CheckResult
        {
            RuleId = rule.Id,
            RuleName = rule.Name,
            Severity = rule.Severity,
            Passed = false,
            Message = $"{violations.Count} non-compliant text style(s): {sample}{more}",
            ObjectType = ObjectType.Drawing,
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

    // ── Types ────────────────────────────────────────────────────────────────

    private class StyleViolation
    {
        public string StyleName { get; set; } = string.Empty;
        public string FontFile { get; set; } = string.Empty;
        public double Height { get; set; }
        public string Issue { get; set; } = string.Empty;
    }
}
