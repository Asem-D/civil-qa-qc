# Configuration

civil-qa-qc uses YAML files to define which checks run and how they behave.

## Default Configuration

When no `--rules` flag is provided, the tool loads `rules/default.yaml` from the installation directory. This file contains all 12 built-in rules with sensible defaults.

## Custom Rules File

Create your own YAML file and pass it with `--rules`:

```bash
civil-qc check drawing.dwg --rules my-standards.yaml
```

## YAML Structure

```yaml
rules:
  - id: LAYER-001          # Unique ID (must match a CheckType class)
    name: Layer naming      # Human-readable name
    check_type: LayerNaming # Maps to IRule implementation
    severity: Warning       # Info | Warning | Error | Critical
    enabled: true           # Set false to skip this rule
    description: >-         # Optional description
      Checks layer naming conventions.
    parameters:             # Rule-specific key/value parameters
      allowed_prefixes: [CIVIL, SURF, ROAD]
      require_prefix: true
      separator: "-"
```

## Severity Levels

| Level | Meaning | Report Impact |
|-------|---------|---------------|
| `Critical` | Drawing is broken or unusable | Red badge, blocks delivery |
| `Error` | Significant issue that must be fixed | Orange badge |
| `Warning` | Issue that should be addressed | Yellow badge |
| `Info` | Informational, no action required | Blue badge |

## Enabling/Disabling Rules

Set `enabled: false` to skip a rule entirely:

```yaml
rules:
  - id: PERF-001
    name: File size
    check_type: FileSize
    severity: Info
    enabled: false    # This rule will be skipped
```

## Rule Parameters by Check Type

### PERF-001: File Size

Reports drawing file size and warns at configurable thresholds.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `warning_mb` | int | 100 | File size warning threshold (MB) |
| `error_mb` | int | 500 | File size error threshold (MB) |

```yaml
- id: PERF-001
  name: File size
  check_type: FileSize
  severity: Info
  parameters:
    warning_mb: 50
    error_mb: 200
```

### LAYER-001: Layer Naming Convention

Validates layer names against a list of allowed prefixes.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `allowed_prefixes` | list | `[]` | Allowed layer name prefixes |
| `require_prefix` | bool | `true` | Fail layers without a prefix |
| `separator` | string | `"-"` | Delimiter between prefix and suffix |

```yaml
- id: LAYER-001
  name: Layer naming convention
  check_type: LayerNaming
  severity: Warning
  parameters:
    allowed_prefixes:
      - CIVIL
      - SURF
      - ROAD
      - PIPE
    require_prefix: true
    separator: "-"
```

### LAYER-002: Empty Layers

Finds layers with zero drawn entities.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `exclude_defaults` | bool | `true` | Skip "0" and "Defpoints" |

### LAYER-003: Unused Layers

Finds layers that are frozen or turned off AND have zero entities.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `exclude_defaults` | bool | `true` | Skip "0" and "Defpoints" |

### DRAW-001: Drawing Units

Reads the INSUNITS system variable and compares against expected value.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `expected` | string | `""` | Expected unit (e.g., "Meters"). Empty = report only |

Supported unit names: Unitless, Inches, Feet, Miles, Millimeters, Centimeters, Meters, Kilometers, Yards.

### DRAW-002: Xref Status

Reports the status of all external references.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `fail_on_missing` | bool | `true` | Mark as failed when xrefs are missing |
| `warn_on_overlay` | bool | `false` | Flag overlay-type xrefs |

### DRAW-003: Proxy Objects

Counts proxy entities (placeholders for unsupported third-party objects).

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `max_count` | int | 0 | Max proxy objects allowed. 0 = report only |

### ANNO-001: Annotation Scale Consistency

Detects annotative entities and checks their distribution.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `fail_if_annotative_in_modelspace` | bool | `false` | Flag annotative objects in ModelSpace |

### ANNO-002: Text Style Standards

Validates text styles against an allowed list.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `allowed_styles` | list | `[]` | Allowed text style names |
| `check_fonts` | bool | `false` | Also validate font files |
| `allowed_fonts` | list | `[]` | Allowed font files |

### BLOCK-001: Block Naming Conventions

Checks block names against prefix/suffix rules.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `allowed_prefixes` | list | `[]` | Allowed block name prefixes |
| `forbidden_prefixes` | list | `["A$", "*U", "*D", "*X"]` | Block name prefixes that indicate problems |
| `require_prefix` | bool | `false` | Fail blocks without a prefix |
| `max_name_length` | int | 0 | Maximum block name length. 0 = no limit |

### BLOCK-002: Dynamic Block Validation

Checks that dynamic blocks have intact definitions.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `fail_on_broken` | bool | `true` | Mark as failed when dynamic block definition is empty |

### DWG-001: Drawing Recovery Status

Detects whether the drawing has been recovered or repaired.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `fail_on_recovery` | bool | `false` | Mark as failed when recovery indicators are found |

## Example: Project-Specific Configuration

```yaml
# my-project-standards.yaml
rules:
  # Enforce Meters for this project
  - id: DRAW-001
    name: Drawing units
    check_type: DrawingUnits
    severity: Error
    parameters:
      expected: "Meters"

  # Strict layer naming
  - id: LAYER-001
    name: Layer naming convention
    check_type: LayerNaming
    severity: Error
    parameters:
      allowed_prefixes: [CIVIL, SURF, ROAD, PIPE, UTIL]
      require_prefix: true

  # No proxy objects allowed
  - id: DRAW-003
    name: Proxy objects
    check_type: ProxyObjects
    severity: Error
    parameters:
      max_count: 0

  # Skip file size check (not relevant for this project)
  - id: PERF-001
    name: File size
    check_type: FileSize
    severity: Info
    enabled: false
```
