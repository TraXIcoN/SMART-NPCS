# Voxel-Agent-Nexus

Native, high-fidelity voxel sandbox on Apple Silicon with autonomous, AI-enabled NPCs that remember, roam, and interact under defined rules of engagement. Persistent agent data is client-side end-to-end encrypted; the AI layer is abstracted behind an adapter so cloud APIs (dev) can be swapped for on-device compute (ship).

See **[DESIGN_BRIEF.md](DESIGN_BRIEF.md)** for the full architecture, NPC memory model, AI cost strategy, and adopted prior-art principles.

## Solution layout

```
VoxelAgentNexus.slnx
src/
  VoxelAgentNexus.Core         Contracts + domain types. Depends on nothing.
  VoxelAgentNexus.Crypto       AES-GCM, key custody (Secure Enclave/Keychain).
  VoxelAgentNexus.Persistence  Encrypted SQLite backend.
  VoxelAgentNexus.Memory       Tiered NPC memory, retrieval, reflection.
  VoxelAgentNexus.Ai           INpcAiAdapter implementations, routing, caching.
  VoxelAgentNexus.Simulation   ECS, rules of engagement, schedules, proximity, LOD.
  VoxelAgentNexus.Engine       Main-thread Metal rendering + voxel meshing.
  VoxelAgentNexus.App          Composition root (wires modules, owns threads).
```

Dependency direction points inward to `Core`; only `App` references everything. Heavy work (AI, memory, crypto, I/O) is fully `async` and runs off the render/simulation threads.

## Status

Pre-implementation scaffold. Contracts defined for the AI layer (`INpcAiAdapter`, `IEmbeddingProvider`); engine code and game loops not yet written.

## Build

Requires the .NET 10 SDK.

```
dotnet build VoxelAgentNexus.slnx
```
