# Analysis: Phase 1 Feasibility and Risk Assessment

## 1. Objective and Scope

**Objective**: Assess whether Phase 1 of OpenVEPA is achievable as specified. Identify technology risks, complexity hotspots, dependency risks, integration risks, underspecified areas, and features that should be cut or deferred.

**Scope**: `docs/project_requirements.md` (Sections 2-9), three research documents (`dotnet_stack_analysis.md`, `dotnet_infrastructure_analysis.md`, `skill_format_analysis.md`), and current package ecosystem status.

**Methodology**: Requirements document review, NuGet package maturity research, web research on dependency status, complexity estimation based on integration surface area.

---

## 2. Phase 1 Scope as Specified

Phase 1 ("Foundation") includes these deliverables per Section 2:

| # | Deliverable | Subsystems Required |
|---|-------------|---------------------|
| 1 | Core Assistant | Agent framework, system prompt, guidance, autonomy levels, task execution settings |
| 2 | Skill Runtime | SKILL.md parser, AssemblyLoadContext loader, ISkill interface, native + MCP bridge + hybrid execution |
| 3 | Local Skill Management | Install, list, enable/disable from local paths and sideloaded packages |
| 4 | Hub Client | Browse, search, install from public Hub registry |
| 5 | CLI / TUI | Spectre.Console.Cli command routing, rich terminal output, SignalR .NET client |
| 6 | Messaging Channels | Telegram, Discord, Email adapters (3 adapters) |
| 7 | LLM Providers (P0) | OpenAI + Ollama via IChatClient |
| 8 | Default Infrastructure | SQLite/EF Core, file storage, HybridCache, Channels, Hangfire, SignalR |

Hidden dependencies pulled in by these deliverables (from Sections 3-9):

- MCP dependency manager with reference counting and process pool (Section 3.4)
- SignalR hub architecture: AssistantHub, TaskHub, SystemHub (Section 4.2)
- Token-based WebSocket authentication (Section 4.1, 9.2)
- Session management with multi-session, cross-client continuity (Section 4.5)
- Long-running task checkpointing and resumability (Section 5.4)
- Scheduled tasks via Hangfire (Section 5.5)
- Concurrent task execution with prioritization (Section 5.6)
- User profile and learned preferences with confidence scoring (Section 5.9)
- Token minimization strategy across all layers (Section 6.4)
- LLM budget tracking per-task and per-day (Section 5.3)
- Full data directory layout with 12+ config/data paths (Section 8.5)
- Bearer token management CLI (Section 9.2)
- Data classification and privacy routing (Section 9.1)
- Message protocol with envelope format (Section 4.4)
- Agent definition format with `<name>.agent.md` (Section 3.1.1)

**Observation**: The explicit Phase 1 table lists 8 deliverables. The detailed sections describe 15+ additional subsystems that are implicitly required for those deliverables to function. This is a significant scope gap between the summary and the specification body.

---

## 3. Risk Register

### 3.1 Technology Risks

| ID | Risk | Severity | Likelihood | Impact | Mitigation |
|----|------|----------|------------|--------|------------|
| T1 | **sqlite-vec is alpha (v0.1.7-alpha)**. All NuGet releases are pre-release. Breaking changes expected. No stable release timeline published. | High | High | Medium | Defer vector DB to Phase 2. Use in-memory volatile store from Semantic Kernel for prototyping. Add sqlite-vec when it reaches beta/RC. |
| T2 | **Microsoft Agent Framework (MAF) is RC2, not GA**. Semantic Kernel is now in maintenance mode. MAF is Phase 3 but the requirements doc references it in Section 6.1 as "Layer 3". If MAF APIs change before Phase 3, the IChatClient abstraction layer design may need rework. | Medium | Medium | Low | No action for Phase 1. MAF is explicitly Phase 3. IChatClient from M.E.AI is stable and decoupled. Monitor MAF release cadence. |
| T3 | **Hangfire.Storage.SQLite is v0.4.2 (April 2024)**. Community-maintained, not official Hangfire. 181 commits total. Low bus factor. | Medium | Medium | Medium | Acceptable for Phase 1. Hangfire Core itself is stable (v1.8.x). If SQLite storage becomes unmaintained, Hangfire supports SQL Server and Redis as alternatives. |
| T4 | **SKILL.md spec is 2 months old (Dec 2025)**. Rapid adoption (26+ platforms, 20K GitHub stars) but the spec may evolve. OpenVEPA adds `openvepa-*` metadata extensions that could conflict with future spec changes. | Low | Medium | Low | Extensions use `openvepa-` prefix, which is the spec's recommended approach. Monitor agentskills.io for breaking changes. |
| T5 | **agentskills.io `allowed-tools` field is marked "Experimental"**. May be removed or redesigned. | Low | Medium | Low | Do not depend on `allowed-tools` for permission enforcement. Use `openvepa-permissions` extension instead. |
| T6 | **.NET 10 is 2 months post-release (Nov 2025)**. Some NuGet packages may not yet target net10.0. | Low | Low | Medium | Most Microsoft packages shipped day-one with .NET 10 support. Third-party packages targeting netstandard2.0 or net8.0 work on .NET 10. |

