using CivilQc.Engine;
using Xunit;

namespace CivilQc.Tests;

/// <summary>
/// Tests for the v0.2.0 rules: ANNO-001, ANNO-002, BLOCK-001, BLOCK-002, DWG-001.
/// These tests verify rule discovery and YAML configuration, not drawing execution.
/// </summary>
public class NewRulesDiscoveryTests
{
    [Fact]
    public void LoadDefault_ContainsAll12Rules()
    {
        var config = RuleLoader.LoadDefault();
        Assert.Equal(12, config.Rules.Count);
    }

    [Fact]
    public void LoadDefault_ContainsAnno001()
    {
        var config = RuleLoader.LoadDefault();
        Assert.Contains(config.Rules, r => r.Id == "ANNO-001");
    }

    [Fact]
    public void LoadDefault_ContainsAnno002()
    {
        var config = RuleLoader.LoadDefault();
        Assert.Contains(config.Rules, r => r.Id == "ANNO-002");
    }

    [Fact]
    public void LoadDefault_ContainsBlock001()
    {
        var config = RuleLoader.LoadDefault();
        Assert.Contains(config.Rules, r => r.Id == "BLOCK-001");
    }

    [Fact]
    public void LoadDefault_ContainsBlock002()
    {
        var config = RuleLoader.LoadDefault();
        Assert.Contains(config.Rules, r => r.Id == "BLOCK-002");
    }

    [Fact]
    public void LoadDefault_ContainsDwg001()
    {
        var config = RuleLoader.LoadDefault();
        Assert.Contains(config.Rules, r => r.Id == "DWG-001");
    }

    [Fact]
    public void LoadDefault_NewRulesHaveValidSeverity()
    {
        var config = RuleLoader.LoadDefault();
        var newRuleIds = new[] { "ANNO-001", "ANNO-002", "BLOCK-001", "BLOCK-002", "DWG-001" };

        foreach (var id in newRuleIds)
        {
            var rule = config.Rules.FirstOrDefault(r => r.Id == id);
            Assert.NotNull(rule);
            Assert.True(Enum.IsDefined(typeof(Severity), rule.Severity),
                $"Rule {id} has invalid severity: {rule.Severity}");
        }
    }

    [Fact]
    public void LoadDefault_NewRulesHaveCheckType()
    {
        var config = RuleLoader.LoadDefault();
        var newRuleIds = new[] { "ANNO-001", "ANNO-002", "BLOCK-001", "BLOCK-002", "DWG-001" };

        foreach (var id in newRuleIds)
        {
            var rule = config.Rules.FirstOrDefault(r => r.Id == id);
            Assert.NotNull(rule);
            Assert.False(string.IsNullOrEmpty(rule.CheckType),
                $"Rule {id} has empty CheckType");
        }
    }
}

/// <summary>
/// Tests that the IRule interface is properly implemented for all new rules.
/// Uses reflection to verify the contract without requiring AutoCAD.
/// </summary>
public class NewRulesContractTests
{
    private static readonly Type[] RuleTypes = AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(a =>
        {
            try { return a.GetTypes(); }
            catch { return Type.EmptyTypes; }
        })
        .Where(t => typeof(IRule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract && t.Namespace?.StartsWith("CivilQc") == true)
        .ToArray();

    [Fact]
    public void AllRuleImplementations_HaveRuleId()
    {
        foreach (var type in RuleTypes)
        {
            var instance = (IRule)Activator.CreateInstance(type)!;
            Assert.False(string.IsNullOrEmpty(instance.RuleId),
                $"{type.Name} has empty RuleId");
            Assert.False(string.IsNullOrEmpty(instance.Name),
                $"{type.Name} has empty Name");
        }
    }

    [Fact]
    public void AllRuleImplementations_HaveUniqueRuleId()
    {
        var ids = RuleTypes
            .Select(t => ((IRule)Activator.CreateInstance(t)!).RuleId)
            .ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void AllRuleImplementations_ImplementIRule()
    {
        Assert.True(RuleTypes.Length >= 12,
            $"Expected at least 12 rule implementations, found {RuleTypes.Length}");
    }
}
