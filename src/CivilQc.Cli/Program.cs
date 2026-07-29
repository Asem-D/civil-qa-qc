using System.CommandLine;
using CivilQc.Engine;

var rootCommand = new RootCommand("Civil QC - Open-source QA/QC tool for Civil 3D drawings");

// --- check command ---
var checkFileArg = new Argument<FileInfo>("drawing", "Path to the DWG file to check");
var rulesOption = new Option<FileInfo?>("--rules", "Path to YAML rules config file");
var outputOption = new Option<FileInfo?>("--output", "Path for output report file");
var formatOption = new Option<string>("--format", () => "html", "Report format: html, json, or both");
var screenshotOption = new Option<DirectoryInfo?>("--screenshots", "Directory for screenshot output");
var verboseOption = new Option<bool>("--verbose", "Show detailed output during checks");
var recoverOption = new Option<bool>("--recover", "Force RECOVER mode for corrupt drawings");

var checkCommand = new Command("check", "Run QA/QC checks on a Civil 3D drawing")
{
    checkFileArg, rulesOption, outputOption, formatOption, screenshotOption, verboseOption, recoverOption
};

checkCommand.SetHandler(async (drawing, rules, output, format, screenshots, verbose, recover) =>
{
    if (!drawing.Exists)
    {
        Console.Error.WriteLine($"Error: Drawing not found: {drawing.FullName}");
        return;
    }

    Console.WriteLine($"Civil QC v0.1.0");
    Console.WriteLine($"Drawing: {drawing.FullName}");
    Console.WriteLine();

    // Load rules
    var ruleConfig = rules != null && rules.Exists
        ? RuleLoader.LoadFromFile(rules.FullName)
        : RuleLoader.LoadDefault();

    var enabledCount = ruleConfig.Rules.Count(r => r.Enabled);
    Console.WriteLine($"Loaded {ruleConfig.Rules.Count} rules ({enabledCount} enabled)");

    if (verbose)
    {
        foreach (var rule in ruleConfig.Rules.Where(r => r.Enabled))
            Console.WriteLine($"  [{rule.Id}] {rule.Name} ({rule.Severity})");
        Console.WriteLine();
    }

    // Prepare output paths
    var reportDir = output?.FullName != null
        ? Path.GetDirectoryName(output.FullName) ?? "."
        : Path.GetDirectoryName(drawing.FullName) ?? ".";
    var reportBase = output?.FullName ?? Path.Combine(reportDir, Path.GetFileNameWithoutExtension(drawing.FullName) + ".civil-qc");
    var screenshotDir = screenshots?.FullName ?? Path.Combine(Path.GetDirectoryName(reportBase)!, "screenshots");
    Directory.CreateDirectory(screenshotDir);

    // Use a temp file for raw plugin output (separate from report file).
    var pluginOutputPath = Path.Combine(Path.GetTempPath(), $"civil_qc_output_{Guid.NewGuid():N}.json");
    var argsPath = RuleEngine.WritePluginArguments(ruleConfig, drawing.FullName, pluginOutputPath, screenshotDir);

    // Run via accoreconsole
    Console.WriteLine("Launching Civil 3D (headless)...");
    var host = new AccoreHost();

    try
    {
        var (exitCode, stdout, stderr) = host.Run(drawing.FullName, argsPath, recover);

        // Auto-retry with RECOVER if the drawing appears corrupt (and not already in recover mode).
        if (!recover && AccoreHost.IsCorruptDrawingError(exitCode, stdout, stderr))
        {
            Console.WriteLine("Drawing appears corrupt or has errors. Retrying with RECOVER...");
            var retry = host.Run(drawing.FullName, argsPath, recover: true);
            exitCode = retry.exitCode;
            stdout = retry.output;
            stderr = retry.error;
        }

        if (verbose)
        {
            if (!string.IsNullOrEmpty(stdout))
                Console.WriteLine($"accoreconsole stdout:\n{stdout}");
            if (!string.IsNullOrEmpty(stderr))
                Console.Error.WriteLine($"accoreconsole stderr:\n{stderr}");

            var debugLog = Path.Combine(Path.GetTempPath(), "civil_qc_debug.log");
            if (File.Exists(debugLog))
            {
                Console.WriteLine($"Plugin debug log:\n{File.ReadAllText(debugLog)}");
                File.Delete(debugLog);
            }
        }

        if (exitCode != 0)
        {
            Console.Error.WriteLine($"accoreconsole exited with code {exitCode}");
            Console.Error.WriteLine("If the drawing is corrupt, open it in Civil 3D and run RECOVER to fix it.");
        }

        // Parse plugin results and generate report
        var reportData = RuleEngine.ParseResults(drawing.FullName, pluginOutputPath);

        // Clean up temp plugin output file
        try { File.Delete(pluginOutputPath); } catch { /* best effort */ }

        if (format is "html" or "both")
        {
            var htmlPath = reportBase + ".html";
            ReportGenerator.GenerateHtml(reportData, htmlPath);
            Console.WriteLine($"HTML report: {htmlPath}");
        }

        if (format is "json" or "both")
        {
            var jsonPath = reportBase + ".json";
            ReportGenerator.GenerateJson(reportData, jsonPath);
            Console.WriteLine($"JSON report: {jsonPath}");
        }

        // Print summary
        Console.WriteLine();
        Console.WriteLine($"Results: {reportData.Passed} passed, {reportData.Failed} failed ({reportData.CriticalCount} critical, {reportData.ErrorCount} errors, {reportData.WarningCount} warnings)");
    }
    catch (FileNotFoundException ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
    }
    catch (IOException ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
    }
}, checkFileArg, rulesOption, outputOption, formatOption, screenshotOption, verboseOption, recoverOption);

rootCommand.AddCommand(checkCommand);

return await rootCommand.InvokeAsync(args);
