# LinkedIn Post Draft: civil-qa-qc Release

---

There's no open-source QA/QC tool for Civil 3D drawings. So I built one.

🔧 **The Problem**

Every Civil 3D team I've worked with has the same pain: checking drawings against standards is manual, tedious, and error-prone. You open each file, eyeball the layers, check units, verify xrefs. Repeat 50 times.

Commercial solutions exist (ARKANCE, Autodesk's upcoming Model Checker), but they're either closed-source, expensive, or still in beta.

📊 **What Civil QC Does**

I built [civil-qa-qc](https://github.com/Asem-D/civil-qa-qc) as a headless CLI tool that:

• Spawns Civil 3D via `accoreconsole` (no GUI required)
• Runs configurable YAML rules against .dwg files
• Generates HTML/JSON reports with pass/warn/fail results
• Checks: layer naming, empty layers, unused layers, drawing units, xref status, proxy objects, file size

Batch process an entire project folder in one command. No clicking through dialogs.

🏗️ **Why It Matters**

**Standards compliance shouldn't require a license fee.** The tool is MIT-licensed, pure .NET 8 C#, and designed for CI/CD integration. Run it on every commit. Run it overnight on your server. Run it before submission.

I'm using Civil 3D 2025's `accoreconsole` for headless execution, which means full Civil 3D engine capability without the GUI overhead.

💡 **What's Next**

v0.1.0 is live with 7 built-in rules. The architecture is extensible: add custom rules by implementing `ICheckRule` in C#. Roadmap includes:
• More rules (annotation standards, coordinate system validation)
• Web dashboard for report browsing
• Plugin architecture for community-contributed checks

---

I'm looking for beta testers. If you manage Civil 3D standards across teams, I'd love your feedback on what rules matter most to your workflow.

What's the QA/QC check that takes you the most time today? 👇

---

#Civil3D #BIM #AECIndustry #OpenSource #InfrastructureManagement
