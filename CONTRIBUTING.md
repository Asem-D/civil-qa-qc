# Contributing to civil-qa-qc

Thanks for your interest in contributing! This project is early-stage and there's a lot of room for improvement.

## Ways to Contribute

### Add a Rule

The most impactful contribution. Rules implement the `IRule` interface:

1. Create a new class in `src/CivilQc.Rules/`
2. Implement `IRule` (see existing rules for examples)
3. Add a YAML configuration entry in `rules/default.yaml`
4. Add tests in `tests/CivilQc.Tests/`
5. Submit a PR

Rules are auto-discovered via reflection, so no registration needed.

### Report Bugs

Open an issue with:
- What you expected
- What happened
- Civil 3D version
- .NET version (`dotnet --info`)

### Suggest Features

Open an issue describing the use case, not just the feature. "I need to check X because Y" is more useful than "Add X check."

## Development Setup

**Prerequisites:**
- .NET 8 SDK
- Civil 3D 2023-2025 installed
- `accoreconsole.exe` on PATH or set `ACCORECONSOLE_PATH`

```bash
# Clone
git clone https://github.com/Asem-D/civil-qa-qc.git
cd civil-qa-qc

# Build
dotnet build

# Test
dotnet test
```

**Note:** Rules and Plugin projects require AutoCAD DLLs to compile. If you don't have Civil 3D installed, the CI pipeline uses stubs to verify compilation. For local development with Civil 3D, the real APIs are used automatically.

## Pull Requests

- Keep PRs focused: one rule or one fix per PR
- Include tests for new rules
- Follow existing code style (C# conventions, XML docs on public members)
- Update `rules/default.yaml` if your rule has configurable parameters

## Code Style

- C# with XML documentation on public APIs
- Descriptive variable names over comments
- Rules should be stateless (no side effects between calls)

## Rule Wishlist

These are checks that users have requested or that would add significant value. Pick one and claim it by opening an issue:

### High Priority
- **LAYER-004: Layer color standards** — Validate that layers use approved color indices (e.g., standard ACI palette)
- **LAYER-005: Layer linetype standards** — Check that layers use approved linetypes (Continuous, Dashed, Center, etc.)
- **ANNO-003: Dimension style consistency** — Validate dimension styles against project standards
- **BLOCK-003: Block insertion point** — Detect blocks inserted at origin (0,0,0) which often indicates copy/paste errors
- **DRAW-004: Sheet index** — Compare layout tabs against expected sheet list from a standards file

### Medium Priority
- **TEXT-001: MText formatting** — Detect inconsistent text formatting (mixed fonts, heights within a drawing)
- **PIPE-001: Pipe network connectivity** — Check for disconnected pipe network segments
- **SURF-001: Surface complexity** — Report triangle count and flag surfaces that may be too dense for performance
- **CORR-001: Corridor frequency** — Validate that corridor frequency values are within acceptable ranges

### Nice to Have
- **XREF-001: Xref layer standards** — Validate that xref layers follow the same naming conventions as the host drawing
- **UTIL-001: Unused blocks** — Find block definitions that are never inserted
- **UTIL-002: Duplicate blocks** — Detect blocks with identical geometry but different names
- **DWG-002: Drawing statistics** — Comprehensive summary: entity counts, layer usage, block usage, xref tree

Have an idea not listed here? Open an issue and describe the check you need!

## License

By contributing, you agree your code is licensed under MIT.
