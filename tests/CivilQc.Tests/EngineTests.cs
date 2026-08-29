using CivilQc.Engine;
using Xunit;

namespace CivilQc.Tests;

public class RuleLoaderTests
{
    [Fact]
    public void LoadDefault_ReturnsBuiltInRules()
    {
        var config = RuleLoader.LoadDefault();
        Assert.NotNull(config);
        Assert.NotEmpty(config.Rules);
    }

    [Fact]
    public void LoadDefault_ContainsFileSizeRule()
    {
        var config = RuleLoader.LoadDefault();
        Assert.Contains(config.Rules, r => r.Id == "PERF-001");
    }

    [Fact]
    public void LoadDefault_AllRulesHaveRequiredFields()
    {
        var config = RuleLoader.LoadDefault();
        foreach (var rule in config.Rules)
        {
            Assert.False(string.IsNullOrEmpty(rule.Id), "Rule ID should not be empty");
            Assert.False(string.IsNullOrEmpty(rule.Name), "Rule Name should not be empty");
            Assert.False(string.IsNullOrEmpty(rule.CheckType), "Rule CheckType should not be empty");
        }
    }
}

public class RuleEngineTests
{
    [Fact]
    public void BuildPluginArguments_ContainsRequiredFlags()
    {
        var config = RuleLoader.LoadDefault();

        var argsPath = RuleEngine.WritePluginArguments(config, "test.dwg", "output.json", "screenshots");
        Assert.False(string.IsNullOrEmpty(argsPath));

        var argsContent = File.ReadAllText(argsPath);
        Assert.Contains("\"rules\"", argsContent);
        Assert.Contains("\"output\"", argsContent);
        Assert.Contains("\"screenshots\"", argsContent);
        Assert.Contains("test.dwg", argsContent);
    }

    [Fact]
    public void ParseResults_MissingFile_ReturnsError()
    {
        var report = RuleEngine.ParseResults("test.dwg", "nonexistent.json");

        Assert.False(report.Results[0].Passed);
        Assert.Equal(Severity.Critical, report.Results[0].Severity);
    }
}

public class ReportGeneratorTests
{
    [Fact]
    public void GenerateHtml_CreatesFile()
    {
        var report = new ReportData
        {
            DrawingPath = @"C:\test\sample.dwg",
            ToolVersion = "0.1.0",
            Results = new List<CheckResult>
            {
                new()
                {
                    RuleId = "TEST-001",
                    RuleName = "Test Rule",
                    Severity = Severity.Warning,
                    Passed = false,
                    Message = "Test issue"
                },
                new()
                {
                    RuleId = "TEST-002",
                    RuleName = "Passing Rule",
                    Severity = Severity.Info,
                    Passed = true,
                    Message = "All good"
                }
            }
        };

        var outputPath = Path.Combine(Path.GetTempPath(), $"civil_qc_test_{Guid.NewGuid():N}.html");
        try
        {
            ReportGenerator.GenerateHtml(report, outputPath);
            Assert.True(File.Exists(outputPath));

            var content = File.ReadAllText(outputPath);
            Assert.Contains("Civil QC Report", content);
            Assert.Contains("Test Rule", content);
            Assert.Contains("PASS", content);
            Assert.Contains("WARNING", content);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public void GenerateJson_CreatesValidJson()
    {
        var report = new ReportData
        {
            DrawingPath = @"C:\test\sample.dwg",
            ToolVersion = "0.1.0",
            Results = new List<CheckResult>()
        };

        var outputPath = Path.Combine(Path.GetTempPath(), $"civil_qc_test_{Guid.NewGuid():N}.json");
        try
        {
            ReportGenerator.GenerateJson(report, outputPath);
            Assert.True(File.Exists(outputPath));

            var content = File.ReadAllText(outputPath);
            Assert.Contains("drawingPath", content);
            Assert.Contains("results", content);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public void GenerateCsv_CreatesFile()
    {
        var report = new ReportData
        {
            DrawingPath = @"C:\test\sample.dwg",
            ToolVersion = "0.1.0",
            Results = new List<CheckResult>
            {
                new()
                {
                    RuleId = "TEST-001",
                    RuleName = "Test Rule",
                    Severity = Severity.Warning,
                    Passed = false,
                    Message = "Test issue"
                },
                new()
                {
                    RuleId = "TEST-002",
                    RuleName = "Passing Rule",
                    Severity = Severity.Info,
                    Passed = true,
                    Message = "All good"
                }
            }
        };

        var outputPath = Path.Combine(Path.GetTempPath(), $"civil_qc_test_{Guid.NewGuid():N}.csv");
        try
        {
            ReportGenerator.GenerateCsv(report, outputPath);
            Assert.True(File.Exists(outputPath));

            var content = File.ReadAllText(outputPath);
            Assert.Contains("Status,RuleId,RuleName,Severity,Message", content);
            Assert.Contains("FAIL", content);
            Assert.Contains("PASS", content);
            Assert.Contains("Test Rule", content);
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}