### 3.2 Complexity Risks

| ID | Risk | Severity | Likelihood | Impact | Mitigation |
|----|------|----------|------------|--------|------------|
| C1 | **Long-running task resumability (Section 5.4)** is deceptively complex. Requires serializing arbitrary agent state, conversation context, partial plan progress, and token usage to a checkpoint. Resuming must reconstruct LLM context from checkpoints without losing coherence. This is the hardest feature in Phase 1. | Very High | High | High | Defer full checkpoint/resume to Phase 2. Phase 1: tasks complete or fail. Hangfire provides retry. Persistence of results only, not mid-task state. |
| C2 | **MCP reference counting and process pool (Section 3.4)** requires tracking process lifecycle, shared state across skills, idle timeouts, graceful shutdown, crash recovery, and stdio/HTTP multiplexing. The spec describes 6 lifecycle events with complex interaction patterns. | High | High | Medium | Phase 1: start/stop MCP servers per skill invocation. No pooling. No reference counting. No idle timeout. Simple 1:1 model. Optimize in Phase 2. |
| C3 | **User preference learning with confidence scoring (Section 5.9)** requires NLP inference over conversation patterns, confidence decay, cross-session aggregation, and confirmation workflows. This is an ML feature disguised as a configuration feature. | High | High | Medium | Defer implicit learning entirely. Phase 1: explicit preferences only (`openvepa profile set`). Store as key-value in SQLite. No confidence scoring. No inference. |
| C4 | **Token budget tracking (Section 5.3)** requires intercepting all LLM calls, computing costs from provider-specific pricing tables, aggregating per-task and per-day, and enforcing budget actions (pause/stop/notify). Provider pricing changes frequently. | Medium | Medium | Medium | Phase 1: log token counts per call (usage data returned by providers). Display in CLI. Skip cost-in-USD calculation and budget enforcement. |
| C5 | **Three execution modes (native, MCP bridge, hybrid)** each require different loading, lifecycle, and error handling paths. Hybrid combines both. Testing matrix grows multiplicatively. | Medium | Medium | Medium | Phase 1: implement native and MCP bridge only. Hybrid is native + MCP bridge combined; defer until both are stable individually. |
| C6 | **Session management with cross-client continuity (Section 4.5)** requires mapping platform-specific identifiers to OpenVEPA sessions, handling reconnection, context reconstruction, and multi-session routing. | Medium | Medium | Medium | Phase 1: one session per client connection. No cross-client session continuity (start on Telegram, continue on TUI). Implement session persistence. Defer session migration across clients. |
| C7 | **Three SignalR hubs (Section 4.2)** with 5 communication patterns (request-response, streaming, push, broadcast, group-targeted). Each hub requires its own message types, auth, and error handling. | Medium | Medium | Low | Phase 1: single hub (AssistantHub) with chat + streaming. TaskHub and SystemHub are Phase 2. |

### 3.3 Dependency Risks

