# Implemented Systems

This file separates what is real from what is partial, placeholder, debug-only, or deprecated.

## Main Menu

Status: Real

Main menu flow exists and can start a new game into the town/dungeon loop. Gameplay HUD location text should be hidden while in main menu context.

## Town Hub

Status: Partial

TownHub exists as the outpost context. Runtime kiosks/services are functional graybox placeholders, not final buildings, interiors, signage, or town progression.

## Runtime Town Kiosks / Services

Status: Partial

Runtime service construction handles Blacksmith, Quartermaster/shop-style services, Saloon/Inn-style service, Bounty Board, and labels/layout. The final art, interiors, unlock progression, and economy depth are not complete.

## Dungeon Generation Current State

Status: Partial

The dungeon runtime builds playable floor layouts with rooms, connections, stairs, encounter placement, room purpose metadata, minimap data, and validation tests. It is still a simple/grid-style baseline and not the planned overhaul.

## World-Floor Architecture

Status: Partial

World-floor definitions, progression state, stable seeds, boss-clear unlock data, settlement/labyrinth/teleport tracking, and HUD location context exist. The actual overworld generator, terrain, field zones, boss gates, and teleport UI are deferred.

## Minimap

Status: Partial

Minimap, circular presentation, player marker, room data, bounty/stair markers, and full-map behavior exist. Final masking/pan/zoom/readability polish should continue in later UI passes.

## Compass

Status: Real

Compass HUD exists and supports dungeon/town navigation context.

## HUD

Status: Partial

Health, Stamina, Focus, XP, gold/reputation/skill-point style readouts, location labels, hints, dash state, minimap, and weapon HUD exist. Visual alignment is still being tuned.

## Weapon HUD

Status: Partial

Weapon name/category, loaded/magazine text, chamber pips, and reload/chamber state exist. Basic reserve ammo is intentionally not shown for Gunslinger gameplay.

## Revolver

Status: Partial

Frontier Revolver firing, reload timing, chamber state, viewmodel pose, and runtime material fallback exist. Material readability still needs manual confirmation and likely a dedicated material reality pass.

## Rifle / Inventory Weapons

Status: Partial

Weapon definitions and inventory hooks exist. Rifle behavior should be treated as prototype unless recently verified.

## Infinite / Basic Ammo Policy

Status: Real

Basic reserve ammo is dormant for current Gunslinger play. Reload/chambers matter; running out of basic reserve ammo should not block normal play. Serialized reserve fields remain for compatibility and future special-ammo work.

## Health / Stamina / Focus

Status: Real

Runtime resources exist. Gunslinger uses Stamina and Focus. Mana remains a future class resource and should not be forced into Gunslinger HUD.

## Dash

Status: Real

Dash/stamina behavior and HUD readiness feedback exist.

## Pistol Whip

Status: Partial

Pistol whip exists as a close-range action and routes through combat feedback, but it is not a final melee-combat system.

## Damage Numbers

Status: Partial

Damage feedback routes through the shared feedback service with screen-space marker work. Manual readability validation remains important.

## Pickups

Status: Partial

Gold/health/pickup routing exists. Basic ammo pickup code remains as dormant compatibility/future hook and should not be active in current loot tables.

## Room Purpose Interactables

Status: Partial

Room purpose rewards/interactables exist for prototype reward moments. They should not grant or mention basic ammo under the current infinite-ammo policy.

## Descent Rewards

Status: Partial

Reward choice and descent reward scaffolding exists. Full loot/itemization is not implemented.

## Reputation

Status: Partial

Reputation service/readout exists. Reputation unlocks and town progression depth are future work.

## Bounties

Status: Partial

Bounty board/selection/markers/target routing exist. Target presentation, unique identity, and quest readability are not final.

## Enemy Roster Foundation

Status: Partial

VS-1.3.1 added taxonomy, 24 active graybox enemies, floor bands, spawn availability, primitive silhouettes, and metadata. Many attack families are still placeholder/tuned fallback behavior.

## Slime Retirement

Status: Real

Slime and SpitterSlime remain debug/compatibility content only. Normal pools, packs, and active bounties should exclude them.

## Enemy Packs

Status: Partial

Data-driven pack templates exist and can influence room composition. Pack feel, behavior synergy, and spawn spacing need ongoing validation.

## Save / Profile State

Status: Partial

Profile/run state, class XP, reputation, inventory hooks, and compatibility fields exist. Long-term save migration policy is not complete.

## ProjectSnapshot / Export Tooling

Status: Partial

Snapshot docs and lightweight export scripts exist. VS-1.3.1D formalizes the project memory system and standard AI handoff export.
