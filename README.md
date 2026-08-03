# civil-qa-qc

Open-source CLI tool for automated QA/QC of Civil 3D drawings. Runs configurable checks against your `.dwg` files and generates HTML/JSON reports.

> **Requires Civil 3D** (2023-2025) installed on the machine. The tool uses `accoreconsole.exe` to run checks headlessly — it automates what you'd otherwise do manually in the Civil 3D GUI.

## Architecture

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

## Projects

| Project | Target | Purpose |
|---|---|---|
| `CivilQc.Cli` | .NET 8 | CLI entry point, report generation |
| `CivilQc.Engine` | .NET 8 | Rule loading (YAML), accoreconsole host, report generation |
| `CivilQc.Models` | .NET 8 | Shared data models (CheckResult, RuleDefinition, ReportData) |
| `CivilQc.Rules` | .NET 8 + .NET 4.8 | IRule interface + built-in rule implementations |
| `CivilQc.Plugin` | .NET 8 + .NET 4.8 | Plugin DLL loaded by accoreconsole |
| `CivilQc.Ai` | .NET 8 | AI-powered rule generation and batch summarization (optional, BYOK) |
| `CivilQc.Tests` | .NET 8 | Unit tests (xUnit) |

## Multi-version Support

| Branch | .NET Target | Civil 3D Version |
|---|---|---|
| `main` | .NET 8 | Civil 3D 2025+ |
| `release/2024` | .NET Framework 4.8 | Civil 3D 2020-2024 |

## Built-in Rules

| ID | Check Type | Class | Severity | Description |
|----|-----------|-------|----------|-------------|
| `PERF-001` | `FileSize` | `FileSizeRule` | Info | Reports file size, warns at configurable thresholds |
| `LAYER-001` | `LayerNaming` | `LayerNamingRule` | Warning | Validates layer names against allowed prefixes (configurable) |
| `LAYER-002` | `EmptyLayers` | `EmptyLayersRule` | Info | Finds layers with zero entities |
| `LAYER-003` | `UnusedLayers` | `UnusedLayersRule` | Info | Finds frozen/off layers with zero entities |
| `DRAW-001` | `DrawingUnits` | `DrawingUnitsRule` | Warning | Reports INSUNITS; enforces expected unit (configurable) |
| `DRAW-002` | `XrefStatus` | `XrefStatusRule` | Warning | Reports xref resolution status, flags missing references |
| `DRAW-003` | `ProxyObjects` | `ProxyObjectsRule` | Warning | Counts proxy entities, flags unsupported third-party objects |
| `ANNO-001` | `AnnotationScale` | `AnnotationScaleRule` | Warning | Detects annotative entities, flags those in ModelSpace |
| `ANNO-002` | `TextStyle` | `TextStyleRule` | Warning | Validates text styles against naming standards |
| `BLOCK-001` | `BlockNaming` | `BlockNamingRule` | Warning | Checks block names against prefix/suffix/naming rules |
| `BLOCK-002` | `DynamicBlock` | `DynamicBlockRule` | Info | Reports dynamic blocks and their properties |
| `DWG-001` | `DrawingRecovery` | `DrawingRecoveryRule` | Critical | Detects recovery mode and audit status |

## Usage

```bash
# Run with default rules
civil-qc check drawing.dwg

# Custom rules file
civil-qc check drawing.dwg --rules custom-rules.yaml

# Both HTML and JSON output
civil-qc check drawing.dwg --format both --verbose

# Specify output location
civil-qc check drawing.dwg --output report.html --screenshots ./shots

# Include AI-powered fix suggestions in the report
civil-qc check drawing.dwg --ai-fix
```

### AI Commands (Optional)

```bash
# Generate rules from a natural language description
civil-qc ai generate-rules --description "All layers must start with discipline prefix C-, S-, E-"

# Generate rules from a standards document
civil-qc ai generate-rules --file company-standards.txt --output rules/company.yaml

# Summarize batch results
civil-qc ai summarize --input ./results/ --output summary.md
```

## Custom Rules