| ID | Risk | Severity | Likelihood | Impact | Mitigation |
|----|------|----------|------------|--------|------------|
| D1 | **Hub does not exist yet**. The Hub is a separate repository (`OpenVEPA-Hub`). Phase 1 requires a Hub Client that connects to a Hub API. No Hub API exists to connect to. | High | Certain | Medium | Phase 1: local-only skill management (sideloading). Hub Client is a stub that returns "Hub not available". Hub Client implementation deferred until `OpenVEPA-Hub` repo has a running API. |
| D2 | **Three messaging platform SDKs** (Telegram.Bot, Discord.Net, MailKit) each have their own async patterns, rate limits, connection management, and failure modes. Each adapter is a non-trivial integration. | Medium | Medium | Medium | Phase 1: implement 1 adapter (Telegram -- most mature, simplest bot API). Discord and Email deferred to late Phase 1 or Phase 2. |
| D3 | **MCP server packages are npm-based** (e.g., `@anthropic/mcp-web-search`). MCP dependency manager must handle npm install, npx execution, and Node.js runtime dependency on user machines. Cross-platform npm/npx behavior varies. | Medium | High | Medium | Phase 1: assume MCP servers are pre-installed. Document manual installation. Automated MCP server installation (npm/npx management) deferred. |
| D4 | **Provider SDK availability**. Anthropic C# SDK is official but younger than OpenAI's. Ollama integration via OllamaSharp is community-maintained. | Low | Low | Medium | Phase 1 targets OpenAI + Ollama only. Both have stable .NET packages. Anthropic and Gemini are P1/P2 (Phase 2+). |

### 3.4 Integration Risks

| ID | Risk | Severity | Likelihood | Impact | Mitigation |
|----|------|----------|------------|--------|------------|
| I1 | **SignalR + token auth + channel adapters**: Adapters are SignalR clients authenticating with bearer tokens to the OpenVEPA server. This requires the server to validate tokens on WebSocket upgrade, manage connection lifecycle, and handle adapter disconnection/reconnection. Three different adapter implementations must each handle this correctly. | High | Medium | High | Phase 1: TUI connects directly (no auth). Reduce to 1 external adapter (Telegram) with token auth. This reduces the integration surface from 4 clients to 2. |
| I2 | **Hangfire + task checkpointing**: Hangfire manages job scheduling and retry. Checkpointing requires custom serialization of agent state into Hangfire's job storage. Hangfire jobs are designed for stateless, idempotent operations. Long-running stateful tasks conflict with this model. | High | Medium | High | Phase 1: use Hangfire for scheduled cron jobs only (briefings, reminders). Use BackgroundService for active task execution. Do not attempt to merge checkpointing into Hangfire's job model. |
| I3 | **EF Core + SQLite + Hangfire SQLite**: Both EF Core and Hangfire want to own SQLite connections. Concurrent access from two ORMs to the same or separate SQLite files requires careful WAL mode configuration and connection management. | Medium | Medium | Medium | Use separate SQLite files: `openvepa.db` (EF Core) and `openvepa-jobs.db` (Hangfire). The infrastructure analysis already recommends this pattern. |
| I4 | **AssemblyLoadContext + DI container**: Skills loaded via ALC need access to host services (ILogger, IChatClient, IDocumentStore). Passing host services across ALC boundaries requires shared contract assemblies and careful dependency resolution. McMaster.NETCore.Plugins handles some of this, but adds another dependency. | Medium | Medium | Medium | Start with McMaster.NETCore.Plugins for the ALC wrapper. It handles shared type resolution. Write a thin abstraction over it for future replacement. |
| I5 | **LLM streaming + SignalR streaming + TUI rendering**: Token-by-token LLM output must flow through IChatClient -> IAsyncEnumerable -> SignalR streaming -> Spectre.Console live rendering. Each layer has its own backpressure and cancellation semantics. | Medium | Medium | Low | Implement as a vertical slice first (OpenAI streaming to TUI). Validate the full pipeline before adding adapters or multiple providers. |

---

## 4. Phase 1 Scope Recommendation

### Features: Keep / Defer / Cut

