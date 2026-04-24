# Decision 0002: Keep the Graph-First Dungeon Architecture

## Status
Accepted

## Context
The project already has a graph-first dungeon foundation:
- logical node/edge layout
- template selection
- runtime geometry rendering
- path-based special-room assignment

## Decision
Keep the graph-first dungeon architecture and stabilize it rather than replacing it.

## Why
- it matches the game’s procedural goals
- it supports validation and deterministic seed debugging
- replacing it now would derail the milestone

## Consequences
- current work focuses on validation, rendering correctness, and reachability
- no broad dungeon architecture rewrite during this milestone
