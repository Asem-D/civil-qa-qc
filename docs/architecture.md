# Architecture

How civil-qa-qc works under the hood.

## Overview

civil-qa-qc is a .NET CLI tool that runs QA/QC checks against Civil 3D drawings. The key challenge: AutoCAD's API only works inside a running AutoCAD process. The solution: spawn a headless Civil 3D instance via `accoreconsole.exe` and load a plugin into it.

```
civil-qc CLI  ──spawns──>  accoreconsole.exe  ──loads──>  CivilQc.Plugin.dll
    │                                                         │
    │  (outside AutoCAD)                    (inside headless Civil 3D)
    │                                                         │
    │  1. Load YAML rules                     3. Run IRule.Execute()
    │  2. Launch accoreconsole                4. Screenshot failures
    │  6. Generate HTML report                5. Write JSON results
    │                                                         │
    └──────────── report.html/json <──────────────────────────┘
```

## Project Structure

```
civil-qa-qc/
├── src/
│   ├── CivilQc.Cli/         # CLI entry point, report generation
│   ├── CivilQc.Engine/      # Rule loading, accoreconsole host, report generation
│   ├── CivilQc.Rules/       # IRule interface + built-in rule implementations
│   ├── CivilQc.Plugin/      # Plugin DLL loaded by accoreconsole
│   └── CivilQc.Ai/          # AI-powered features (optional, BYOK)
├── tests/
│   └── CivilQc.Tests/       # Unit tests (xUnit)
├── rules/
│   └── default.yaml         # Default rule configuration
└── docs/                    # Documentation
```

### CivilQc.Cli

The command-line interface. Parses arguments, orchestrates the check pipeline, and generates reports. Uses [System.CommandLine](https://github.com/dotnet/command-line-api) for argument parsing.

### CivilQc.Engine

Core logic that doesn't depend on AutoCAD:

- **RuleLoader**: Reads YAML configuration into `RuleConfig` objects
- **RuleEngine**: Serializes rule configuration to JSON for the plugin, parses plugin output back to `ReportData`
- **AccoreHost**: Spawns `accoreconsole.exe` with a script that loads the plugin and runs checks
- **ReportGenerator**: Generates HTML and JSON reports from `ReportData`

### CivilQc.Rules

Contains the `IRule` interface and all built-in rule implementations. Each rule class:

1. Implements `IRule` (RuleId, Name, Execute)
2. Reads parameters from the YAML configuration
3. Accesses the AutoCAD drawing via `DrawingContext` and `AcadContext`
4. Returns a list of `CheckResult` objects

Rules are **auto-discovered via reflection** at runtime. No registration needed.

### CivilQc.Plugin

The DLL that runs inside `accoreconsole.exe`. It:

1. Reads rule configuration from a temp JSON file
2. Discovers all `IRule` implementations via reflection
3. Populates `DrawingContext` with AutoCAD API handles
4. Runs each rule and collects results
5. Writes results to a temp JSON file for the CLI to pick up

### CivilQc.Ai

Optional AI-powered features. Uses OpenAI-compatible APIs (OpenRouter, Ollama, etc.) for:

- **Rule generation**: Convert natural language descriptions to YAML rules
- **Fix suggestions**: Generate actionable fix recommendations when rules fail
- **Batch summarization**: Executive summaries from multiple report results

## Data Flow

1. **CLI** loads YAML rules via `RuleLoader`
2. **CLI** serializes rules + drawing path to a temp JSON file
3. **CLI** spawns `accoreconsole.exe` with a `.scr` script
4. **accoreconsole** loads `CivilQc.Plugin.dll` via NETLOAD
5. **Plugin** reads the temp JSON, discovers rules, runs them
6. **Plugin** writes results to a temp JSON file
7. **accoreconsole** exits
8. **CLI** reads the results JSON, generates HTML/JSON report
9. **CLI** cleans up temp files

## Multi-Version Support

| Branch | .NET Target | Civil 3D Version |
|--------|-------------|------------------|
| `main` | .NET 8 | Civil 3D 2025+ |
| `release/2024` | .NET Framework 4.8 | Civil 3D 2020-2024 |

The Plugin and Rules projects target both frameworks. On CI without Civil 3D installed, `NO_AUTOCAD` is defined and stub types satisfy the compiler.

## Key Design Decisions

**Why accoreconsole?** It's the official Autodesk headless mode. No GUI, no licensing issues, no hacks. It's how Autodesk recommends running AutoCAD unattended.

**Why YAML for rules?** YAML is human-readable, editable by non-developers, and supports comments. A project manager can customize rules without touching C# code.

**Why reflection for rule discovery?** Rules can live in external DLLs in the future. The plugin scans all loaded assemblies for `IRule` implementations, making it trivial to add rules without modifying the core.

**Why separate CLI and Plugin?** The CLI runs outside AutoCAD (no license needed). The Plugin runs inside AutoCAD (needs a license). Separating them means you only need one Civil 3D license per machine, and the CLI can generate reports from cached results.