| Feature | Verdict | Rationale |
|---------|---------|-----------|
| **Core Assistant (basic)** | KEEP | Foundation. Receives input, selects skill, returns result. Without autonomy levels 3-4 or multi-agent. |
| **Skill Runtime -- native execution** | KEEP | Core value. Load .NET assemblies via ALC, execute ISkill. |
| **Skill Runtime -- MCP bridge** | KEEP (simplified) | Core value. Wrap MCP servers. Skip reference counting and process pooling. |
| **Skill Runtime -- hybrid execution** | DEFER to Phase 2 | Hybrid = native + MCP combined. Defer until both modes are individually stable. |
| **SKILL.md parser** | KEEP | Required for skill discovery. YAML frontmatter parser is low effort. |
| **Local skill management** | KEEP | Sideload from local paths. Essential for development and testing. |
| **Hub Client** | DEFER to Phase 2 | No Hub API exists. Stub interface only. Local-only mode is fully functional without it. |
| **CLI (Spectre.Console.Cli)** | KEEP | Primary interface. Command routing, basic output. |
| **TUI (rich interactive)** | KEEP (minimal) | Live streaming display. Skip dashboard, task management UI. |
| **Telegram adapter** | KEEP | 1 messaging adapter proves the channel adapter pattern. |
| **Discord adapter** | DEFER to late Phase 1 | Second priority. Add after Telegram is stable. |
| **Email adapter** | DEFER to Phase 2 | IMAP/SMTP is significantly more complex (polling, threading, OAuth2). |
| **OpenAI provider** | KEEP | P0 provider. Most mature .NET SDK. |
| **Ollama provider** | KEEP | P0 provider. Local/offline execution. Zero API cost for development. |
| **SQLite via EF Core** | KEEP | Foundation database. Well-understood. |
| **File-based document store** | KEEP | Zero-dependency default. |
| **HybridCache** | KEEP | 1 NuGet package. Low effort. |
| **System.Threading.Channels** | KEEP | Built-in. Zero effort. |
| **Hangfire (scheduled jobs)** | KEEP (cron only) | Morning briefings, recurring tasks. Skip as task execution engine. |
| **SignalR (AssistantHub)** | KEEP | Required for CLI/TUI and adapter communication. |
| **SignalR (TaskHub, SystemHub)** | DEFER to Phase 2 | Single hub is sufficient for Phase 1. |
| **Token auth (bearer tokens)** | KEEP (basic) | Required for Telegram adapter. Simple token validation. |
| **Session management (basic)** | KEEP | 1 session per connection. Persistence. History retrieval. |
| **Session cross-client continuity** | DEFER to Phase 2 | Start on Telegram, continue on TUI. Complex routing logic. |
| **Long-running task checkpointing** | DEFER to Phase 2 | Hardest feature. Tasks complete or fail in Phase 1. |
| **Long-running task resumability** | DEFER to Phase 2 | Depends on checkpointing. |
| **User preference learning (implicit)** | DEFER to Phase 2 | ML-adjacent feature. Needs conversation pattern analysis. |
| **User preferences (explicit)** | KEEP | Simple key-value store. `openvepa profile set/show`. |
| **Token budget tracking (per-task/day)** | DEFER to Phase 2 | Phase 1: log token counts. Skip cost calculation and enforcement. |
| **Token usage logging** | KEEP | Capture usage data returned by providers. Store in SQLite. Display in CLI. |
| **Data classification / privacy routing** | DEFER to Phase 2 | Configuration-driven cloud vs. local routing. Phase 1: user picks provider manually. |
| **Autonomy levels 1-2** | KEEP | Supervised and semi-autonomous. |
| **Autonomy levels 3-4** | DEFER to Phase 2 | Requires checkpointing, budget enforcement, and failure recovery. |
| **Concurrent task execution** | DEFER to Phase 2 | Phase 1: one task at a time. Sequential. |
| **MCP automated installation** | DEFER to Phase 2 | Phase 1: manual MCP server installation. |
| **Agent definition format** | KEEP | `<name>.agent.md` parser. Low effort given SKILL.md parser already exists. |
| **Vector DB (sqlite-vec)** | DEFER to Phase 2 | Alpha package. Use in-memory volatile store for any early RAG experiments. |
| **Briefing Center** | DEFER to Phase 2 | Depends on TTS, scheduling, content skills. |
| **Message envelope format** | KEEP | Define the message types early. Foundation for all communication. |
| **.opvpkg package format** | DEFER to Phase 2 | No Hub to distribute packages. Local sideloading uses raw directories. |

### Minimum Viable Phase 1

The smallest Phase 1 that delivers the core value proposition ("give it a task, get a result"):

1. **Core Assistant** receiving input and returning results
2. **Skill Runtime** with native execution and simplified MCP bridge
3. **SKILL.md parser** for skill discovery
4. **Local skill management** (sideload from directories)
5. **CLI / TUI** with Spectre.Console (streaming LLM output)
6. **1 messaging adapter** (Telegram)
7. **2 LLM providers** (OpenAI + Ollama)
8. **Default infrastructure** (SQLite, file store, HybridCache, Channels, Hangfire for cron)
9. **SignalR AssistantHub** with basic token auth
10. **Basic session management** (create, persist, retrieve history)
11. **Explicit user preferences** (key-value store)
12. **Token usage logging** (counts per call, no budget enforcement)