Create a `rules/custom.yaml`:

```yaml
rules:
  - id: CUSTOM-001
    name: My custom check
    check_type: LayerNaming
    severity: Error
    parameters:
      min_length: 3
```

## Extending

Drop a new `IRule` implementation into `CivilQc.Rules`:

```csharp
public class MyCustomRule : IRule
{
    public string RuleId => "CUSTOM-001";
    public string Name => "My Custom Check";

    public List<CheckResult> Execute(RuleDefinition rule, DrawingContext context)
    {
        // Your check logic here
        return new List<CheckResult> { /* ... */ };
    }
}
```

The rule engine auto-discovers `IRule` implementations via reflection.

## Rule Configuration

Rules are configured via `rules/default.yaml` (copied to the CLI output directory at build time).
Each rule entry supports these fields:

```yaml
- id: LAYER-001            # Unique ID (matches CheckType class discovery)
  name: Layer naming        # Human-readable name
  check_type: LayerNaming   # Maps to IRule.RuleId for class resolution
  severity: Warning         # Info | Warning | Error | Critical
  enabled: true             # Set false to skip this rule
  description: >-           # Optional description
    Checks layer naming conventions.
  parameters:               # Rule-specific key/value parameters
    allowed_prefixes: [CIVIL, SURF, ROAD]
    require_prefix: true
    separator: "-"
```

### Parameters by Rule

| Rule | Parameter | Type | Default | Description |
|------|-----------|------|---------|-------------|
| `PERF-001` | `warning_mb` | int | 100 | File size warning threshold (MB) |
| `PERF-001` | `error_mb` | int | 500 | File size error threshold (MB) |
| `LAYER-001` | `allowed_prefixes` | list | `[]` | Allowed layer name prefixes |
| `LAYER-001` | `require_prefix` | bool | `true` | Fail layers without a prefix |
| `LAYER-001` | `separator` | string | `"-"` | Delimiter between prefix and suffix |
| `LAYER-002` | `exclude_defaults` | bool | `true` | Skip "0" and "Defpoints" |
| `LAYER-003` | `exclude_defaults` | bool | `true` | Skip "0" and "Defpoints" |
| `DRAW-001` | `expected` | string | `""` | Expected unit (e.g. "Meters"); empty = report only |
| `DRAW-002` | `fail_on_missing` | bool | `true` | Mark as failed when xrefs are missing |
| `DRAW-002` | `warn_on_overlay` | bool | `false` | Flag overlay-type xrefs |
| `DRAW-003` | `max_count` | int | `0` | Max proxy objects allowed; 0 = report only |

## AI Configuration

AI features are **entirely optional**. The tool works fully without an API key. Bring Your Own Key (BYOK) if you want AI-powered rule generation or batch summarization.

Provide your API key in one of three ways (highest precedence first):

1. **CLI flag**: `--api-key <key>`
2. **Environment variable**: `CIVIL_QC_AI_KEY`
3. **Config file**: `~/.civil-qa-qc/config.json`

```json
{
  "ai": {
    "api_key": "sk-...",
    "api_base": "https://openrouter.ai/api/v1",
    "model": "anthropic/claude-sonnet-4"
  }
}
```

You can also override the API base URL (`--api-base`) and model (`--model`) via flags, environment variables, or the config file. Works with OpenRouter, OpenAI, Ollama, or any OpenAI-compatible endpoint.

## Requirements

- .NET 8 SDK (for building)
- Civil 3D 2023-2025 installed (for running checks)
- `accoreconsole.exe` accessible on PATH or via `ACCORECONSOLE_PATH` env var

## Roadmap

See [ROADMAP.md](ROADMAP.md) for planned features and version timeline.

**Current**: v0.2.0 (12 rules, .NET 8)
**Next**: v0.3.0 (multi-version .NET, improved reports)

## Contributing

Contributions welcome! See the roadmap for priority areas.

1. Fork the repo
2. Create a feature branch
3. Add your rule implementing `IRule`
4. Submit a PR

## License

MIT License - see [LICENSE](LICENSE) for details.
