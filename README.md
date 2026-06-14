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

Early foundation. Contracts defined for the AI, crypto, persistence, and memory layers. Two slices are implemented and tested:

- **Encrypted memory:** AES-256-GCM crypto provider, SQLite encrypted store, and a memory store that seals every record before it touches disk, plus Generative-Agents-style retrieval.
- **AI layer:** an OpenAI-compatible `INpcAiAdapter` (generate + stream, usage/cache parsing) and an `NpcPromptBuilder` that assembles a cache-stable prefix + per-turn retrieved memories.

Engine rendering, simulation, and game loops are not yet written.

## Build & test

Requires the .NET 10 SDK.

```
dotnet build VoxelAgentNexus.slnx
dotnet test  VoxelAgentNexus.slnx
```

Key tests (`tests/VoxelAgentNexus.IntegrationTests`): a memory round-trips through the AES-GCM + SQLite pipeline, and a scan of the raw database file asserts **no plaintext ever reaches disk**.

## Run the NPC demo

```
dotnet run --project src/VoxelAgentNexus.App
```

Chat with Brom the blacksmith. Everything he "remembers" is encrypted on disk (`src/VoxelAgentNexus.App/bin/.../data/nexus.sqlite`); quit and re-run and he recalls prior conversations. With no AI endpoint set it uses a deterministic fallback. For real dialogue, point it at any OpenAI-compatible endpoint:

```
export NEXUS_AI_BASE_URL=https://api.openai.com/v1
export NEXUS_AI_MODEL=gpt-4o-mini
export NEXUS_AI_KEY=sk-...
dotnet run --project src/VoxelAgentNexus.App
```

A local server (Ollama / LM Studio / an MLX server) works the same way — just set `NEXUS_AI_BASE_URL` to its address.
