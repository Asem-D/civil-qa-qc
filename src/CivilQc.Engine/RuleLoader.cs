using System.Text.Json;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CivilQc.Engine;

/// <summary>
/// Converts snake_case YAML keys to PascalCase C# properties.
/// </summary>
public sealed class SnakeCaseNamingConvention : INamingConvention
{
    public static readonly SnakeCaseNamingConvention Instance = new();
    public string Apply(string name) =>
        Regex.Replace(name, @"([A-Z])", "_$1").TrimStart('_').ToLowerInvariant();
    public string Reverse(string name) =>
        Regex.Replace(name, @"(^|_)(\w)", m => m.Groups[2].Value.ToUpperInvariant());
    public bool PrependAllowedPrefix(string name) => true;
    public bool IsAllowedPrefix(string name) => false;
}

public class RuleConfig
{
    public List<RuleDefinition> Rules { get; set; } = new();
}

public static class RuleLoader
{
    public static RuleConfig LoadFromFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Rules file not found: {path}");

        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(SnakeCaseNamingConvention.Instance)
            .Build();

        var config = deserializer.Deserialize<RuleConfig>(yaml);
        return config ?? new RuleConfig();
    }

    public static RuleConfig LoadDefault()
    {
        var exeDir = AppContext.BaseDirectory;
        var defaultPath = Path.Combine(exeDir, "rules", "default.yaml");
        if (File.Exists(defaultPath))
            return LoadFromFile(defaultPath);

        return new RuleConfig { Rules = GetBuiltInDefaults() };
    }

    private static List<RuleDefinition> GetBuiltInDefaults()
    {
        return new List<RuleDefinition>
        {
            new() { Id = "PERF-001", Name = "File size", CheckType = "FileSize", Severity = Severity.Info },
        };
    }
}
