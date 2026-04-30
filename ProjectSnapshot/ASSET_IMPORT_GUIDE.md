# Asset Import Guide

Imported packs can be useful, but they must not destabilize the project. Never import Asset Store, Fab, or third-party packs directly into `main` first.

## Standard Flow

```powershell
git status
git switch -c spike/dungeon-generation-pack
# import pack in Unity
git status --short
git diff --stat
git diff --name-status
```

## Inspection Checklist

- Inspect new folders and asset sizes.
- Inspect `ProjectSettings` changes.
- Inspect `Packages` changes.
- Inspect scripts, namespaces, assembly definitions, and editor tools.
- Inspect demo scenes without moving them into production scenes.
- Check whether the pack expects a different input, render, networking, or scene architecture.

## Decisions After Import

- Ignore it entirely.
- Use it only as reference.
- Copy small ideas manually.
- Wrap useful pieces behind Frontier Depths interfaces.
- Integrate intentionally in a later gate.

## Rules

- Do not commit heavy assets unless explicitly approved.
- Do not let imported scripts own core architecture by accident.
- Do not replace current dungeon generation blindly.
- Do not commit scene YAML or ProjectSettings churn unless the gate explicitly requires it.
- Prefer spike branches and written notes over main-branch asset experiments.