This is approximately 60% of the specified Phase 1 scope. The other 40% is deferred, not cut. All deferred features have clear extension points in the kept features.

---

## 5. Complexity Estimates

| Feature | Complexity | Estimated Effort | Notes |
|---------|-----------|-----------------|-------|
| Solution structure + DI + config | Simple | 2-3 days | Standard .NET patterns. Well-documented. |
| EF Core + SQLite setup + migrations | Simple | 2-3 days | One-time setup. Schema design takes thought. |
| IDocumentStore + FileDocumentStore | Simple | 1-2 days | 3 methods: Save, Get, Query. |
| HybridCache registration | Simple | 0.5 day | 1 NuGet + 1 DI line. |
| Serilog setup | Simple | 0.5 day | Console + file sinks. |
| SKILL.md YAML frontmatter parser | Simple | 2-3 days | Parse YAML, validate name/description/metadata fields. |
| Agent.md parser | Simple | 1-2 days | Same pattern as SKILL.md. |
| Message envelope types | Simple | 1 day | C# record types with JSON serialization. |
| Explicit user preferences | Simple | 2-3 days | CRUD on key-value table. CLI commands. |
| Token usage logging | Simple | 2-3 days | IChatClient middleware. Log to SQLite. |
| Bearer token management | Medium | 3-4 days | Token CRUD, hashing, validation on SignalR. |
| Spectre.Console CLI command structure | Medium | 4-5 days | Command tree, argument parsing, help text. |
| TUI with live LLM streaming | Medium | 3-5 days | Spectre.Console Live + IAsyncEnumerable rendering. |
| ISkill interface + native skill loading (ALC) | Medium | 5-7 days | AssemblyLoadContext, shared contracts, DI injection across ALC boundary. |
| OpenAI provider via IChatClient | Medium | 3-5 days | SDK integration, streaming, tool calling. |
| Ollama provider via IChatClient | Medium | 3-5 days | OllamaSharp, streaming, model selection. |
| SignalR AssistantHub | Medium | 5-7 days | Hub definition, streaming, connection management, auth middleware. |
| Session management (basic) | Medium | 4-5 days | EF Core model, create/resume/list/archive, history retrieval. |
| Hangfire cron job setup | Medium | 3-4 days | SQLite storage, job definitions, dashboard. |
| Local skill install/list/enable/disable | Medium | 4-5 days | Directory scanning, registry, CLI commands. |
| Telegram adapter | Hard | 7-10 days | Telegram.Bot SDK, webhook/polling, session mapping, reconnection, rate limiting. |
| MCP bridge skill (simplified) | Hard | 7-10 days | MCP client, stdio transport, process start/stop, tool discovery, tool calling, error handling. |
| LLM streaming pipeline (provider -> SignalR -> TUI) | Hard | 5-7 days | End-to-end IAsyncEnumerable plumbing, backpressure, cancellation. |
| IMessageBus + ChannelMessageBus | Medium | 2-3 days | Thin wrapper. Producer/consumer pattern. |
| Failure handling (Polly retry, fallback) | Medium | 3-4 days | Polly policies, fallback provider selection. |
| *Deferred: Long-running task checkpointing* | Very Hard | 10-15 days | State serialization, context reconstruction, LLM context replay. |
| *Deferred: MCP process pool + ref counting* | Hard | 7-10 days | Lifecycle management, crash recovery, idle timeout. |
| *Deferred: User preference learning (implicit)* | Very Hard | 15-20 days | NLP inference, confidence scoring, cross-session aggregation. |
| *Deferred: Token budget enforcement* | Hard | 5-7 days | Provider pricing tables, real-time aggregation, pause/stop actions. |
| *Deferred: Concurrent task execution* | Hard | 7-10 days | SemaphoreSlim, prioritization, dependency handling, progress tracking. |
| *Deferred: Hub Client (full)* | Hard | 7-10 days | API client, package download, checksum verification, dependency resolution. |

**Total estimated effort for recommended Phase 1**: 75-100 developer-days (1 developer, approximately 4-5 months; 2 developers, approximately 2-3 months).

**Total estimated effort for full Phase 1 as specified**: 130-170 developer-days (1 developer, approximately 7-9 months).

---

## 6. Underspecified Areas

