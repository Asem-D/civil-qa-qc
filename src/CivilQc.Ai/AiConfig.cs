using System.Text.Json;

namespace CivilQc.Ai;

/// <summary>
/// Configuration for AI features. Resolved in order of precedence:
///   1. CLI flags (passed directly to AiConfig)
///   2. Environment variables (CIVIL_QC_AI_KEY, CIVIL_QC_AI_BASE, CIVIL_QC_AI_MODEL)
///   3. Config file (~/.civil-qa-qc/config.json)
/// </summary>
public class AiConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiBase { get; set; } = "https://openrouter.ai/api/v1";
    public string Model { get; set; } = "anthropic/claude-sonnet-4";

    /// <summary>
    /// Returns true if an API key is configured.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>
    /// Load configuration by merging file, env vars, and CLI overrides.
    /// CLI overrides take highest precedence.
    /// </summary>
    public static AiConfig Load(string? cliApiKey = null, string? cliApiBase = null, string? cliModel = null)
    {
        var config = LoadFromFile();
        ApplyEnvironmentVariables(config);

        // CLI flags override everything
        if (!string.IsNullOrWhiteSpace(cliApiKey))
            config.ApiKey = cliApiKey;
        if (!string.IsNullOrWhiteSpace(cliApiBase))
            config.ApiBase = cliApiBase;
        if (!string.IsNullOrWhiteSpace(cliModel))
            config.Model = cliModel;

        return config;
    }

    private static AiConfig LoadFromFile()
    {
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".civil-qa-qc", "config.json");

        if (!File.Exists(configPath))
            return new AiConfig();

        try
        {
            var json = File.ReadAllText(configPath);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var config = new AiConfig();

            if (root.TryGetProperty("ai", out var aiSection))
            {
                // api_key_env: read the env var named by this value
                if (aiSection.TryGetProperty("api_key_env", out var envVarName))
                {
                    var envValue = Environment.GetEnvironmentVariable(envVarName.GetString());
                    if (!string.IsNullOrEmpty(envValue))
                        config.ApiKey = envValue;
                }

                // Direct api_key (fallback if api_key_env not set)
                if (string.IsNullOrEmpty(config.ApiKey) && aiSection.TryGetProperty("api_key", out var directKey))
                    config.ApiKey = directKey.GetString() ?? string.Empty;

                if (aiSection.TryGetProperty("api_base", out var apiBase))
                    config.ApiBase = apiBase.GetString() ?? config.ApiBase;

                if (aiSection.TryGetProperty("model", out var model))
                    config.Model = model.GetString() ?? config.Model;
            }

            return config;
        }
        catch
        {
            // Corrupt config file — ignore and return defaults
            return new AiConfig();
        }
    }

    private static void ApplyEnvironmentVariables(AiConfig config)
    {
        var envKey = Environment.GetEnvironmentVariable("CIVIL_QC_AI_KEY");
        if (!string.IsNullOrEmpty(envKey))
            config.ApiKey = envKey;

        var envBase = Environment.GetEnvironmentVariable("CIVIL_QC_AI_BASE");
        if (!string.IsNullOrEmpty(envBase))
            config.ApiBase = envBase;

        var envModel = Environment.GetEnvironmentVariable("CIVIL_QC_AI_MODEL");
        if (!string.IsNullOrEmpty(envModel))
            config.Model = envModel;
    }
}
