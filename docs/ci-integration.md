# CI/CD Integration

Run civil-qa-qc automatically in your build pipeline to catch drawing issues before they reach production.

## GitHub Actions

```yaml
# .github/workflows/drawing-qa.yml
name: Drawing QA/QC

on:
  push:
    paths:
      - '**/*.dwg'
  pull_request:
    paths:
      - '**/*.dwg'

jobs:
  qa-check:
    runs-on: windows-latest

    steps:
      - uses: actions/checkout@v4

      - name: Install Civil 3D
        # Use your organization's Civil 3D installation method
        # This may be a custom action, chocolatey, or MSI install
        run: |
          # Example: silent install from network share
          Start-Process msiexec.exe -ArgumentList '/i "\\server\civil3d\Civil3D.msi" /qn /norestart' -Wait

      - name: Run QA/QC checks
        run: |
          civil-qc check drawing.dwg --format both --rules rules/project-standards.yaml

      - name: Upload report
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: qa-report
          path: '*.civil-qc.*'
```

## Azure DevOps

```yaml
# azure-pipelines.yml
trigger:
  paths:
    include:
      - '**/*.dwg'

pool:
  vmImage: 'windows-latest'

steps:
  - script: civil-qc check $(Build.SourcesDirectory)/**/*.dwg --format both
    displayName: 'Run QA/QC checks'

  - task: PublishBuildArtifacts@1
    inputs:
      pathToPublish: '$(Build.SourcesDirectory)/**/*.civil-qc.*'
      artifactName: 'qa-reports'
    condition: always()
```

## Batch Mode

For checking multiple drawings at once, use a script:

```powershell
# check-all.ps1
$drawings = Get-ChildItem -Path . -Filter "*.dwg" -Recurse
$rules = "C:\Standards\project-rules.yaml"

foreach ($dwg in $drawings) {
    Write-Host "Checking: $($dwg.Name)"
    civil-qc check $dwg.FullName --rules $rules --format both
}

Write-Host "All drawings checked. Reports saved next to each drawing."
```

## Interpreting Results in CI

The tool exits with code 0 if all rules pass. To fail the build on warnings or errors, parse the JSON output:

```powershell
$json = Get-Content "drawing.civil-qc.json" | ConvertFrom-Json
$failures = $json.Results | Where-Object { -not $_.Passed }

if ($failures.Count -gt 0) {
    Write-Error "$($failures.Count) QA/QC issue(s) found"
    exit 1
}
```

## Tips

- **Cache Civil 3D installation** to speed up pipeline runs
- **Use `--rules`** with project-specific standards for consistent enforcement
- **Upload reports as artifacts** for easy access without digging through logs
- **Run on a schedule** (e.g., nightly) for drawings that change frequently
