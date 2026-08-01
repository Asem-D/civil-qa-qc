# Contributing to civil-qa-qc

Thanks for your interest in contributing! This project is early-stage and there's a lot of room for improvement.

## Ways to Contribute

### Add a Rule

The most impactful contribution. Rules implement the `ICheckRule` interface:

1. Create a new class in `src/CivilQc.Rules/`
2. Implement `ICheckRule` (see existing rules for examples)
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

## Pull Requests

- Keep PRs focused: one rule or one fix per PR
- Include tests for new rules
- Follow existing code style (C# conventions, XML docs on public members)
- Update `rules/default.yaml` if your rule has configurable parameters

## Code Style

- C# with XML documentation on public APIs
- Descriptive variable names over comments
- Rules should be stateless (no side effects between calls)

## License

By contributing, you agree your code is licensed under MIT.
