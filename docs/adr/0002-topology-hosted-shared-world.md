# ADR 0002 — Topology: hosted shared world + web client

**Status:** Accepted · **Date:** 2026-06-14
**Supersedes the local-first assumption in:** DESIGN_BRIEF §2.4, §3.1 (E2EE)

## Context

The original brief assumed a native desktop client with **client-side E2EE** (local SQLite, keys in the Secure Enclave). We are pivoting to a **hosted shared world**: one authoritative server runs the world and streams it to **browser clients** (WebGPU rendering), so multiple players inhabit the same persistent town of growing NPCs — which suits the *Alicization* vision of a living, shared world.

## Decision

- **Server:** ASP.NET Core + SignalR hosts the authoritative `GameWorld` (our existing simulation/AI/memory libraries, unchanged). A single tick loop advances the world ~10×/s and broadcasts snapshots; the existing `AgentSnapshot` is what clients render. Player chat is routed to the nearest NPC and answered through the `INpcAiAdapter` + encrypted memory, off the tick.
- **Client:** browser. A minimal 2D canvas client exists today to prove the pipe; the **WebGPU voxel renderer (Babylon.js / Three.js) is the next frontend step** (see ADR 0001 — on Apple Silicon, browser WebGPU compiles to Metal, so Metal acceleration is retained).
- **Single shared runtime:** all world mutation happens on the one tick loop; player input arrives via thread-safe structures and AI calls run async. (Mirrors a16z's AI Town runtime model.)

## Consequence: the privacy model changes

This is the real trade-off. Agent memory now lives **server-side**, so:

- The crypto layer (AES-GCM + SQLite) becomes **encryption-at-rest with a server-held key**, **not** client-side E2EE. The code is unchanged; the **trust model and key custody** change (server holds the key instead of the Secure Enclave).
- Players must trust the server operator with conversation data — standard for an online game, but a deliberate departure from the original privacy pillar. If client-side E2EE is ever required again, it would mean a local-server topology instead.

## Status of the first cut

Implemented: shared world, multiplayer join/move, schedule-driven roaming NPCs, snapshot broadcast, player→nearest-NPC AI dialogue, encrypted server-side memory, a 2D browser client.

Not yet: WebGPU voxel rendering, persisted/encrypted relationship graph (currently in-memory), per-player LOD, server-authoritative movement validation, auth.
