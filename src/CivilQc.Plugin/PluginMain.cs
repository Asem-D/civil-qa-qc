using System.Reflection;
using System.Text.Json;
using System.Text;
using Autodesk.AutoCAD.Runtime;
using CivilQc.Engine;
using CivilQc.Rules;

namespace CivilQc.Plugin;

/// <summary>
/// Plugin entry point that runs inside accoreconsole.
/// Registers the CIVILQC_CHECK command via [CommandMethod] attribute.
///
/// Flow:
/// 1. CLI writes temp JSON args file (civil_qc_active_args.json)
/// 2. CLI writes .scr: NETLOAD this DLL, then CIVILQC_CHECK
/// 3. accoreconsole loads this DLL and calls ExecuteCommand()
/// 4. ExecuteCommand() reads args from temp file, runs rules, writes JSON output
/// 5. accoreconsole exits, CLI picks up the JSON and generates HTML report
/// </summary>
public static class PluginMain
{
    // Well-known temp path used by both CLI (writes) and Plugin (reads)
    private const string ArgsFileName = "civil_qc_active_args.json";

    // Ensure dependency DLLs are found in the plugin's own directory
    private static readonly string PluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

    private static bool _assembliesLoaded;

    static PluginMain()
    {
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            var name = new AssemblyName(args.Name).Name + ".dll";
            var path = Path.Combine(PluginDir, name);
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };

        // Pre-load dependent assemblies so DiscoverRules can find them
        LoadDependentAssemblies();
    }

    private static void LoadDependentAssemblies()
    {
        if (_assembliesLoaded) return;
        _assembliesLoaded = true;

        foreach (var dll in Directory.GetFiles(PluginDir, "CivilQc.*.dll"))
        {
            var name = AssemblyName.GetAssemblyName(dll);
            // Skip the plugin assembly itself and any already-loaded
            if (name.Name == Assembly.GetExecutingAssembly().GetName().Name) continue;
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == name.Name)) continue;
            try { Assembly.LoadFrom(dll); } catch { /* will be caught by AssemblyResolve later if needed */ }
        }
    }

    /// <summary>
    /// AutoCAD command handler registered via [CommandMethod].
    /// Called by accoreconsole when the .scr script runs CIVILQC_CHECK.
    /// </summary>
    private static readonly string DebugLogPath = Path.Combine(Path.GetTempPath(), "civil_qc_debug.log");

    private static void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
        File.AppendAllText(DebugLogPath, line, Encoding.UTF8);
    }

    [CommandMethod("CIVILQC_CHECK")]
    public static void ExecuteCommand()
    {
        Log("ExecuteCommand called");
        try
        {
            var argsPath = Path.Combine(Path.GetTempPath(), ArgsFileName);
            Log($"Args path: {argsPath}, exists: {File.Exists(argsPath)}");
            if (!File.Exists(argsPath))
            {
                Log("Args file not found, returning");
                return;
            }

            var argsJson = File.ReadAllText(argsPath, Encoding.UTF8);
            Log($"Args JSON length: {argsJson.Length}");
            var payload = JsonSerializer.Deserialize<ArgsPayload>(argsJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (payload == null || payload.Rules == null || payload.Rules.Count == 0)
            {
                Log("No rules found in args file");
                return;
            }

            Log($"Rules: {payload.Rules.Count}, Drawing: {payload.Drawing}, Output: {payload.Output}");
            var results = Execute(payload.Rules, payload.Drawing, payload.Screenshots);
            Log($"Results: {results.Count} checks");

            // Write results as JSON
            var resultJson = JsonSerializer.Serialize(results, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            File.WriteAllText(payload.Output, resultJson, Encoding.UTF8);
            Log($"Output written to {payload.Output}");
        }
        catch (System.Exception ex)
        {
            Log($"ERROR: {ex}");
        }
    }

    public static List<CheckResult> Execute(
        List<RuleDefinition> rules,
        string drawingPath,
        string screenshotDir)
    {
        var results = new List<CheckResult>();

        // Populate DrawingContext with real AutoCAD API handles.
        // Rules cast these via AcadContext to access the drawing database.
        var doc = Autodesk.AutoCAD.ApplicationServices.Application
            .DocumentManager.MdiActiveDocument;

        var context = new DrawingContext
        {
            DrawingPath = drawingPath,
            ScreenshotDir = screenshotDir,
            Document = doc,
            Database = doc?.Database
        };

        var ruleImplementations = DiscoverRules();

        foreach (var rule in rules.Where(r => r.Enabled))
        {
            try
            {
                var impl = ruleImplementations.GetValueOrDefault(rule.CheckType);
                if (impl == null)
                {
                    results.Add(new CheckResult
                    {
                        RuleId = rule.Id,
                        RuleName = rule.Name,
                        Severity = rule.Severity,
                        Passed = false,
                        Message = $"No implementation found for check type: {rule.CheckType}"
                    });
                    continue;
                }

                var ruleResults = impl.Execute(rule, context);
                results.AddRange(ruleResults);
            }
            catch (System.Exception ex)
            {
                results.Add(new CheckResult
                {
                    RuleId = rule.Id,
                    RuleName = rule.Name,
                    Severity = rule.Severity,
                    Passed = false,
                    Message = $"Rule execution failed: {ex.Message}"
                });
            }
        }

        return results;
    }

    private static Dictionary<string, IRule> DiscoverRules()
    {
        var rules = new Dictionary<string, IRule>(StringComparer.OrdinalIgnoreCase);

        // Scan all loaded assemblies for IRule implementations, not just the Plugin assembly
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Skip dynamic and framework assemblies
            if (assembly.IsDynamic || assembly.Location == "") continue;

            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(IRule).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                    {
                        if (Activator.CreateInstance(type) is IRule rule)
                        {
                            var name = type.Name.Replace("Rule", "");
                            rules[name] = rule;
                        }
                    }
                }
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that can't fully load their types
            }
        }

        return rules;
    }

    /// <summary>
    /// Deserialization model for the args JSON file written by the CLI.
    /// </summary>
    private class ArgsPayload
    {
        public List<RuleDefinition> Rules { get; set; } = new();
        public string Drawing { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public string Screenshots { get; set; } = string.Empty;
    }
}
