using System.CommandLine;
using System.Text.RegularExpressions;
using CivilQc.Engine;
using CivilQc.Ai;
using YamlDotNet.RepresentationModel;

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
    var reportBaseRaw = output?.FullName ?? Path.Combine(reportDir, Path.GetFileNameWithoutExtension(drawing.FullName) + ".civil-qc");
    var reportBase = Path.ChangeExtension(reportBaseRaw, null);
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

// --- ai command group ---
var aiApiKeyOption = new Option<string?>("--api-key", "AI API key (or set CIVIL_QC_AI_KEY env var)");
var aiApiBaseOption = new Option<string?>("--api-base", "AI API base URL (default: https://openrouter.ai/api/v1)");
var aiModelOption = new Option<string?>("--model", "AI model name (default: anthropic/claude-sonnet-4)");

var aiCommand = new Command("ai", "AI-powered features (optional, requires BYOK)")
{
    aiApiKeyOption, aiApiBaseOption, aiModelOption
};

// --- ai generate-rules ---
var grDescriptionOption = new Option<string?>("--description", "Natural-language description of the QA/QC rules to generate");
var grFileOption = new Option<FileInfo?>("--file", "Path to a standards document to extract rules from");
var grOutputOption = new Option<FileInfo?>("--output", () => new FileInfo("rules/ai-generated.yaml"), "Output path for generated YAML");

var generateRulesCommand = new Command("generate-rules", "Generate QA/QC rule YAML from a description or standards document")
{
    grDescriptionOption, grFileOption, grOutputOption, aiApiKeyOption, aiApiBaseOption, aiModelOption
};

generateRulesCommand.SetHandler(async (description, file, output, apiKey, apiBase, model) =>
{
    if (string.IsNullOrWhiteSpace(description) && file is null)
    {
        Console.Error.WriteLine("Error: Provide --description or --file.");
        return;
    }

    if (!string.IsNullOrWhiteSpace(description) && file is not null)
    {
        Console.Error.WriteLine("Error: Use either --description or --file, not both.");
        return;
    }

    var config = AiConfig.Load(apiKey, apiBase, model);
    if (!config.IsConfigured)
    {
        Console.Error.WriteLine("Error: No AI API key configured.");
        Console.Error.WriteLine("Set --api-key flag, CIVIL_QC_AI_KEY env var, or ~/.civil-qa-qc/config.json");
        return;
    }

    Console.WriteLine("Generating QA/QC rules with AI...");
    Console.WriteLine($"  Model: {config.Model}");

    var client = new OpenAiClient(config);
    var service = new RuleGeneratorService(client);

    try
    {
        var yaml = !string.IsNullOrWhiteSpace(description)
            ? await service.GenerateFromDescriptionAsync(description)
            : await service.GenerateFromFileAsync(file!.FullName);

        // Strip markdown code fences that LLMs sometimes wrap around YAML
        yaml = StripMarkdownFences(yaml);

        // Validate YAML syntax before writing
        try
        {
            var yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(yaml));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"WARNING: Generated YAML failed validation: {ex.Message}");
            Console.Error.WriteLine("Writing file anyway — please review the output.");
        }

        // Ensure output directory exists
        var outputDir = Path.GetDirectoryName(output?.FullName ?? "rules/ai-generated.yaml");
        if (!string.IsNullOrEmpty(outputDir))
            Directory.CreateDirectory(outputDir);

        var outputPath = output?.FullName ?? "rules/ai-generated.yaml";
        await File.WriteAllTextAsync(outputPath, yaml);

        Console.WriteLine();
        Console.WriteLine($"Generated rules written to: {outputPath}");
    }
    catch (AiApiException ex)
    {
        Console.Error.WriteLine($"AI API error: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
    }
}, grDescriptionOption, grFileOption, grOutputOption, aiApiKeyOption, aiApiBaseOption, aiModelOption);

aiCommand.AddCommand(generateRulesCommand);

// --- ai summarize ---
var smInputOption = new Option<DirectoryInfo?>("--input", () => new DirectoryInfo("."), "Directory containing batch check result JSON files");
var smOutputOption = new Option<string?>("--output", "Output path for markdown summary (default: stdout)");

var summarizeCommand = new Command("summarize", "Summarize batch QA/QC results using AI")
{
    smInputOption, smOutputOption, aiApiKeyOption, aiApiBaseOption, aiModelOption
};

summarizeCommand.SetHandler(async (input, output, apiKey, apiBase, model) =>
{
    var config = AiConfig.Load(apiKey, apiBase, model);
    if (!config.IsConfigured)
    {
        Console.Error.WriteLine("Error: No AI API key configured.");
        Console.Error.WriteLine("Set --api-key flag, CIVIL_QC_AI_KEY env var, or ~/.civil-qa-qc/config.json");
        return;
    }

    var inputDir = input?.FullName ?? ".";
    Console.WriteLine($"Summarizing batch results from: {Path.GetFullPath(inputDir)}");
    Console.WriteLine($"  Model: {config.Model}");

    var client = new OpenAiClient(config);
    var service = new BatchSummarizerService(client);

    try
    {
        var summary = await service.SummarizeDirectoryAsync(inputDir);

        if (!string.IsNullOrEmpty(output))
        {
            var outputDir = Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(outputDir))
                Directory.CreateDirectory(outputDir);

            await File.WriteAllTextAsync(output, summary);
            Console.WriteLine();
            Console.WriteLine($"Summary written to: {output}");
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine(summary);
        }
    }
    catch (AiApiException ex)
    {
        Console.Error.WriteLine($"AI API error: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error: {ex.Message}");
    }
}, smInputOption, smOutputOption, aiApiKeyOption, aiApiBaseOption, aiModelOption);

aiCommand.AddCommand(summarizeCommand);

rootCommand.AddCommand(aiCommand);

static string StripMarkdownFences(string text)
{
    // Remove ```yaml ... ``` or ``` ... ``` fences that LLMs sometimes add
    return Regex.Replace(text.Trim(), @"^```(?:ya?ml)?\s*\r?\n?", "", RegexOptions.Multiline)
                .TrimEnd('`')
                .Trim();
}

return await rootCommand.InvokeAsync(args);
