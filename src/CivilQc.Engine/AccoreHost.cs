using System.Diagnostics;
using System.Text;

namespace CivilQc.Engine;

/// <summary>
/// Spawns accoreconsole.exe to run the plugin inside a headless Civil 3D process.
/// </summary>
public class AccoreHost
{
    private readonly string _accoreconsolePath;

    public AccoreHost(string? accoreconsolePath = null)
    {
        _accoreconsolePath = accoreconsolePath ?? FindAccoreconsole();
    }

    /// <summary>
    /// Write plugin arguments to a temp JSON file and return its path.
    /// The plugin reads from this file during CIVILQC_CHECK execution.
    /// </summary>
    public static string WriteArgsFile(string argsJson)
    {
        var argsPath = Path.Combine(Path.GetTempPath(), "civil_qc_active_args.json");
        try
        {
            File.WriteAllText(argsPath, argsJson, Encoding.UTF8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
        {
            throw new IOException($"Failed to write plugin arguments to {argsPath}: {ex.Message}", ex);
        }
        return argsPath;
    }

    /// <summary>
    /// Run accoreconsole with a script that loads the plugin and executes checks.
    /// pluginArgsPath is the temp JSON file written by WriteArgsFile().
    /// When recover is true, uses RECOVER command to auto-repair corrupt drawings.
    /// When repair is true, uses AUDIT command to fix errors before checks.
    /// </summary>
    public (int exitCode, string output, string error) Run(string drawingPath, string pluginArgsPath, bool recover = false, bool repair = false)
    {
        if (!File.Exists(_accoreconsolePath))
            throw new FileNotFoundException($"accoreconsole.exe not found at: {_accoreconsolePath}");

        if (!File.Exists(drawingPath))
            throw new FileNotFoundException($"Drawing not found: {drawingPath}");

        if (!File.Exists(pluginArgsPath))
            throw new FileNotFoundException($"Plugin args file not found: {pluginArgsPath}");

        var scriptPath = Path.Combine(Path.GetTempPath(), $"civil_qc_{Guid.NewGuid():N}.scr");
        var pluginDll = Path.Combine(AppContext.BaseDirectory, "CivilQc.Plugin.dll");

        var script = new StringBuilder();
        script.AppendLine("(setvar \"SECURELOAD\" 0)");

        if (recover)
        {
            // Use RECOVER to auto-repair corrupt drawings before loading the plugin.
            // RECOVER opens the drawing with built-in error repair.
            // When the drawing is clean, RECOVER behaves like a normal open.
            script.AppendLine("RECOVER");
            script.AppendLine($"\"{drawingPath}\"");
        }

        if (repair && !recover)
        {
            // Use AUDIT to fix errors in the currently open drawing.
            // AUDIT is lighter than RECOVER (does not reopen the file).
            script.AppendLine("AUDIT");
            script.AppendLine("Y"); // Answer "Yes" to fix errors
        }

        script.AppendLine($"NETLOAD \"{pluginDll}\"");
        script.AppendLine("CIVILQC_CHECK");
        script.AppendLine("QUIT");
        script.AppendLine("Y");

        File.WriteAllText(scriptPath, script.ToString(), Encoding.ASCII);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _accoreconsolePath,
                Arguments = recover
                    ? $"/s \"{scriptPath}\" /l en-US"
                    : $"/i \"{drawingPath}\" /s \"{scriptPath}\" /l en-US",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Failed to start accoreconsole process");

            // Read streams asynchronously to avoid deadlock
            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();
            process.WaitForExit(300_000); // 5 minute timeout

            return (process.ExitCode, stdoutTask.Result, stderrTask.Result);
        }
        finally
        {
            if (File.Exists(scriptPath))
                File.Delete(scriptPath);
        }
    }

    /// <summary>
    /// Check whether accoreconsole output indicates a corrupt drawing
    /// that should be retried with RECOVER.
    /// </summary>
    public static bool IsCorruptDrawingError(int exitCode, string stdout, string stderr)
    {
        // ErrorStatus=53 typically indicates corrupt or unsupported objects.
        var combined = $"{stdout} {stderr}";
        return exitCode == 53
            || combined.IndexOf("ErrorStatus=53", StringComparison.OrdinalIgnoreCase) >= 0
            || combined.IndexOf("could not be opened", StringComparison.OrdinalIgnoreCase) >= 0
            || combined.IndexOf("drawing is corrupt", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string FindAccoreconsole()
    {
        var candidates = new[]
        {
            @"C:\Program Files\Autodesk\AutoCAD 2025\accoreconsole.exe",
            @"C:\Program Files\Autodesk\AutoCAD 2024\accoreconsole.exe",
            @"C:\Program Files\Autodesk\AutoCAD 2023\accoreconsole.exe",
            @"C:\Program Files\Autodesk\AutoCAD 2025\acad.exe",
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        var envPath = Environment.GetEnvironmentVariable("ACCORECONSOLE_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
            return envPath;

        throw new FileNotFoundException(
            "Cannot find accoreconsole.exe. Set ACCORECONSOLE_PATH environment variable " +
            "or install Civil 3D 2023-2025.");
    }
}
