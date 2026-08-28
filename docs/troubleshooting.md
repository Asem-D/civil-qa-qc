# Troubleshooting

Common issues and how to fix them.

## accoreconsole not found

**Error:**
```
accoreconsole.exe not found at: ...
```

**Cause:** The tool can't find `accoreconsole.exe`.

**Fix:**
1. Verify Civil 3D is installed
2. Set the environment variable:
   ```powershell
   $env:ACCORECONSOLE_PATH = "C:\Program Files\Autodesk\AutoCAD 2025\accoreconsole.exe"
   ```
3. Or check the default path: `C:\Program Files\Autodesk\AutoCAD 2025\accoreconsole.exe`

## Drawing is corrupt (exit code 53)

**Error:**
```
accoreconsole exited with code 53
```

**Cause:** The drawing has internal errors that prevent accoreconsole from opening it.

**Fix:**
1. Open the drawing in Civil 3D GUI
2. Run `AUDIT` (type AUDIT in the command line, select "Fix errors")
3. Run `RECOVER` (type RECOVER, select the drawing)
4. Save the drawing
5. Try civil-qa-qc again

The tool will automatically retry with RECOVER mode if it detects this error.

## Plugin did not produce output

**Error:**
```
Plugin did not produce output. accoreconsole may have failed.
```

**Cause:** The plugin DLL failed to load or execute inside accoreconsole.

**Fix:**
1. Run with `--verbose` to see accoreconsole output
2. Check that `CivilQc.Plugin.dll` and `CivilQc.Rules.dll` are in the same directory as the CLI executable
3. Check the debug log at `%TEMP%\civil_qc_debug.log`

## Rules not found

**Error:**
```
Rules file not found: my-rules.yaml
```

**Fix:** Use an absolute path or ensure the file is in the current directory:
```bash
civil-qc check drawing.dwg --rules C:\Projects\my-rules.yaml
```

## AI features not working

**Symptom:** `--ai-fix` flag doesn't produce suggestions.

**Fix:**
1. Set your API key:
   ```powershell
   $env:CIVIL_QC_AI_KEY = "sk-your-api-key"
   ```
2. Or create `~/.civil-qa-qc/config.json`:
   ```json
   {
     "ai": {
       "api_key": "sk-your-api-key",
       "api_base": "https://openrouter.ai/api/v1",
       "model": "anthropic/claude-sonnet-4"
     }
   }
   ```

## Build fails on CI

**Error:** `The type or namespace name 'Autodesk' could not be found`

**Cause:** CI runners don't have Civil 3D installed.

**This is expected.** The project uses conditional compilation (`NO_AUTOCAD` stubs) to build on CI. If you see this error locally, ensure Civil 3D is installed and the AutoCAD DLLs are accessible.

## Report is empty

**Symptom:** The HTML report shows 0 results.

**Fix:**
1. Check that rules are enabled in your YAML configuration
2. Run with `--verbose` to see which rules are loaded
3. Check if the plugin executed successfully (look for debug log)

## Performance is slow

**Cause:** Large drawings with many entities take time to scan.

**Tips:**
- Disable rules you don't need by setting `enabled: false`
- Use `--rules` with a minimal configuration for quick checks
- The tool processes one drawing at a time; batch mode is on the roadmap

## FAQ

**Q: Does this work with AutoCAD (not Civil 3D)?**
A: No. The tool uses Civil 3D-specific APIs (`accoreconsole.exe`, `AeccDbMgd.dll`). It will not work with plain AutoCAD.

**Q: Do I need a Civil 3D license?**
A: Yes, for running checks. The CLI itself doesn't need a license, but `accoreconsole.exe` requires a valid Civil 3D license on the machine.

**Q: Can I run this on a server without Civil 3D?**
A: Not directly. You need Civil 3D installed on the machine running the checks. For CI/CD, use a Windows runner with Civil 3D installed.

**Q: How do I add a rule for my company's specific standards?**
A: See [Custom Rules](custom-rules.md). It's a C# class implementing `IRule` plus a YAML entry.

**Q: Can I contribute rules?**
A: Yes! See [CONTRIBUTING.md](../CONTRIBUTING.md) and the [Rule Wishlist](../CONTRIBUTING.md#rule-wishlist) for ideas.

**Q: What Civil 3D versions are supported?**
A: Civil 3D 2020-2025. The `.NET 8` build works with 2025+. The `.NET Framework 4.8` build works with 2020-2024.
