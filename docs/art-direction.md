# Art Direction — Floor 1: Town of Beginnings

**Status:** Decided · **Date:** 2026-06-14

## Decisions (locked)

| Axis | Choice |
|---|---|
| Setting & scope | **Floor 1 village**, framed as the ground floor of an Aincrad-style ascending tiered world (built incrementally — only Floor 1 now) |
| Art style | **Stylized low-poly** (soft palette, flat-ish lighting; Crossy Road / Monument Valley energy) |
| Characters | **Low-poly rigged + animated** (walk/idle), per-NPC colour/accessory variation |
| Sourcing | **Free CC0 packs + procedural**, swap to custom later |

## Pillars

1. **Graphics serve the NPCs, not vice-versa.** The differentiator is a living, remembering town. Art should be readable and charming, never heavy or photoreal.
2. **Low asset count, high cohesion.** One biome, one palette, modular kits. A small set assembled well beats a large bespoke set.
3. **Browser-WebGPU friendly.** glTF assets, instanced repeated props, modest poly/draw counts, baked/cheap lighting. (On Apple Silicon, browser WebGPU → Metal.)
4. **Aincrad-shaped.** Floor 1 is a self-contained town that everyone spawns into; floors stack above later with no rework.

## The setting

**Town of Beginnings** — a cobbled stylized-fantasy village on a grassy plateau: a central plaza with a well, a few timber-and-stone shops (Brom's smithy, Mara's herbalist hut), homes, market stalls, a notice board, a perimeter wall with a gate, and surrounding trees/fields. Soft daylight cycling to lantern-lit dusk via the world clock we already broadcast. Small enough to hand-place, big enough to feel alive with NPCs on their daily routines.

## Asset list (Floor 1 minimum)

1. **Terrain & ground** — procedural low-poly ground (grass plateau, dirt paths, a stream, stone plaza). Palette: grass, dirt, stone, water, sand. *(Procedural + Kenney Nature Kit ground pieces.)*
2. **Buildings** (modular) — blacksmith, herbalist hut, 2–3 house variants, market stalls, well, gate + wall sections, notice board. *(Kenney City/Nature kits, KayKit Medieval/Village — CC0, glTF.)*
3. **Characters** (rigged, animated) — player avatar + NPC variants (Brom, Mara, townsfolk) distinguished by colour/hat/tool. Walk + idle clips. *(Quaternius RPG Character Pack / Animated Base Character — CC0, rigged, glTF.)*
4. **Props & set dressing** — trees, bushes, rocks, fences, lanterns, barrels, crates, crops, signposts. *(Kenney Nature Kit, KayKit.)*
5. **Sky & atmosphere** — procedural gradient skybox + sun/moon driven by `TimeOfDay`, soft fog, a few clouds. *(Procedural.)*
6. **Lighting** — hemispheric ambient + directional sun, soft/baked shadows. Stylized, cheap.
7. **HUD/UI** — chat panel, billboard nameplates, "press to talk" prompt near NPCs, player list, day-night clock, minimap. *(Babylon GUI / HTML overlay.)*
8. **Audio** (deferred) — ambient town loop, footsteps, UI blips; NPC voice via TTS later. *(Kenney audio / Freesound CC0.)*

## Sourcing & pipeline

- **Format:** glTF/GLB everywhere (Babylon.js loads it natively). Assets live in `src/VoxelAgentNexus.Server/wwwroot/assets/` so the server serves them.
- **Licensing:** CC0 = public domain, no attribution required, commercial-safe. We still keep an `ASSETS.md` crediting each source (good practice + easy swaps).
- **Primary CC0 sources:**
  - Kenney — https://kenney.nl/assets (Nature Kit, City Kit; 40k+ CC0 assets)
  - Quaternius — https://quaternius.com (rigged/animated low-poly characters)
  - KayKit (Kay Lousberg) and Poly Pizza (https://poly.pizza) for additional CC0 glTF models
  - Curated list: https://github.com/madjin/awesome-cc0
- **Swap-later seam:** because everything is glTF loaded by id, replacing a CC0 model with custom art is a file swap, no code change.

## How this maps to the build

The Babylon.js WebGPU renderer (Linear ADI-6) consumes this: load the village glTF scene + character models once, then each SignalR snapshot drives agent transforms (NPCs walking their routines, players moving). NPC `Id` → character model + skin variant; `TimeOfDay` → sky/lighting.

## Sources

- Quaternius CC0 characters — https://gamefromscratch.com/quaternius-free-3d-assets/ , https://quaternius.com/packs/rpgcharacters.html
- Kenney Nature Kit (CC0, glTF) — https://kenney.nl/assets/nature-kit
- Awesome CC0 list — https://github.com/madjin/awesome-cc0
