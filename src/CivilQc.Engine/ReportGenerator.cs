using System.Net;
using System.Text;
using System.Text.Json;

namespace CivilQc.Engine;

public static class ReportGenerator
{
    public static void GenerateHtml(ReportData report, string outputPath)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang='en'>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset='UTF-8'>");
        html.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        html.AppendLine($"<title>Civil QC Report - {Path.GetFileName(report.DrawingPath)}</title>");
        html.AppendLine(@"<style>
:root {
  --color-bg: #1a1a2e;
  --color-surface: #16213e;
  --color-border: #0f3460;
  --color-text: #e0e0e0;
  --color-text-muted: #a0a0b0;
  --color-critical: #ff4757;
  --color-error: #ff6b35;
  --color-warning: #ffa502;
  --color-info: #1e90ff;
  --color-pass: #2ed573;
}
* { margin: 0; padding: 0; box-sizing: border-box; }
body { font-family: 'Segoe UI', system-ui, sans-serif; background: var(--color-bg); color: var(--color-text); line-height: 1.5; }
.container { max-width: 1200px; margin: 0 auto; padding: 2rem; }
header { border-bottom: 2px solid var(--color-border); padding-bottom: 1.5rem; margin-bottom: 2rem; }
h1 { font-size: 1.5rem; font-weight: 600; }
.meta { color: var(--color-text-muted); font-size: 0.875rem; margin-top: 0.5rem; }
.summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(120px, 1fr)); gap: 1rem; margin-bottom: 2rem; }
.summary-card { background: var(--color-surface); border: 1px solid var(--color-border); border-radius: 8px; padding: 1rem; text-align: center; }
.summary-card .count { font-size: 2rem; font-weight: 700; }
.summary-card .label { font-size: 0.75rem; color: var(--color-text-muted); text-transform: uppercase; letter-spacing: 0.05em; }
.summary-card.critical .count { color: var(--color-critical); }
.summary-card.error .count { color: var(--color-error); }
.summary-card.warning .count { color: var(--color-warning); }
.summary-card.info .count { color: var(--color-info); }
.summary-card.pass .count { color: var(--color-pass); }
table { width: 100%; border-collapse: collapse; background: var(--color-surface); border: 1px solid var(--color-border); border-radius: 8px; overflow: hidden; }
th { background: var(--color-border); padding: 0.75rem 1rem; text-align: left; font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.05em; }
td { padding: 0.75rem 1rem; border-top: 1px solid var(--color-border); font-size: 0.9rem; }
.badge { display: inline-block; padding: 0.15rem 0.5rem; border-radius: 4px; font-size: 0.75rem; font-weight: 600; }
.badge-critical { background: rgba(255,71,87,0.2); color: var(--color-critical); }
.badge-error { background: rgba(255,107,53,0.2); color: var(--color-error); }
.badge-warning { background: rgba(255,165,2,0.2); color: var(--color-warning); }
.badge-info { background: rgba(30,144,255,0.2); color: var(--color-info); }
.badge-pass { background: rgba(46,213,115,0.2); color: var(--color-pass); }
.screenshot { margin-top: 0.5rem; }
.screenshot img { max-width: 400px; border-radius: 4px; border: 1px solid var(--color-border); }
footer { margin-top: 2rem; padding-top: 1rem; border-top: 1px solid var(--color-border); color: var(--color-text-muted); font-size: 0.8rem; }
</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<div class='container'>");

        // Header
        html.AppendLine("<header>");
        html.AppendLine("<h1>\U0001F4CB Civil QC Report</h1>");
        html.AppendLine($"<div class='meta'>Drawing: {WebUtility.HtmlEncode(report.DrawingPath)}</div>");
        html.AppendLine($"<div class='meta'>Generated: {report.Timestamp:yyyy-MM-dd HH:mm:ss} | Tool version: {report.ToolVersion}</div>");
        html.AppendLine("</header>");

        // Summary cards
        html.AppendLine("<div class='summary'>");
        html.AppendLine($"<div class='summary-card pass'><div class='count'>{report.Passed}</div><div class='label'>Passed</div></div>");
        html.AppendLine($"<div class='summary-card critical'><div class='count'>{report.CriticalCount}</div><div class='label'>Critical</div></div>");
        html.AppendLine($"<div class='summary-card error'><div class='count'>{report.ErrorCount}</div><div class='label'>Error</div></div>");
        html.AppendLine($"<div class='summary-card warning'><div class='count'>{report.WarningCount}</div><div class='label'>Warning</div></div>");
        html.AppendLine($"<div class='summary-card info'><div class='count'>{report.InfoCount}</div><div class='label'>Info</div></div>");
        html.AppendLine("</div>");

        // Results table
        html.AppendLine("<table>");
        html.AppendLine("<thead><tr><th>Status</th><th>Rule</th><th>Severity</th><th>Message</th><th>Object</th></tr></thead>");
        html.AppendLine("<tbody>");

        foreach (var r in report.Results.OrderByDescending(x => x.Severity).ThenBy(x => x.Passed))
        {
            var statusBadge = r.Passed
                ? "<span class='badge badge-pass'>PASS</span>"
                : $"<span class='badge badge-{r.Severity.ToString().ToLower()}'>{r.Severity.ToString().ToUpper()}</span>";
            var sevBadge = $"<span class='badge badge-{r.Severity.ToString().ToLower()}'>{r.Severity}</span>";

            html.AppendLine("<tr>");
            html.AppendLine($"<td>{statusBadge}</td>");
            html.AppendLine($"<td>{WebUtility.HtmlEncode(r.RuleName)}<br><small style='color:var(--color-text-muted)'>{WebUtility.HtmlEncode(r.RuleId)}</small></td>");
            html.AppendLine($"<td>{sevBadge}</td>");
            html.AppendLine($"<td>{WebUtility.HtmlEncode(r.Message)}</td>");
            html.AppendLine($"<td>{WebUtility.HtmlEncode(r.ObjectName ?? "-")}</td>");
            html.AppendLine("</tr>");

            // Show AI fix suggestion if available
            if (!string.IsNullOrEmpty(r.SuggestedFix))
            {
                html.AppendLine($"<tr><td colspan='5' style='background:var(--color-surface);padding:0.5rem 1rem;border-top:1px solid var(--color-border)'>");
                html.AppendLine($"<strong style='color:var(--color-pass)'>\U0001F4A1 Suggested Fix:</strong> {WebUtility.HtmlEncode(r.SuggestedFix)}");
                html.AppendLine("</td></tr>");
            }

            if (!string.IsNullOrEmpty(r.ScreenshotPath))
            {
                html.AppendLine($"<tr><td colspan='5' class='screenshot'><img src='{WebUtility.HtmlEncode(r.ScreenshotPath)}' alt='Screenshot for {WebUtility.HtmlEncode(r.RuleId)}' /></td></tr>");
            }
        }

        html.AppendLine("</tbody></table>");

        // Footer
        html.AppendLine("<footer>");
        html.AppendLine("<p>Generated by Civil QC | Open-source QA/QC for Civil 3D</p>");
        html.AppendLine("</footer>");

        html.AppendLine("</div></body></html>");

        File.WriteAllText(outputPath, html.ToString(), Encoding.UTF8);
    }

    public static void GenerateJson(ReportData report, string outputPath)
    {
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(outputPath, json, Encoding.UTF8);
    }

    // ── CSV ───────────────────────────────────────────────────────────────────────────

    public static void GenerateCsv(ReportData report, string outputPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Status,RuleId,RuleName,Severity,Message,ObjectName,LayerName");

        foreach (var r in report.Results)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(r.Passed ? "PASS" : "FAIL"),
                CsvEscape(r.RuleId),
                CsvEscape(r.RuleName),
                CsvEscape(r.Severity.ToString()),
                CsvEscape(r.Message ?? string.Empty),
                CsvEscape(r.ObjectName ?? string.Empty),
                CsvEscape(r.LayerName ?? string.Empty)));
        }

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return '"' + value.Replace("\"", "\"\"") + '"';
        return value;
    }

    // ── Batch Reports ─────────────────────────────────────────────────────────────────

    public static void GenerateBatchHtml(BatchReportData batch, string outputPath)
    {
        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang='en'>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset='UTF-8'>");
        html.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        html.AppendLine($"<title>Civil QC Batch Report - {Path.GetFileName(batch.DirectoryPath)}</title>");
        html.AppendLine(@"<style>
:root {
  --color-bg: #1a1a2e;
  --color-surface: #16213e;
  --color-border: #0f3460;
  --color-text: #e0e0e0;
  --color-text-muted: #a0a0b0;
  --color-critical: #ff4757;
  --color-error: #ff6b35;
  --color-warning: #ffa502;
  --color-info: #1e90ff;
  --color-pass: #2ed573;
}
* { margin: 0; padding: 0; box-sizing: border-box; }
body { font-family: 'Segoe UI', system-ui, sans-serif; background: var(--color-bg); color: var(--color-text); line-height: 1.5; }
.container { max-width: 1200px; margin: 0 auto; padding: 2rem; }
header { border-bottom: 2px solid var(--color-border); padding-bottom: 1.5rem; margin-bottom: 2rem; }
h1 { font-size: 1.5rem; font-weight: 600; }
.meta { color: var(--color-text-muted); font-size: 0.875rem; margin-top: 0.5rem; }
.summary { display: grid; grid-template-columns: repeat(auto-fit, minmax(120px, 1fr)); gap: 1rem; margin-bottom: 2rem; }
.summary-card { background: var(--color-surface); border: 1px solid var(--color-border); border-radius: 8px; padding: 1rem; text-align: center; }
.summary-card .count { font-size: 2rem; font-weight: 700; }
.summary-card .label { font-size: 0.75rem; color: var(--color-text-muted); text-transform: uppercase; letter-spacing: 0.05em; }
.summary-card.pass .count { color: var(--color-pass); }
.summary-card.error .count { color: var(--color-error); }
.summary-card.warning .count { color: var(--color-warning); }
.summary-card.critical .count { color: var(--color-critical); }
table { width: 100%; border-collapse: collapse; background: var(--color-surface); border: 1px solid var(--color-border); border-radius: 8px; overflow: hidden; }
th { background: var(--color-border); padding: 0.75rem 1rem; text-align: left; font-size: 0.8rem; text-transform: uppercase; letter-spacing: 0.05em; }
td { padding: 0.75rem 1rem; border-top: 1px solid var(--color-border); font-size: 0.9rem; }
.badge { display: inline-block; padding: 0.15rem 0.5rem; border-radius: 4px; font-size: 0.75rem; font-weight: 600; }
.badge-pass { background: rgba(46,213,115,0.2); color: var(--color-pass); }
.badge-fail { background: rgba(255,107,53,0.2); color: var(--color-error); }
.badge-error { background: rgba(255,71,87,0.2); color: var(--color-critical); }
footer { margin-top: 2rem; padding-top: 1rem; border-top: 1px solid var(--color-border); color: var(--color-text-muted); font-size: 0.8rem; }
</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<div class='container'>");

        // Header
        html.AppendLine("<header>");
        html.AppendLine("<h1>\U0001F4CB Civil QC Batch Report</h1>");
        html.AppendLine($"<div class='meta'>Directory: {WebUtility.HtmlEncode(batch.DirectoryPath)}</div>");
        html.AppendLine($"<div class='meta'>Generated: {batch.Timestamp:yyyy-MM-dd HH:mm:ss} | Tool version: {batch.ToolVersion}</div>");
        html.AppendLine("</header>");

        // Summary cards
        html.AppendLine("<div class='summary'>");
        html.AppendLine($"<div class='summary-card pass'><div class='count'>{batch.TotalPassed}</div><div class='label'>Passed</div></div>");
        html.AppendLine($"<div class='summary-card error'><div class='count'>{batch.TotalFailed}</div><div class='label'>Failed</div></div>");
        html.AppendLine($"<div class='summary-card critical'><div class='count'>{batch.TotalErrors}</div><div class='label'>Errors</div></div>");
        html.AppendLine("</div>");

        // Results table
        html.AppendLine("<table>");
        html.AppendLine("<thead><tr><th>Drawing</th><th>Status</th><th>Passed</th><th>Failed</th><th>Critical</th><th>Errors</th><th>Warnings</th></tr></thead>");
        html.AppendLine("<tbody>");

        foreach (var r in batch.Results)
        {
            var name = Path.GetFileName(r.DrawingPath);
            var statusBadge = !r.Success
                ? $"<span class='badge badge-error'>ERROR</span>"
                : r.Failed == 0
                    ? $"<span class='badge badge-pass'>PASS</span>"
                    : $"<span class='badge badge-fail'>FAIL</span>";

            html.AppendLine("<tr>");
            html.AppendLine($"<td>{WebUtility.HtmlEncode(name)}</td>");
            html.AppendLine($"<td>{statusBadge}</td>");
            html.AppendLine($"<td>{r.Passed}</td>");
            html.AppendLine($"<td>{r.Failed}</td>");
            html.AppendLine($"<td>{r.CriticalCount}</td>");
            html.AppendLine($"<td>{r.ErrorCount}</td>");
            html.AppendLine($"<td>{r.WarningCount}</td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine("</tbody></table>");

        // Footer
        html.AppendLine("<footer>");
        html.AppendLine("<p>Generated by Civil QC | Open-source QA/QC for Civil 3D</p>");
        html.AppendLine("</footer>");
        html.AppendLine("</div></body></html>");

        File.WriteAllText(outputPath, html.ToString(), Encoding.UTF8);
    }

    public static void GenerateBatchJson(BatchReportData batch, string outputPath)
    {
        var json = JsonSerializer.Serialize(batch, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        File.WriteAllText(outputPath, json, Encoding.UTF8);
    }
}
