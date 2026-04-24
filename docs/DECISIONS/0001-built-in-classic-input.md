# Decision 0001: Stay on Built-In Render Path and Classic Input

## Status
Accepted

## Context
The project is already running in a graybox prototype state. The current milestone is about stabilizing dungeon geometry and proving the core combat/reward loop.

## Decision
Keep the Built-In render path and classic input for this milestone.

## Why
- avoids pipeline churn during a gameplay-critical milestone
- avoids shader/material migration before art import
- reduces surface area while geometry, combat, and reward systems are still unstable

## Consequences
- visual polish stays limited for now
- art import is deferred until the loop is proven
- a later milestone can revisit URP/Input System deliberately
