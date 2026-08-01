# LinkedIn Post Draft: civil-qa-qc Release (v2)

---

Automated QA/QC for Civil 3D drawings exists inside the software. But running it headless, from a CLI, across hundreds of files, without touching the GUI? That part was missing.

So I built [civil-qa-qc](https://github.com/Asem-D/civil-qa-qc).

🔧 **The Problem**

Every Civil 3D team I've worked with handles standards checking the same way: open each file, check layers, verify units, inspect xrefs, repeat 50 times. Autodesk's built-in Batch Standards Checker and the Standardized Data Tool help, but they still need the Civil 3D GUI and don't cover everything teams actually need to validate.

📊 **What civil-qa-qc Does**

A headless CLI tool that:

- Spawns Civil 3D via `accoreconsole` (no GUI required)
- Runs configurable YAML rules against .dwg files
- Generates HTML/JSON reports with pass/warn/fail results
- Checks: layer naming, empty layers, unused layers, drawing units, xref status, proxy objects, file size

Batch process an entire project folder in one command.

🏗️ **Why I Built It**

I wanted something that fits into a CI/CD pipeline or runs overnight on a server, with rules I can customize per project. The tool is MIT-licensed and pure .NET 8 C#. The rules are extensible: implement `ICheckRule` in C# to add your own checks.

Note: civil-qa-qc requires a Civil 3D installation (it uses `accoreconsole` under the hood). It's not replacing Civil 3D, it's automating the checks you'd otherwise do manually.

💡 **What's Next**

v0.1.0 is live with 7 built-in rules. Roadmap includes:

- More rules (annotation standards, coordinate system validation)
- Web dashboard for report browsing
- Community-contributed rule plugins

---

I'm looking for beta testers. If you manage Civil 3D standards across teams, I'd love to hear which rules matter most to your workflow.

What's the QA/QC check that takes you the most time today? 👇

---

#Civil3D #BIM #AECIndustry #OpenSource #InfrastructureManagement
