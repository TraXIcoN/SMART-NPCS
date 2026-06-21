# Assets drop folder (glTF / GLB)

The Babylon.js renderer loads 3D models from here at runtime. Until CC0 packs are
added, the client uses procedural placeholders (capsules + boxes).

## How to add models

1. Download CC0 glTF/GLB packs (see `docs/art-direction.md`):
   - Characters (rigged + animated): https://quaternius.com
   - Environment / buildings / props: https://kenney.nl/assets , https://poly.pizza
2. Drop files here, e.g. `assets/characters/blacksmith.glb`, `assets/buildings/well.glb`.
3. Map NPC ids to models in `wwwroot/index.html` → the `MODELS` registry:
   ```js
   const MODELS = { "npc_brom": "assets/characters/blacksmith.glb",
                    "npc_mara": "assets/characters/herbalist.glb" };
   ```
   The renderer will import the model instead of a placeholder — no other code changes.

## Licensing

Prefer **CC0** (public domain, no attribution required, commercial-safe). Record each
source in `ASSETS.md` at the repo root regardless, for easy swaps and good hygiene.
