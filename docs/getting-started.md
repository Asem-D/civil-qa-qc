# Getting Started

## Prerequisites

Before using civil-qa-qc, you need:

1. **Civil 3D** (2020-2025) installed on the machine
2. **.NET 8 SDK** (for building from source) or the [prebuilt release](https://github.com/Asem-D/civil-qa-qc/releases)
3. **accoreconsole.exe** accessible on your PATH or via the `ACCORECONSOLE_PATH` environment variable

## Installation

### Option 1: Download a release (recommended)

1. Go to [GitHub Releases](https://github.com/Asem-D/civil-qa-qc/releases)
2. Download the latest `.zip` for your Civil 3D version
3. Extract to a folder (e.g., `C:\Tools\civil-qa-qc`)
4. Add the folder to your PATH, or set `ACCORECONSOLE_PATH` to point to your `accoreconsole.exe`

### Option 2: Build from source

```bash
git clone https://github.com/Asem-D/civil-qa-qc.git
cd civil-qa-qc
dotnet build -c Release
```

The built executable will be at `src/CivilQc.Cli/bin/Release/net8.0-windows/civil-qc.exe`.

## Finding accoreconsole

The tool needs `accoreconsole.exe` to run checks. It looks in this order:

1. `--accoreconsole` CLI flag (if added in future)
2. `ACCORECONSOLE_PATH` environment variable
3. Default Civil 3D 2025 path: `C:\Program Files\Autodesk\AutoCAD 2025\accoreconsole.exe`

If you have multiple Civil 3D versions, set the environment variable:

```powershell
# PowerShell
$env:ACCORECONSOLE_PATH = "C:\Program Files\Autodesk\AutoCAD 2024\accoreconsole.exe"

# Or permanently via System Properties > Environment Variables
```

## Your First Check

Run the tool against any `.dwg` file:

```bash
civil-qc check drawing.dwg
```

This will:
1. Load the default rules (12 checks)
2. Launch Civil 3D in headless mode
3. Run all enabled checks against the drawing
4. Generate an HTML report next to the drawing

The output looks like:

```
Civil QC v0.1.0
Drawing: C:\Projects\Corridor-1a.dwg

Loaded 12 rules (12 enabled)
Launching Civil 3D (headless)...
Report: C:\Projects\Corridor-1a.civil-qc.html
```

## Understanding the Report

Open the generated `.html` file in any browser. The report shows:

- **Summary cards**: Total passed, critical, error, warning, and info counts
- **Results table**: Each rule's status (PASS/WARNING/ERROR), the rule name, severity, and a message explaining the finding
- **Screenshots** (if captured): Visual evidence of issues

## Common First-Run Issues

| Problem | Solution |
|---------|----------|
| `accoreconsole.exe not found` | Set `ACCORECONSOLE_PATH` to your Civil 3D install directory |
| `Drawing not found` | Check the file path. Use absolute paths to avoid confusion |
| `Plugin did not output` | The drawing may be corrupt. Open it in Civil 3D and run RECOVER first |
| `Exit code 53` | Drawing has internal errors. Open in Civil 3D GUI, run AUDIT and RECOVER |

## Next Steps

- [Configure rules](configuration.md) to match your project standards
- [Write custom rules](custom-rules.md) for your team's specific checks
- [Integrate with CI/CD](ci-integration.md) for automated checking
