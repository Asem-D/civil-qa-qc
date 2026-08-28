---
name: Rule Request
about: Suggest a new QA/QC check for Civil 3D drawings
title: ''
labels: enhancement, rule-request
assignees: ''
---

## What should this rule check?

Describe the QA/QC check you'd like to see.

## Why does this matter?

What problem does this check solve? What goes wrong when drawings don't pass this check?

## Example violation

Describe a drawing scenario that should fail this check. If possible, include:
- Layer name pattern
- Block naming convention
- Drawing setting (units, scale, etc.)

## Desired parameters (optional)

Should this rule be configurable? What settings would you want?

```yaml
# Example rule configuration
- id: MY-RULE-001
  name: My custom check
  severity: Warning
  parameters:
    some_setting: value
```

## Are you willing to contribute this rule?

Rules are the easiest way to contribute. See [CONTRIBUTING.md](../../CONTRIBUTING.md) for how.
