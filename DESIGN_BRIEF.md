# Voxel-Agent-Nexus — Design Brief

**Status:** Pre-implementation design reference · **Last updated:** 2026-06-14
**Scope:** Architecture, NPC memory model, AI cost strategy, and adopted prior-art principles. No engine code yet.

---

## 1. Vision in one paragraph

A native, high-fidelity voxel sandbox on Apple Silicon whose defining feature is an open world of autonomous, AI-enabled NPCs. NPCs roam, follow daily routines, interact with the player and each other under defined "rules of engagement," and build persistent, evolving memories and relationships. All persistent agent data is client-side end-to-end encrypted. The AI execution layer is abstracted behind an adapter so a cloud API (development/quality) can be swapped for on-device compute (shipping) without touching game logic.

---

## 2. System architecture

The core decision is **three execution lanes that communicate only by message-passing**, never by shared mutable state.

### 2.1 Render thread (main) — 8–16 ms frame budget
Owns Metal and the voxel world and nothing else. Never blocks on network, DB, or decryption. Each frame it reads a **double-buffered, immutable NPC state snapshot** published by the simulation thread. If the AI is mid-thought, the renderer neither knows nor cares.

### 2.2 Simulation thread — fixed timestep
Runs the ECS world and the "rules of engagement." NPC behavior is a deterministic **FSM / utility layer** that can always act on its own. Richer decisions (dialogue, social choices) are dropped onto a bounded lock-free queue; the NPC keeps using its scripted fallback until a result returns. **The AI is an enrichment layer, never a dependency** — an NPC is never frozen waiting on a model.

### 2.3 Async task pool — off the critical path
Hosts all slow, allocation-heavy work: the `INpcAiAdapter`, context assembly, embeddings, retrieval, encryption, and SQLite I/O. Everything here is `async/await` and physically isolated from the frame loop.

### 2.4 Crypto boundary
A hard line around persistence: plaintext memory exists only in process RAM. On write, everything passes through hardware AES-GCM (key from Secure Enclave / Keychain) into encrypted SQLite. **Nothing in plaintext ever touches disk.** The cloud adapter sits behind the same orchestration interface, so swapping to a local model is a one-class change.

```
[Render thread] --reads snapshot--> [Sim thread] --request queue--> [Async pool: AI + Memory]
                                                                          |
                                                            [AES-GCM crypto boundary]
                                                                          |
                                                              [Encrypted SQLite]   <-- AI adapter --> Cloud LLM / (local LLM)
```

---

## 3. NPC memory model

Do **not** model memory as "a chat log per NPC" — that grows without bound and destroys both context budget and retrieval quality. Use a **tiered memory stream**, adopted from Stanford's Generative Agents.

- **Working memory** — last N interactions, in RAM, fed directly into prompts.
- **Episodic memory** — append-only event log: `{actor, action, target, location, timestamp, importance}`.
- **Semantic / long-term memory** — periodic off-thread **reflection** summarizes clusters of episodic events into durable beliefs ("the player keeps stealing from the village"). This is what makes personalities *grow* rather than just accumulate.
- **Social graph** — relationships as structured relational rows (affinity, trust, debts), not free text. Queryable and cheap.

**Retrieval** scores candidate memories on **recency × importance × relevance** (the Generative Agents composite score). Only top-k enter the prompt. Relevance uses **on-device embeddings** (Core ML / MLX) — local keeps retrieval private and free.

**Reflection trigger:** sum the importance scores of recent memories; when the running total crosses a threshold, fire a reflection pass (adopted verbatim from Generative Agents — not a fixed timer).

**Housekeeping:** decay/compact low-importance episodic rows into summaries so the DB doesn't grow forever; dedupe and cache embeddings.

### 3.1 Encryption ↔ search interaction (critical)
You cannot run vector similarity or SQL filters over ciphertext. The real model is **encrypted-at-rest, plaintext-in-RAM**: at session start, lazily decrypt an NPC's working set into a secure in-memory index; query there; re-encrypt on write. This decision dictates the schema, so it is locked before any table is created. Keep the decrypted working set minimal (per-NPC, lazy-loaded).