| # | Section | Gap | Impact |
|---|---------|-----|--------|
| 1 | **ISkill interface** (Section 3.2) | No formal interface definition beyond `Name`, `Description`, `ExecuteAsync`. What does `SkillInput` contain? What is `SkillResult`? How are errors communicated? What about cancellation? Streaming results? | HIGH. This is the core contract. Every skill author needs this defined precisely. |
| 2 | **IChannelAdapter interface** (Section 4.3) | Mentioned as "common interface" with `StartAsync`, `StopAsync`, `SendMessageAsync`, `OnMessageReceived`. No definition of message types, error handling, reconnection policy, or rate limit behavior. | MEDIUM. First adapter implementation will define this, but retrofit risk exists. |
| 3 | **MCP bridge translation layer** (Section 3.4) | How does McpBridgeSkill translate between ISkill input/output and MCP `tools/call` request/response? Schema mapping is undefined. | HIGH. This is the bridge between two ecosystems. |
| 4 | **Agent-to-skill binding** (Section 3.1.1) | Agent metadata lists `openvepa-skills` but no specification of how skills are bound, how skill selection works, or how the LLM is prompted with skill information. | MEDIUM. Central to the Assistant's reasoning loop. |
| 5 | **Task execution model** (Section 5.3-5.4) | How is a "task" defined? What is the relationship between a user message, a task, and a Hangfire job? Are all user messages tasks? Only explicit ones? | HIGH. Fundamental domain model question. |
| 6 | **Conversation summarization** (Section 6.4) | Listed as a token minimization strategy. No specification of when to summarize, how to summarize, or how to preserve key information. | LOW for Phase 1 (can use simple truncation). HIGH for later phases. |
| 7 | **Skill permission enforcement** (Section 9.3) | Skills declare permissions via `openvepa-permissions`. No specification of enforcement mechanism. What prevents a native skill from ignoring its declared permissions? | LOW for Phase 1 (trust model). MEDIUM for Hub-distributed skills. |
| 8 | **Configuration merging** (Section 8.5) | Agent config, skill config, global config, per-task overrides. 5 layers of configuration priority. Interaction semantics between `config/appsettings.json`, `agents/<name>/config.json`, and `skills/<name>/config.json` are not fully specified. | MEDIUM. Will cause confusion during implementation. |
| 9 | **Error propagation model** | No cross-cutting specification of how errors propagate from skill -> runtime -> agent -> hub -> user. Each section handles errors differently. | MEDIUM. Inconsistent error handling is a common integration issue. |
| 10 | **OpenAI / Ollama tool calling format** | Section 6.3 notes tool calling is provider-dependent for Ollama. No specification of how tool definitions map to provider-specific formats. | LOW. IChatClient abstraction handles this, but edge cases exist. |

---

## 7. Features to Cut from Phase 1

These features add scope without contributing to the core value proposition ("give the assistant a task, get a result back").

| Feature | Reason to Cut | When to Add |
|---------|---------------|-------------|
| **Hub Client** | No Hub exists. Local-only mode is complete without it. | When `OpenVEPA-Hub` has a running API. |
| **Email adapter** | IMAP/SMTP complexity (polling, threading, OAuth2) is 3x Telegram. | Phase 2 after Telegram + Discord prove the adapter pattern. |
| **Hybrid skill execution** | Combines two untested modes. Ship native + MCP bridge first. | Phase 2 when both modes are individually stable. |
| **Long-running task checkpoint/resume** | Hardest feature. Requires state serialization and LLM context reconstruction. | Phase 2 with dedicated design spike. |
| **User preference learning (implicit)** | ML inference feature. Not needed when explicit preferences exist. | Phase 2 or 3. Requires conversation data to learn from. |
| **Token budget enforcement** | Requires provider pricing tables and real-time cost tracking. | Phase 2. Token logging provides visibility without enforcement. |
| **Data classification / privacy routing** | Configuration overhead. Users can manually choose provider. | Phase 2 when multiple providers are available. |
| **Autonomy levels 3-4** | Require checkpointing, budget enforcement, and unsupervised execution. | Phase 2 when safety infrastructure exists. |
| **Concurrent task execution** | Prioritization and dependency handling add significant complexity. | Phase 2. Sequential is fine for initial release. |
| **Vector DB (sqlite-vec)** | Alpha package. Breaking changes likely. | Phase 2 when sqlite-vec reaches beta or use Qdrant. |
| **Briefing Center** | Depends on TTS, content skills, and scheduling. Full feature, not foundation. | Phase 2 content phase. |
| **TaskHub / SystemHub** | Single AssistantHub handles Phase 1 needs. | Phase 2 when task management UI is built. |
| **Cross-client session continuity** | Start on Telegram, continue on TUI. Complex routing logic. | Phase 2. |
| **MCP automated installation** | Requires managing npm/npx cross-platform. | Phase 2. Manual install with documentation is sufficient. |
| **.opvpkg package format** | No Hub to distribute to. Local sideloading uses raw directories. | Phase 2 alongside Hub Client. |
| **MCP process pool + reference counting** | Optimization. Simple start/stop per invocation works for Phase 1 load. | Phase 2 when multiple skills share MCP servers. |

