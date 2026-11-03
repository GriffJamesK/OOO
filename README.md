# Unity Prototype Kit

A Unity 6 (6000.6.4f1, URP, 3D) template

It ships with a first-person controller, a pastel greybox kit built to real dimensions, Source-style movement (bunny hopping, air strafing, surfing) with slides and wall runs, and a walled obstacle-course Sandbox to test it in.

![Sandbox](docs/sandbox.png)

## 1. Host
1. Push this kit to GitHub:
   ```bash
   git remote add origin https://github.com/<owner>/unity-prototype-kit.git
   git push -u origin main
   ```
2. On GitHub, open **Settings > General** and tick **Template repository**.
3. `.gitignore` already keeps `Library/`, `Temp/`, `Logs/` and `obj/` out of the repo. Git LFS is used for binary art (`git lfs install` once per machine).

## 2. New prototype
1. On GitHub, click **Use this template**, or run:
   ```bash
   gh repo create <name> --template <owner>/unity-prototype-kit --private --clone
   ```
2. Open the clone once in Unity Hub with **6000.6.4f1** so `Library/` generates.
3. In Unity, open **Window > MCP for Unity > Connect** and click **Start Server** (transport: HTTP Local). Claude Code reaches it through `.mcp.json` as **UnityMCP**.
4. Open `Assets/_Project/Scenes/Boot.unity` and press Play to check that the kit works.

## 3. Brief
Write the one-paragraph brief into `docs/BRIEF.md`.

Optionally, drop reference images into `docs/references/` and add a section for each in `docs/references/REFERENCES.md`: what it's for, what to pay attention to, and what to ignore.

## 4. Start
Open Claude Code in the repo root and say:

> Read CLAUDE.md and docs/WORKFLOW.md, then run phase 1 on docs/BRIEF.md.

`CLAUDE.md` loads automatically. The workflow takes it from there and stops for owner review after the GDD.

## Useful commands
- Rebuild generated assets: Unity menu **Kit > Rebuild Kit Assets**.
- Offline compile check (Editor closed): `powershell -File Tools/compile-check.ps1`
- Offline tests (Editor closed): `powershell -File Tools/run-tests.ps1`