---

## 4. AI cost & latency strategy

Cloud round-trips run ~0.8–2.5 s and are non-deterministic, and per-NPC token cost grows with playtime if handled naïvely. Bound it, cheapest-first:

1. **Prompt caching is the biggest lever.** Cached input ≈ 10% of base cost, cache writes carry a ~25% premium, break-even after ~2 hits, ~5-min TTL. Put persona + lore + rules + format examples in a **byte-for-byte identical prefix**; put volatile retrieved-memories and dialogue only at the end. The context assembler must treat the cached block as immutable ("don't break the cache").
2. **Hybrid local/cloud routing via the adapter.** A small on-device model handles high-frequency grunt work (embeddings, summarization, importance scoring, intent classification). Cloud frontier tokens are spent only on live, player-facing creative dialogue.
3. **Local session management.** The cloud is stateless; we own the session. Send cached prefix + local summary + last few turns + top-k memories under a hard per-turn token budget. Compress to episodic memory when the conversation ends; drop the window.
4. **Constrain output.** Output tokens cost ~4–5× input — emit compact structured intents (action/target/short line), low max-tokens.
5. **Salience routing.** Background "hello traveler" → templated/local. Named, story-relevant NPC → cloud frontier.
6. **Batch & debounce.** No per-frame calls; gate behind events and cooldowns; batch concurrent requests.

---

## 5. Open-world roaming: simulation level-of-detail

Hundreds of roaming agents cannot each run a model per interaction — it bankrupts the budget and blows the frame. Tie simulation fidelity to distance/relevance from the player (the RimWorld / Dwarf Fortress / Skyrim approach).

- **Near (on-screen / player nearby):** full behavior + animation; proximity events may escalate to live, cloud-generated dialogue. Bounded count — this is what you pay for.
- **Mid (loaded, off-screen):** schedule + proximity simulated, but interactions resolved **locally/statistically** — relationship delta + one-line memory stub. No network calls.
- **Far (unloaded):** not ticked; on chunk reload, a cheap catch-up fast-forwards the schedule and rolls a few abstract outcomes. Lazy-generate detail only if the player later asks.

**Supporting systems**
- **Daily cycle = deterministic, not AI.** Schedule table / GOAP (wake → commute → work → eat → socialize → sleep), keyed to in-game time + home/work locations. Runs for every NPC at zero token cost.
- **Proximity via spatial hash.** Each tick, query neighbors within an interaction radius; a hit fires an *interaction candidate*; escalation depends on relationship, cooldown, importance, and player visibility.
- **Event-driven, not poll-driven.** NPCs react to discrete triggers (arrived, saw player, slot changed); between events they just path-follow. Cache repeated commute routes; hierarchical pathfinding off the main thread.
- **Guardrail (Radiant AI lesson):** clamp off-screen outcomes to sane bounds and keep authoritative state minimal/structured, or emergent chaos eventually surfaces to the player.

---

## 6. Adopted prior-art principles