---

## 8. Conclusion

**Verdict**: Phase 1 is achievable with scope reduction. The full Phase 1 as specified is 60-70% larger than what a small team should attempt first.

**Confidence**: High

**Rationale**: The technology stack is sound. .NET 10, EF Core, SignalR, Spectre.Console, and Microsoft.Extensions.AI are mature and well-documented. The two genuine technology risks (sqlite-vec alpha status and Hangfire.Storage.SQLite bus factor) are mitigable by deferring sqlite-vec and accepting the Hangfire risk. The primary risk is not technology but scope. The requirements document describes a Phase 1 that includes 8 named deliverables but implicitly requires 15+ additional subsystems. The recommended approach is to ship the minimum viable Phase 1 (12 focused deliverables) in 75-100 developer-days, then expand.

### Critical Path

1. **ISkill interface design** -- blocks everything. Define the contract first.
2. **Solution structure + DI + EF Core** -- foundation for all components.
3. **SKILL.md parser + local skill management** -- enables skill development.
4. **IChatClient + OpenAI provider** -- enables LLM interaction.
5. **SignalR AssistantHub + TUI** -- enables user interaction.
6. **Native skill execution (ALC)** -- enables first real skill.
7. **LLM streaming pipeline** -- end-to-end vertical slice.
8. **MCP bridge (simplified)** -- enables MCP ecosystem.
9. **Telegram adapter** -- proves channel adapter pattern.
10. **Hangfire cron + session management + token auth** -- infrastructure completion.

### Top 3 Actions Before Implementation

1. **Specify the ISkill interface precisely.** Input types, output types, error model, cancellation, streaming. This is the single most important design decision.
2. **Define the task domain model.** What is a "task"? How does it relate to user messages, sessions, and Hangfire jobs? This ambiguity permeates the entire spec.
3. **Build the vertical slice first.** User types in TUI -> Assistant calls OpenAI -> streams response back. Prove the full pipeline before branching into adapters, skills, and scheduling.

---

## 9. Appendices

### Sources Consulted

- `docs/project_requirements.md` (1248 lines, read in full)
- `docs/research/dotnet_stack_analysis.md` (635 lines, read in full)
- `docs/research/dotnet_infrastructure_analysis.md` (712 lines, read in full)
- `docs/research/skill_format_analysis.md` (720 lines, read in full)
- NuGet: sqlite-vec 0.1.7-alpha.2.1 (https://www.nuget.org/packages/sqlite-vec/)
- NuGet: ModelContextProtocol 1.0.0 (https://www.nuget.org/packages/ModelContextProtocol)
- NuGet: Hangfire.Storage.SQLite 0.4.2 (https://www.nuget.org/packages/Hangfire.Storage.SQLite/)
- NuGet: MicrosoftAgentFramework 1.0.0-rc2 (https://www.nuget.org/profiles/MicrosoftAgentFramework/)
- agentskills.io specification (https://agentskills.io/specification)
- GitHub: sqlite-vec releases (https://github.com/asg017/sqlite-vec/releases)
- GitHub: Hangfire.Storage.SQLite (https://github.com/raisedapp/Hangfire.Storage.SQLite)

### Data Transparency

- **Found**: Current NuGet package versions and release status for all critical dependencies. SKILL.md adoption metrics (26+ platforms, 20K GitHub stars). MCP C# SDK stable at v1.0.0. MAF at RC2 status. Hangfire.Storage.SQLite last release April 2024.
- **Not Found**: Exact developer-day estimates for .NET plugin systems using AssemblyLoadContext (no comparable open-source projects found). Performance benchmarks for SignalR streaming with Spectre.Console live rendering. Real-world examples of SKILL.md -> ISkill translation patterns.
