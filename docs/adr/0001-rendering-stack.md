# ADR 0001 — Rendering stack for the voxel engine on Apple Silicon

**Status:** Proposed (awaiting decision) · **Date:** 2026-06-14
**Context owner:** Quest Forge / Voxel-Agent-Nexus

## Context

The project targets a native, high-fidelity voxel sandbox on **Apple Silicon**, written in **C#/.NET**. The simulation/AI "brain" (encrypted memory, AI adapter, schedules, proximity, engagement gate, reflection) is already built as renderer-agnostic .NET libraries; the renderer only ever reads the immutable `AgentSnapshot`. We now need to choose the rendering/engine stack the "body" is built on.

Two hard constraints shape everything:

1. **Apple Silicon means Metal.** macOS has **no native Vulkan**; Apple ships only Metal. Vulkan runs on Apple solely through translation layers — primarily **MoltenVK** (near-conformant Vulkan 1.4 over Metal). As of early 2026 there is also **KosmicKrisp** (LunarG/Google, fully conformant Vulkan over Metal 4, Apple-Silicon-only, very new), and there's industry chatter that Apple may tighten restrictions around MoltenVK-style layers. Net: the lowest-risk Apple path is **native Metal**; Vulkan-on-Apple is a translation dependency.
2. **Two hard problems, not one.** "Graphics" here is really (a) getting Metal rendering working from C# at high fidelity, and (b) voxel-specific rendering — chunk meshing (greedy meshing), face culling, chunk LOD, paging chunks in/out — kept off the render thread.

The fundamental fork is **build on an existing engine** vs **build a custom engine on a low-level graphics library**.

## Options evaluated

| Option | C# story | Apple Silicon / Metal | Voxel tooling | Maintenance | Effort | Verdict |
|---|---|---|---|---|---|---|
| **Godot 4 (C#/.NET) + `godot_voxel`** | First-class-ish (C++ engine, C# scripting via .NET arm64) | **Native Metal backend** (Godot 4.4, Apple-Silicon, now default, faster than MoltenVK) | **Mature** — Zylann's `godot_voxel`: blocky terrain, LOD, infinite chunk paging, AO, instancing | Active, large community | **Low–Med** | **Recommended** |
| **Evergine** | Pure C# engine | Metal (Metal 3, iOS/macOS) | None voxel-specific | Active, commercial (Plain Concepts), smaller community | Med–High | Strong alternative |
| **Stride** | **Pure C#** engine, .NET 10 / C# 14, PBR | **macOS/Apple Silicon NOT an official target** (Win/Linux/Android/iOS/UWP/Xbox) | None voxel-specific | Active | High (+ self-porting to mac) | Disqualified for Apple-first |
| **Silk.NET + MoltenVK (custom engine)** | Bindings only — you write the engine | Vulkan→Metal via MoltenVK (translation dep + Apple risk) | Build it all yourself | Silk.NET active | **Very High** | Only if rendering *is* the goal |
| **SDL3 GPU API (SDL_gpu) + C# bindings** | Bindings — you write the engine | Abstracts Metal/Vulkan/D3D12; targets Metal natively | Build it yourself | Emerging; where ex-Veldrid users are migrating | Very High | Promising but young |
| **Veldrid** | .NET-native graphics lib w/ a Metal backend | Native Metal (shipped in osu! on Apple Silicon) | Build it yourself | **EOL** — maintainer stepped back (2023); osu! migrating to SDL3_gpu | High | **Don't start here** |
| **MonoGame** | XNA-style C# framework | Metal for macOS **not yet released** (Vulkan/DX12 in 3.8.5 preview) | None; not fidelity-oriented | Active | High | Not a fit |

## Decision (recommended)

**Build on Godot 4 (C#/.NET) with the `godot_voxel` module, keeping the simulation/AI brain as standalone .NET libraries the Godot C# layer calls.**

Rationale: it is the **only** option that combines all three of (1) **native Metal on Apple Silicon**, (2) **first-class C#**, and (3) a **production-grade voxel engine**. It collapses *both* hard problems — Metal rendering and voxel meshing/LOD — into solved, actively-maintained components, so effort goes into the game (NPC behavior, world, art) rather than into reinventing an engine. Our brain stays a clean .NET dependency; the renderer reads snapshots exactly as designed, so this choice doesn't perturb anything already built.

### How it fits our architecture
- Godot owns the main render thread + Metal + voxel chunks (`godot_voxel`).
- Our existing `VoxelAgentNexus.*` libraries run as the simulation/AI layer the Godot C# scripts call; NPC positions flow to Godot via the `AgentSnapshot` snapshot, exactly as today.
- Perf-critical voxel work lives in the C++ module; gameplay/AI glue is C#.

## Consequences & risks

- **"Native C# engine" caveat.** Godot is a **C++ engine scripted in C#**, not a pure-managed engine. If "pure C#" is a hard requirement, Godot doesn't satisfy it — see the trade-off below.
- **Godot C# is second-class to GDScript** in some editor tooling and docs; .NET export has occasional friction.
- **A Godot 4.4 Metal performance regression on M1** (vs 4.3) was reported and is being worked — worth benchmarking on target hardware before committing hard.
- **`godot_voxel` is a C++ GDExtension/module** — you consume it, you don't write voxel rendering in C#. That's a feature (perf) but means the lowest layer isn't yours.

## The one judgment call for the project owner

There is a genuine **pick-two** tension between:

- **Pure C# engine** (managed all the way down),
- **Apple-Silicon-native Metal** (no translation layer), and
- **Reasonable effort** (don't build an engine).

- Godot = native Metal + reasonable effort, **but not pure C#**.
- Evergine = pure C# + Metal, **but smaller ecosystem, no voxel tooling, more effort**.
- Stride = pure C#, **but no macOS** (would need porting).
- Custom (Silk.NET/SDL3_gpu) = native + yours, **but very high effort**.

**Recommendation holds (Godot)** unless "pure managed C# engine" is non-negotiable — in which case the realistic path is **Evergine** (and we accept building voxel tooling), or a **custom Silk.NET/MoltenVK** engine if bespoke rendering is itself a project goal.

## Sources

- Godot 4.4 native Metal backend (Apple Silicon) — https://github.com/godotengine/godot-proposals/discussions/6841 ; internal rendering architecture: https://docs.godotengine.org/en/latest/engine_details/architecture/internal_rendering_architecture.html
- Godot 4.4 Metal perf regression on M1 — https://github.com/godotengine/godot/issues/103723
- Zylann `godot_voxel` (voxel engine module for Godot 4) — https://github.com/Zylann/godot_voxel
- State of Vulkan on Apple, Jan 2026 (Metal-only, MoltenVK, KosmicKrisp) — https://www.lunarg.com/the-state-of-vulkan-on-apple-jan-2026/
- MoltenVK — https://github.com/KhronosGroup/MoltenVK
- Stride engine (platforms; .NET 10) — https://en.wikipedia.org/wiki/Stride_(game_engine) ; https://www.stride3d.net/
- Veldrid EOL / osu! moving to SDL3_gpu — https://github.com/ppy/osu/discussions/27659
- Evergine Metal (iOS/macOS) — https://evergine.com/ios-metal-api/
- MonoGame platforms / 3.8.5 preview (Vulkan/DX12; Metal pending) — https://monogame.net/blog/2025-12-19-385-preview/
- Silk.NET (Vulkan bindings) — https://www.nuget.org/packages/Silk.NET.Vulkan/2.22.0
