# Civil QC Roadmap

## Current Status: v0.1.0

**Released**: August 2026
**License**: MIT
**Platform**: .NET 8, Civil 3D 2025 (accoreconsole)

### Features
- CLI tool spawning headless Civil 3D via accoreconsole
- YAML-configurable rules
- HTML/JSON report generation
- AI-powered features (optional, BYOK):
  - `ai generate-rules`: Generate rule YAML from natural language or standards documents
  - `ai summarize`: Executive summary from batch QA/QC results
- 7 built-in rules:
  - PERF-001: File size check
  - LAYER-001: Layer naming conventions
  - LAYER-002: Empty layers
  - LAYER-003: Unused layers
  - DRAW-001: Drawing units
  - DRAW-002: Xref status
  - DRAW-003: Proxy objects

---

## v0.2.0 (Target: March 2027)

**Theme**: Compatibility & Core Rules

### Goals
- [ ] .NET Framework 4.8 target for Civil 3D 2020-2024 compatibility
- [ ] Multi-version build pipeline (main = .NET 8, release branches = .NET Framework)
- [ ] Add 5 new rules:
  - ANNO-001: Annotation scale consistency
  - ANNO-002: Text style standards
  - BLOCK-001: Block naming conventions
  - BLOCK-002: Dynamic block validation
  - DWG-001: Drawing recovery status
- [ ] Improved error messages for common failures
- [ ] JSON schema for report output (API integration)

### Success Criteria
- Builds on .NET Framework 4.8 without errors
- Runs on Civil 3D 2020, 2021, 2022, 2023, 2024, 2025
- 12+ rules passing
- 5 beta testers providing feedback

---

## v0.3.0 (Target: June 2027)

**Theme**: Reporting & Integration

### Goals
- [ ] Web dashboard for browsing reports (Blazor or static HTML)
- [ ] CI/CD integration guides (GitHub Actions, Azure DevOps)
- [ ] Rule severity levels (Critical, Error, Warning, Info)
- [ ] Custom rule configuration UI (simple web form)
- [ ] Batch mode improvements (parallel execution)
- [ ] Export to CSV/Excel for management reporting

### Success Criteria
- Dashboard renders reports from JSON
- CI/CD pipeline example working end-to-end
- 20+ rules available

---

## v1.0.0 (Target: September 2027)

**Theme**: Production Ready

### Goals
- [ ] Plugin architecture for community rules
- [ ] NuGet package for rule libraries
- [ ] Documentation site (GitHub Pages or ReadTheDocs)
- [ ] VS Code extension for rule authoring
- [ ] Enterprise features:
  - [ ] LDAP/AD integration for user management
  - [ ] Audit logging
  - [ ] Multi-tenant support
- [ ] Performance optimization (parallel drawing processing)
- [ ] Auto-update mechanism

### Success Criteria
- 3+ community-contributed rule packages
- 50+ rules available
- Documentation covers all features
- 10+ organizations using in production

---

## Future Ideas (Post v1.0)

### Advanced Features
- AI-powered drawing analysis (classify layer violations, detect anomalies)
- Integration with BIM 360 / Autodesk Construction Cloud
- Real-time collaboration on QA/QC reports
- Custom report templates (company branding)
- Mobile app for report viewing

### Ecosystem
- Rule marketplace for community sharing
- Integration with Procore, PlanGrid, etc.
- API for third-party tools
- Training courses and certification

---

## Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Priority Areas
1. **Rules**: Most needed - add checks for your team's standards
2. **Documentation**: Guides, tutorials, examples
3. **Testing**: Civil 3D version compatibility testing
4. **UI/UX**: Dashboard improvements, report design

---

## Feedback

Open an issue on [GitHub](https://github.com/Asem-D/civil-qa-qc/issues) or reach out on LinkedIn.

Your feedback shapes the roadmap. Tell me what rules matter most to your team.
