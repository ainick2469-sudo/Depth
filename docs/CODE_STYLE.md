# Code Style

## General
- Prefer small, readable classes over giant mixed-responsibility files.
- Follow existing naming conventions and namespaces.
- Use ASCII unless the file already needs another character set.
- Favor explicit wiring and stable references over scene lookups in `Update`.
- Use comments sparingly and only where they save real parsing effort.

## Unity-Specific
- UI displays state; UI should not own gameplay rules.
- Runtime mutable state should not live in assets.
- Keep editor-only code out of runtime assemblies.
- Prefer deterministic helper methods over hidden scene state.
- Generated objects must use clear names for debugging.

## File Placement
- Put new code in the subsystem folder that owns the behavior.
- Do not broad-restructure folders during the current milestone.
- Do not add third-party packages for this milestone.

## Safety
- No silent fallbacks when required geometry is broken.
- Validation failures should log seed, floor, node, template, and reason.
