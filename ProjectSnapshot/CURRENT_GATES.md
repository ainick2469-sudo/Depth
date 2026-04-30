# Current Gates

## Latest Stable Gameplay Base

- `4dbb4de Gate VS-1.4.1E.1: fix shell floor alignment and descent state regressions`

## Current Gate

- `Gate VS-1.4.1F: Labyrinth Layout Quality Foundation`

Purpose:

- Add layout-quality reporting for rooms, corridors, branches, landmarks, merge candidates, and warnings.
- Populate structural layout metadata for main path, branches, dead ends, protected rooms, landmarks, and future boss approach.
- Tune room template size weighting so deeper floors can produce more varied/larger room silhouettes without rewriting generation.
- Add metadata-only merge candidates for future merged-room work; geometry merging is still deferred.
- Tighten special-room distribution warnings and branch/dead-end preference while preserving secret-room non-reveal rules.

## Next Planned Gate

- `Gate VS-1.4.2: Graybox World Floor Runtime`

Begin the SAO-style world-floor runtime wrapper after the current labyrinth foundation is stable.
