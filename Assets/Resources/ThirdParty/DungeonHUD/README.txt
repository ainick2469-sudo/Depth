DungeonHUD Starter Pack v0.1
Dark Fantasy Western Dungeon Crawler HUD Assets
================================================

This starter pack contains the first 10 mandatory Unity-ready HUD assets generated from the requested dark fantasy western dungeon-crawler UI brief.

Folder layout
-------------
Assets/DungeonHUD/
  Panels/                  Game-ready panel PNGs
  Bars/                    Game-ready status bar PNGs
  Slots/                   Game-ready inventory slot PNGs
  Buttons/                 Reserved for the next batch
  Icons/
    Weapons/               Reserved for weapon icons
    Ammo/                  Reserved for ammo icons
    Items/                 Reserved for item icons
    Abilities/             Reserved for gunslinger ability icons
    StatusEffects/         Reserved for status icons
    Map/                   Game-ready minimap frame
    QuestBounty/           Reserved for quest/bounty icons
  Decor/                   Reserved for decorative UI trim
  Source/
    OriginalGenerated/     Raw generated PNGs, kept for reference
    TransparentHighRes/    Cleaned high-resolution transparent PNGs
  UnityReady/              Mirrored game-ready assets for direct import
  README.txt
  QUICKSTART.md
  asset_manifest.json

Included assets
---------------
1. Panels/inventory_panel_frame_large.png
2. Panels/character_stat_panel_frame.png
3. Panels/quest_tracker_panel_frame.png
4. Panels/weapon_ammo_panel_frame.png
5. Bars/health_bar_frame_fill.png
6. Bars/mana_bar_frame_fill.png
7. Bars/stamina_grit_bar_frame_fill.png
8. Slots/inventory_slot_empty.png
9. Slots/inventory_slot_selected.png
10. Icons/Map/circular_minimap_frame.png

Suggested Unity HUD layout
--------------------------
Top-left:
- health_bar_frame_fill.png
- mana_bar_frame_fill.png
- stamina_grit_bar_frame_fill.png

Top-right:
- circular_minimap_frame.png

Left side:
- quest_tracker_panel_frame.png

Bottom-right:
- weapon_ammo_panel_frame.png

Inventory/menu screen:
- inventory_panel_frame_large.png
- inventory_slot_empty.png
- inventory_slot_selected.png
- character_stat_panel_frame.png

Recommended Unity import settings
---------------------------------
For all PNGs:
- Texture Type: Sprite (2D and UI)
- Sprite Mode: Single
- Alpha Source: Input Texture Alpha
- Alpha Is Transparency: ON
- Mesh Type: Full Rect for panels/bars; Tight can be tested for icons/slots
- Filter Mode: Bilinear for most HUD use
- Compression: None or High Quality
- Generate Mip Maps: OFF for normal screen-space Canvas UI

Suggested Max Size:
- Large panels: 4096
- Medium panels/bars: 2048
- Slots/buttons: 512 or 1024
- Minimap frame: 1024 or 2048 depending on HUD scale

9-slice notes
-------------
Use Unity Image Type: Sliced for:
- inventory_panel_frame_large.png
- character_stat_panel_frame.png
- quest_tracker_panel_frame.png
- weapon_ammo_panel_frame.png

Suggested starting border values:
- Large panels: 96 to 160 px
- Quest tracker: 80 to 120 px
- Weapon/ammo panel: 80 to 128 px

Do not 9-slice the filled bar versions unless you split the frame and fill later.

Better production version:
For final HUD bars, split each into:
- frame only
- fill only
- mask
- glow overlay

Naming conventions
------------------
Use lowercase snake_case:
category_asset_state_variant.png

Examples:
inventory_slot_empty.png
inventory_slot_selected.png
health_bar_frame_fill.png
circular_minimap_frame.png

Quality notes
-------------
These assets were generated as high-detail starter art and processed into PNGs with alpha transparency.

Honest production critique:
- The visual style is strong and cohesive.
- The panels are ornate, so some manual cleanup may be needed for perfect 9-slicing.
- The bars include frame and fill together; final HUD implementation should separate those layers.
- The quest tracker has parchment row shapes baked in, which is useful for prototype but less flexible long-term.
- The selected slot works, but future state variants should push silhouette differences harder.

Recommended next batch
----------------------
1. Health bar frame only
2. Health bar fill only
3. Mana bar frame only
4. Mana bar fill only
5. Stamina bar frame only
6. Stamina bar fill only
7. Hotbar slot normal
8. Hotbar slot selected
9. Ammo slot
10. Potion slot