| Source | What they do | What we adopt |
|---|---|---|
| **Stanford Generative Agents** | Memory stream (observations → reflections → durable beliefs); composite recency×importance×relevance retrieval; importance-threshold reflection | The memory model and retrieval/reflection heuristics, verbatim |
| **a16z AI Town** (MIT, open source) | Single shared async runtime processes all LLM calls separate from the world | Validates async pool off render thread (reference, not portable — it's hosted JS/TS) |
| **Mantella** (Skyrim mod) | STT→LLM→TTS pipeline; vector-DB short-term memory; emergent grudges/relationships; OpenAI-compatible, local or cloud, "no data leaves your PC" | Validates adapter + local-DB + E2EE stance; **standardize the adapter on the OpenAI-compatible chat schema** (free cloud + local swap) |
| **NVIDIA ACE / Dead Meat (CES 2025)** | On-device dialogue via ~8B SLM (Mistral-NeMo-Minitron-8B); cloud latency 0.8–2.5 s breaks immersion | Target an **~8B on-device SLM** (via MLX) as the shippable AI path; cloud = dev/quality tier |
| **Inworld** | Cloud-first, per-consumption pricing; "costs scale with engagement" | Confirms the token bottleneck — caching + local routing are mandatory, not optional |
| **Prompt caching (industry)** | Stable-prefix caching cuts cost up to ~90%, latency up to ~85% | Cache-prefix-aware context assembler; immutable persona block |
| **Radiant AI / Dwarf Fortress** | Off-screen settlements simulated abstractly; Bethesda reined in emergent behavior pre-ship | LOD abstraction for mid/far zones + clamp off-screen outcomes |

---

## 7. Top technical risks (carry forward)

1. **E2EE fights semantic search.** Resolved via encrypted-at-rest / plaintext-in-RAM with lazy per-NPC decryption; locks the schema design.
2. **Managed-runtime GC pauses on the Metal path.** Keep the render hot loop allocation-free (pooled buffers, structs/spans, no LINQ), Server GC, isolate alloc-heavy AI/memory on the task pool; validate frame-time early with a profiler.
3. **AI latency / cost / unbounded context.** Resolved via async-with-fallback, tiered memory + top-k retrieval, prompt caching, and local routing.

---

## 8. Provisional tech stack (to validate)

- **Engine/runtime:** native C# (.NET) targeting Metal — e.g. Vulkan-via-MoltenVK (Stride/Silk.NET) or a custom Metal interop layer. *Open question: confirm the highest-fidelity native path on Apple Silicon.*
- **Persistence:** SQLite with app-level AES-GCM (or SQLCipher) — decision pending the encryption-search model in §3.1.
- **Embeddings / local LLM:** MLX or llama.cpp; ~8B SLM target for shipping.
- **AI adapter:** OpenAI-compatible chat schema behind `INpcAiAdapter`.

---

## 9. Bottom-up emergence & run-to-run divergence (vision addendum)

The target is *Sword Art Online: Alicization*-style bottom-up growth: NPCs start from light presets and grow more individuated through accumulated lived experience, and a world run from the same base state should rarely play out the same way twice.

The existing architecture already supports this:

- **Bottom-up personality is the memory stream (§3).** Observations accumulate; the importance-threshold reflection pass distills them into durable beliefs; those beliefs are folded back into the NPC's cacheable persona prefix — so the prompt the model sees literally grows from what the NPC has lived. No retraining; personality is emergent state in encrypted memory.
- **Presets / preloaded conversations** are just seed memories + an initial persona + seed relationships written to the encrypted store at world creation (a `WorldSeeder`). They give each NPC a starting voice that then drifts.
- **Divergence** comes from two compounding sources: (1) a single explicit world RNG **seed** driving stochastic movement, schedule jitter, and interaction tie-breaks — fix the seed for reproducible debugging, draw it from entropy for "different every run"; and (2) **LLM sampling temperature**, which makes dialogue non-deterministic and cascades through proximity encounters into divergent relationship graphs.

**Design rule:** keep the simulation reproducible-by-seed (testable, debuggable) and concentrate *all* nondeterminism in (a) the injected RNG and (b) AI sampling — never in incidental ordering or wall-clock.

## References

- Generative Agents: Interactive Simulacra of Human Behavior — https://dl.acm.org/doi/fullHtml/10.1145/3586183.3606763
- Generative Agents memory architecture — https://www.subodhjena.com/blog/generative-agents-memory-stanford
- a16z AI Town — https://github.com/a16z-infra/ai-town
- Mantella — https://github.com/art-from-the-machine/Mantella
- NVIDIA ACE autonomous characters — https://www.nvidia.com/en-us/geforce/news/nvidia-ace-autonomous-ai-companions-pubg-naraka-bladepoint/
- On-device SLM NPCs in Dead Meat (GDC 2025) — https://www.youtube.com/watch?v=F_Tpfn1LxEA
- Prompt caching cost reduction — https://medium.com/tr-labs-ml-engineering-blog/prompt-caching-the-secret-to-60-cost-reduction-in-llm-applications-6c792a0ac29b
- Don't Break the Cache (arXiv) — https://arxiv.org/pdf/2601.06007
- Radiant AI — https://en.wikipedia.org/wiki/Radiant_AI
