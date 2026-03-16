# Plan Critique: OpenVEPA Project Requirements (docs/project_requirements.md)

## Verdict

**[NEEDS REVISION]**

**Confidence**: High (95%). Based on full reading of the 1248-line requirements document and all 7 research documents.

**Rationale**: The document is strong on technology selection, architecture decisions, and extensibility design. It is weak on error paths, security boundaries, data lifecycle, first-run experience, and testability criteria. 14 critical findings and 18 important findings must be addressed before this document can serve as a reliable implementation spec.

---

## Strengths

- Technology selections are well-researched and justified with dedicated research docs
- Progressive disclosure for skills/agents is a sound token-efficiency strategy
- Zero-config default stack with clear upgrade paths reduces initial deployment friction
- Clear separation between Hub server (separate repo) and Hub client (this repo)
- Phase structure (Foundation, Content/Reach, Multi-Agent) provides reasonable delivery sequencing
- Configuration layering (CLI > env > secrets > JSON > defaults) follows .NET conventions
- MCP dependency management with reference counting is well-specified

---

## Issues Found

### Critical (Must Fix)

**C-01: No error handling for SQLite WAL mode concurrent access**
- **Section**: 8.2 Default Stack, 4.5 Session Management
- **Issue**: SQLite in WAL mode supports single writer + multiple readers. The requirements specify multiple concurrent sessions (4.5) and concurrent task execution (5.6), both writing to SQLite. The document does not address write contention, queuing, or failure behavior when concurrent writes collide.
- **Fix**: Define a write serialization strategy. Options: single `DbContext` writer with `Channel<T>` queue, `SemaphoreSlim(1,1)` guarding writes, or EF Core retry policy for `SQLITE_BUSY`. Document the chosen approach and its failure mode.

**C-02: No skill sandboxing for native skills (acknowledged but unresolved)**
- **Section**: 9.3 Hub Security, 11 Open Questions (#4)
- **Issue**: Native skills run in-process via `AssemblyLoadContext`. ALC provides dependency isolation only, not permission isolation. A malicious or buggy native skill can access the file system, network, and all process memory. The document acknowledges this in Open Questions but provides no mitigation for Phase 1. Users install skills from the Hub in Phase 1.
- **Fix**: For Phase 1, define a minimum viable trust model. Options: (a) native skills are built-in only, Hub skills must be MCP-bridge (process-isolated), (b) signature verification required for native skills, (c) explicit "trust this publisher" prompt. Pick one and document it.

**C-03: Token storage security model is incomplete**
- **Section**: 9.2 Authentication and Access, 8.5 (tokens.json)
- **Issue**: Tokens are "stored hashed" in `tokens/tokens.json`. The document does not specify: (a) which hash algorithm, (b) whether tokens are salted, (c) how the original token is returned to the user on creation (stored in memory only? displayed once?), (d) what happens if `tokens.json` is corrupted or deleted. File-based hashed token storage has known weaknesses compared to database-backed stores.
- **Fix**: Specify: SHA-256 or bcrypt with per-token salt. Token displayed exactly once on creation. Token recovery is not possible (revoke and recreate). Move token storage to the SQLite database for atomic writes and corruption resilience.

**C-04: No data migration or schema evolution strategy**
- **Section**: 8.2, 8.5
- **Issue**: The document specifies EF Core with SQLite and mentions "migrations" in research docs, but the requirements never define how schema changes are handled across version upgrades. When a user runs `dotnet tool update -g openvepa`, what happens to their `openvepa.db`? What if migration fails mid-flight?
- **Fix**: Add a section on data migration strategy: (a) EF Core Migrations applied on startup, (b) automatic backup of `openvepa.db` before migration, (c) rollback procedure if migration fails, (d) version compatibility matrix (can v1.2 read v1.0 data?).

**C-05: No definition of `ISkill` interface contract**
- **Section**: 3.2 Skill-First Architecture
- **Issue**: The document references `ISkill` 12 times as the core abstraction but never defines its method signatures, return types, or error contract. The research doc shows a basic `ExecuteAsync(SkillInput, CancellationToken)` signature, but the requirements document itself does not specify: input validation behavior, cancellation semantics, timeout handling, or what `SkillResult` contains (success/failure, output type, metadata).
- **Fix**: Add an `ISkill` interface specification section with: method signatures, `SkillInput` structure (must include task context, parameters, cancellation token), `SkillResult` structure (success flag, output payload, error info, token usage), and error contract (does `ISkill` throw or return error results?).

**C-06: Checkpointing mechanism is underspecified**
- **Section**: 5.4 Long-Running Tasks
- **Issue**: The document states tasks "periodically save state to the database" and "resume from the last checkpoint." It does not specify: (a) checkpoint frequency (time-based? step-based?), (b) what "state" is serialized (entire agent memory? just step outputs?), (c) maximum checkpoint size, (d) what happens when checkpoint serialization fails, (e) how agent context (LLM conversation history) is resumed -- the LLM has no memory of previous turns after restart.
- **Fix**: Define checkpoint granularity (after each completed step), checkpoint contents (task plan, completed step outputs, trimmed conversation summary, accumulated context), and context restoration strategy (re-inject summarized history into LLM on resume, not full conversation replay).

**C-07: No first-run / onboarding experience defined**
- **Section**: (missing)
- **Issue**: The document assumes the user already has API keys, knows how to configure providers, and understands the skill system. There is no specification for: (a) what happens when the user runs `openvepa start` for the first time, (b) guided setup for LLM provider configuration, (c) what the assistant does when no LLM provider is configured, (d) default skill set on fresh install.
- **Fix**: Add a "First Run Experience" section covering: interactive setup wizard via TUI, LLM provider configuration flow (detect Ollama locally, prompt for API keys), initial skill set (built-in skills that work without Hub), and graceful degradation when no LLM is configured.

**C-08: Privacy routing rules are vague**
- **Section**: 9.1 Data Classification, 6.3 Provider Requirements
- **Issue**: Data classification defines three categories (Public, Private, Configurable) but does not specify: (a) who classifies data -- the user, the assistant, or a rules engine?, (b) what happens when classification is ambiguous, (c) how classification is enforced at the code level (middleware? skill-level check?), (d) default classification when unspecified. "Configuration-driven, per-skill and per-task" is too vague for implementation.
- **Fix**: Specify the classification mechanism: default all data to "Private" (fail-safe). User configures per-skill overrides. Classification is checked in `IChatClient` middleware before sending to any provider. Add concrete examples of routing rules.

**C-09: Conversation summarization has no specification**
- **Section**: 6.4 Token Minimization Strategy
- **Issue**: "Periodically summarize long conversations to compress context" is listed as a strategy but has zero implementation detail. This is a hard problem. When does summarization trigger? What compression ratio is targeted? How is summary quality verified? What happens when the summary loses critical context? This directly impacts the core goal of autonomous task completion.
- **Fix**: Define: trigger condition (e.g., when conversation exceeds 75% of model context window), summarization approach (LLM-based summarization of older turns, keeping recent N turns verbatim), summary validation (none initially -- accept LLM output), and user-visible indicator that summarization occurred.

**C-10: `${env:VAR_NAME}` syntax is non-standard**
- **Section**: 8.5 Configuration (Skill config example)
- **Issue**: The skill config example uses `"apiKey": "${env:BRAVE_SEARCH_API_KEY}"` with a custom interpolation syntax. `Microsoft.Extensions.Configuration` does not support this syntax natively. This requires a custom configuration provider or value resolver. The document does not acknowledge this as custom work.
- **Fix**: Either (a) remove the custom syntax and document that sensitive values must use environment variables or user-secrets (the standard .NET approach), or (b) explicitly specify that a custom `IConfigurationProvider` will resolve `${env:...}` placeholders, and add this to Phase 1 scope.

**C-11: No backup/restore specification**
- **Section**: 11 Open Questions (#6)
- **Issue**: Backup strategy is listed as an open question. For a personal assistant storing sessions, learned preferences, task history, and scheduled jobs, data loss is a high-impact failure. This should not be deferred.
- **Fix**: Define a minimum viable backup: (a) `openvepa backup` CLI command that copies `~/.openvepa/` to a timestamped archive, (b) automatic pre-upgrade backup, (c) `openvepa restore` from archive. This is 1-2 days of implementation and prevents data loss from the start.

**C-12: Autonomy Level 2 "low-risk" vs "high-impact" not defined**
- **Section**: 5.2 Autonomy Levels
- **Issue**: Level 2 (Semi-Autonomous) says "executes low-risk actions, asks approval for high-impact decisions." The document never defines what constitutes "low-risk" or "high-impact." Without a classification, every implementation will interpret this differently. Level 4 adds "high-risk actions (financial, file deletion, external communications) require explicit configuration" but does not enumerate what these are.
- **Fix**: Add a risk classification table. Example: Low-risk = read-only operations, web search, summarization. Medium-risk = sending messages, creating files. High-risk = file deletion, financial transactions, external API calls with side effects. Map each risk level to autonomy level thresholds.

**C-13: Message protocol version field has no evolution strategy**
- **Section**: 4.4 Message Protocol
- **Issue**: The message envelope includes a `Version` field "for forward compatibility" but does not specify: (a) what happens when a client sends Version 2 to a server that only knows Version 1, (b) whether older versions are supported simultaneously, (c) what constitutes a breaking change requiring version bump. Without rules, the version field is decoration.
- **Fix**: Define versioning policy: server accepts Version N and N-1. Unknown version returns an error with supported versions. Breaking changes (field removal, type changes) increment version. Additive changes (new optional fields) do not.

**C-14: No logging/audit specification for LLM interactions**
- **Section**: 6.3 Provider Requirements, 8.5
- **Issue**: The document mentions "token/cost usage logs" in the database and "cost tracking" via middleware, but does not specify what is logged per LLM call: prompt content? response content? latency? model used? error details? For debugging autonomous agents, full audit of LLM interactions is essential. The document also does not address log retention or storage growth.
- **Fix**: Define LLM audit log schema: timestamp, provider, model, input tokens, output tokens, cost, latency_ms, session_id, task_id, skill_name, success/failure, error_message. Specify retention policy (default: 30 days, configurable). Estimate storage growth (rough: 1KB per LLM call).

---

### Important (Should Fix)

**I-01: Hub Client has no offline caching**
- **Section**: 3.3 Hub
- **Issue**: The Hub Client can browse, search, and install from the Hub. The document does not specify what happens when the Hub is temporarily unreachable during a search or install. No mention of local metadata cache, retry behavior, or timeout configuration.
- **Fix**: Specify: Hub Client caches last-known skill index locally. Install operations timeout after 30 seconds with user-facing error. Search falls back to local cache when Hub is unreachable.

**I-02: Scheduled task execution context is undefined**
- **Section**: 5.5 Scheduled Tasks
- **Issue**: Scheduled tasks execute a "prompt" at the scheduled time. The document does not specify: which LLM provider is used (the default? the one configured for the bound agent?), what session context the task runs in (new session? dedicated scheduled-task session?), what happens when a scheduled task fails (retry? skip? notify?).
- **Fix**: Specify: scheduled tasks create a new ephemeral session per execution, use the bound agent's default LLM provider, and follow the failure handling rules from 5.8 (retry with backoff, then notify).

**I-03: MCP server crash recovery not specified**
- **Section**: 3.4 MCP Integration
- **Issue**: MCP dependency management covers start/stop lifecycle but not crash recovery. What happens when an MCP server process crashes mid-operation? Is the skill invocation retried? Is the MCP server auto-restarted? What if the MCP server enters a crash loop?
- **Fix**: Add MCP server health monitoring: (a) detect process exit, (b) auto-restart once with backoff, (c) after N consecutive crashes, mark MCP server as degraded and disable dependent skills with user notification, (d) expose health status in SystemHub.

**I-04: Channel adapter message size limits not specified**
- **Section**: 4.3 Messaging Channels
- **Issue**: Each messaging platform has different message size limits (Telegram: 4096 chars, Discord: 2000 chars, Email: effectively unlimited). The document does not specify how long assistant responses are handled across channels. Truncation? Multi-message splitting? Attachment fallback?
- **Fix**: Define per-channel message handling: responses exceeding platform limits are split into multiple messages with continuation indicators. Long-form outputs (reports, code) are sent as file attachments where the platform supports it.

**I-05: Session cross-device continuation has no conflict resolution**
- **Section**: 4.5 Session Management
- **Issue**: "A session started from one client can be continued from another." The document does not address what happens when two clients connect to the same session simultaneously. Can both send messages? Who sees the responses? Race conditions on session state?
- **Fix**: Define concurrent session access rules: only one active writer per session. Second connection gets read-only access with notification. Or: both can write, messages are serialized by timestamp, both receive all responses.

**I-06: SKILL.md `allowed-tools` enforcement not specified**
- **Section**: 3.2.1 SKILL.md Format
- **Issue**: SKILL.md includes an `allowed-tools` field but the document never specifies how this is enforced at runtime. Is it advisory (LLM guidance only)? Is it enforced by the runtime (block tool calls not in the list)? What happens when a skill attempts to use a tool not in its allowed list?
- **Fix**: Specify enforcement mechanism: `allowed-tools` is enforced by the Skill Runtime. Tool calls from an `ISkill` execution that reference tools outside the allowed list are blocked and return an error. This is a security boundary.

**I-07: No resource limits for MCP server processes**
- **Section**: 3.4 MCP Integration
- **Issue**: MCP servers run as separate processes. The document specifies no memory limits, CPU limits, or disk usage limits for these processes. A misbehaving MCP server could consume all system resources.
- **Fix**: Define default resource limits: memory cap (configurable, default 512MB), process timeout (configurable, default 5 minutes per operation), disk usage monitoring. On limit violation, kill the process and notify user.

**I-08: `openvepa-autonomy-default` is a string in the agent example**
- **Section**: 3.1.1 Agent Definition Format
- **Issue**: The frontmatter table says autonomy level is `2` (integer), but the YAML example shows `"3"` (string). Inconsistent types between specification and example will cause parsing ambiguity.
- **Fix**: Pick one type (integer) and update the YAML example to `openvepa-autonomy-default: 3` (no quotes).

**I-09: No specification for conversation history pagination**
- **Section**: 4.5 Session Management
- **Issue**: `GetSessionHistory(sessionId)` "supports pagination" but page size, sort order, cursor vs offset pagination, and maximum returnable history are not specified. For sessions with thousands of messages, unbounded history retrieval will cause performance issues.
- **Fix**: Define: default page size (50 messages), cursor-based pagination (by message ID/timestamp), descending order (newest first), maximum single-request limit (200 messages).

**I-10: Model routing (cheap vs expensive) has no decision criteria**
- **Section**: 6.4 Token Minimization Strategy
- **Issue**: "Use cheaper/smaller models for simple tasks and expensive models only for complex reasoning" is stated as a strategy but has zero specification for how routing decisions are made. Is this manual configuration? Automatic classification? LLM-based pre-routing?
- **Fix**: For Phase 1, specify manual configuration: each agent/skill has a `defaultModel` in its config.json. Add a note that automatic model routing is a Phase 2+ optimization.

**I-11: Hangfire dashboard security not addressed**
- **Section**: 8.2 Default Stack
- **Issue**: Hangfire ships with a built-in web dashboard. The document does not specify whether this dashboard is exposed, on what port, or with what authentication. An unsecured Hangfire dashboard allows viewing and manipulating all scheduled jobs.
- **Fix**: Specify: Hangfire dashboard is disabled by default. Enable via configuration with authentication required (reuse existing token-based auth). Dashboard only accessible from localhost unless explicitly configured.

**I-12: No specification for skill/agent version conflicts**
- **Section**: 3.2.5 Skill Lifecycle, 3.3 Hub
- **Issue**: Skills declare `compatibility: openvepa >= 1.0`. The document does not specify what happens when: (a) a skill requires OpenVEPA v2.0 but the user runs v1.5, (b) two skills require conflicting versions of the same MCP server, (c) an agent requires a skill version that conflicts with another agent's requirement.
- **Fix**: Define conflict resolution: (a) block installation with clear error if OpenVEPA version is incompatible, (b) MCP server version conflicts resolved by installing the newer compatible version (semver range), (c) skill dependency conflicts surfaced at install time with resolution options.

**I-13: Email adapter "IDLE push" requires long-lived IMAP connections**
- **Section**: 4.3 Messaging Channels
- **Issue**: The Email adapter uses MailKit with "IDLE push" for real-time email monitoring. IMAP IDLE connections frequently drop (ISP timeouts, server limits). The document does not specify reconnection strategy, polling fallback, or handling of email servers that do not support IDLE.
- **Fix**: Specify: IDLE with automatic reconnection on disconnect. Fallback to polling (configurable interval, default 60 seconds) when IDLE is not supported. Connection health monitoring via SystemHub.

**I-14: No rate limiting on SignalR connections**
- **Section**: 4.1 WebSocket Core
- **Issue**: SignalR hubs accept connections with bearer tokens. There is no specification for rate limiting: maximum connections per token, maximum messages per second, or protection against token abuse. A compromised token could flood the system.
- **Fix**: Specify: per-token connection limit (default 5), per-connection message rate limit (default 60/minute), connection rejected with 429 when limits exceeded.

**I-15: Profile preference conflicts between explicit and inferred**
- **Section**: 5.9 User Profile and Learned Preferences
- **Issue**: Preferences can be "explicit" (user stated) or "inferred" (assistant observed). The document does not specify what happens when an inferred preference contradicts an explicit one. Example: user explicitly sets "communication.responseLength = detailed" but assistant infers "concise" from recent interactions.
- **Fix**: Specify precedence: explicit preferences always override inferred. Inferred preferences are never written if an explicit preference exists for the same key. Confidence score reset when user explicitly sets a value.

**I-16: Semantic Kernel referenced but not in the architecture**
- **Section**: 6.3 Provider Requirements, 8.4 Vector DB Requirements, 10 Technology Stack Summary
- **Issue**: The document references "SK plugins," "SK connector," and "Semantic Kernel connector" for vector DB integration. But Section 6.1 states MAF (Microsoft Agent Framework) "replaces Semantic Kernel (now in maintenance mode)." The document uses SK for vector store connectors while simultaneously deprecating SK. This is contradictory.
- **Fix**: Clarify: SK connectors for `Microsoft.Extensions.VectorData` are used as infrastructure adapters (not orchestration). MAF replaces SK for agent orchestration only. Or: identify if MAF provides its own vector store connectors and plan the migration.

**I-17: `openvepa start` architecture unclear**
- **Section**: 8.1 Deployment
- **Issue**: `openvepa start` starts the assistant, but the document does not specify whether this starts a long-running background service (daemon) or a foreground process that blocks the terminal. If it is a foreground process, how does the user interact (same terminal? separate TUI?). If it is a background daemon, how does the user stop it?
- **Fix**: Specify: `openvepa start` starts a foreground process with the TUI attached. `openvepa start --daemon` starts a background service. `openvepa stop` sends a shutdown signal to the daemon. `openvepa status` checks if the service is running.

**I-18: No specification for OPENVEPA_HOME discovery**
- **Section**: 8.5 Configuration and Data Storage
- **Issue**: The data directory is `~/.openvepa/` with a reference to `OPENVEPA_HOME`. The document does not specify: (a) the exact resolution order (env var first, then `~/.openvepa/`?), (b) behavior when the directory does not exist (auto-create? error?), (c) permissions on the created directory, (d) Windows-specific path (`%USERPROFILE%\.openvepa\` or `%APPDATA%\openvepa\`?).
- **Fix**: Specify: (1) `OPENVEPA_HOME` env var if set, (2) `~/.openvepa/` on Linux/macOS, `%APPDATA%\openvepa\` on Windows. Auto-create on first run with user-only permissions (700 on Unix). Error if path is not writable.

---

### Nice to Have (Can Address During Implementation)

**N-01: No health check endpoint specification**
- **Section**: 4.2 Hub Endpoints (SystemHub)
- **Issue**: SystemHub mentions "health checks" but does not define what is checked: LLM provider reachability? Database connectivity? MCP server status? Disk space?
- **Fix**: Define health check components during implementation: database ping, LLM provider connectivity (cached, not per-request), MCP server process status, disk space threshold.

**N-02: No telemetry or observability specification**
- **Section**: (missing)
- **Issue**: The document does not mention OpenTelemetry, distributed tracing, or metrics collection. For debugging multi-step autonomous tasks across skills and LLM calls, observability is valuable.
- **Fix**: Add to Phase 2: OpenTelemetry integration for traces (task execution spans) and metrics (LLM latency, token usage, skill execution time). .NET Aspire provides this if adopted later.

**N-03: Docker volume mapping does not match data directory**
- **Section**: 8.1 Deployment
- **Issue**: Docker example maps `-v ~/.openvepa:/data` but the data directory layout in 8.5 uses `~/.openvepa/` with subdirectories `config/`, `data/`, `skills/`, etc. The Docker mapping should mount to the OPENVEPA_HOME root, not a `/data` subdirectory.
- **Fix**: Update Docker example to `-v ~/.openvepa:/root/.openvepa` or set `OPENVEPA_HOME=/data` in the container and document the mapping.

**N-04: No specification for skill/agent uninstallation cleanup**
- **Section**: 3.2.5 Skill Lifecycle
- **Issue**: Skill lifecycle covers install through execute but does not specify uninstall behavior: are skill-specific config files removed? What about cached data? Audit logs referencing the skill?
- **Fix**: Define during implementation: `openvepa skill uninstall <name>` removes the skill directory but preserves audit logs and task history that reference it. Config backup offered before removal.

**N-05: Briefing Center assumes TTS without specifying it**
- **Section**: 7.5 Briefing Center
- **Issue**: "Audio Playback" and "text-to-speech generated" are listed as features. TTS engine is listed as TBD in Section 8.3. The Briefing Center specification depends on an unresolved technology choice.
- **Fix**: Mark audio features as "requires TTS engine selection" and defer to Phase 2. Text-only briefings work in Phase 1.

**N-06: ULID vs GUID not justified**
- **Section**: 4.4 Message Protocol, 4.5 Session Management
- **Issue**: The document specifies ULID for message IDs and session IDs without explaining the choice over standard GUIDs. ULIDs are sortable by time, which is useful, but require a third-party library in .NET.
- **Fix**: Add a one-line rationale: "ULID chosen for time-sortable ordering (chronological message/session listing without secondary index)." Or switch to `Guid.CreateVersion7()` available in .NET 10 (also time-sortable, no third-party dependency).

**N-07: No specification for graceful shutdown**
- **Section**: (missing)
- **Issue**: When the process receives SIGTERM or `openvepa stop`, the document does not specify: (a) how in-progress LLM calls are handled, (b) whether running tasks are checkpointed, (c) SignalR connection drain behavior, (d) maximum shutdown timeout.
- **Fix**: Define during implementation: on shutdown signal, stop accepting new tasks, checkpoint all running tasks, complete or cancel in-flight LLM calls (5-second timeout), drain SignalR connections, then exit.

---

## Questions for Planner

1. **Skill trust boundary**: Is it acceptable for Phase 1 to allow native (in-process) skills from the Hub, or should Hub-distributed skills be restricted to MCP-bridge (process-isolated) until a sandboxing solution is in place?

2. **Single-user assumption**: The document alternates between "personal assistant" (single user) and features like "hundreds of concurrent connections" and multi-user session management. Is Phase 1 strictly single-user? If so, several complexity areas (concurrent write handling, multi-token auth) can be simplified.

3. **MAF maturity**: Microsoft Agent Framework is specified for Phase 3 orchestration. MAF was released in 2025 and is rapidly evolving. What is the fallback if MAF introduces breaking changes or pivots direction before Phase 3?

4. **Offline-first priority**: The core goal says "zero-config default." How important is fully offline operation (no internet, local Ollama only)? This affects error handling design for every feature that touches external services.

---

## Recommendations

1. **Add an Error Handling section** (new Section 5.8.1 or expand 5.8). Define error taxonomy: transient (retry), permanent (fail), degraded (partial result). Map each to user-visible behavior per autonomy level.

2. **Add a First Run Experience section** (new Section 7.6 or 8.6). Specify the onboarding flow, default configuration, and what works out of the box before any LLM provider is configured.

3. **Move backup/restore out of Open Questions** into a requirements section. Data durability is not optional for a personal assistant storing long-term preferences and history.

4. **Add a Data Lifecycle section** (new Section 8.6). Cover: session archival and deletion, log rotation and retention, database growth management, token usage log pruning.

5. **Resolve the Semantic Kernel contradiction** (I-16). Clarify which SK components are still used and which are replaced by MAF.

6. **Define the `ISkill` contract** (C-05). This is the single most important interface in the system and it has no specification.

---

## Approval Conditions

Before approval, the following must be addressed:

1. All 14 Critical issues must have either a resolution or a documented decision to defer with rationale
2. `ISkill` interface contract must be specified (C-05)
3. First-run experience must be defined (C-07)
4. Error handling must be expanded beyond the 4-row table in 5.8 (C-06, C-09, and broader)
5. Security model for Hub-installed native skills must be resolved (C-02)
6. SQLite concurrent write strategy must be documented (C-01)

---

## Checklist Summary

### Completeness
- [x] Core requirements addressed (architecture, tech stack, phases)
- [ ] Error paths defined -- **[FAIL]**: 7 missing error path findings (C-01, C-03, C-06, C-09, I-03, I-13, N-07)
- [ ] Acceptance criteria defined per milestone -- **[FAIL]**: No measurable acceptance criteria for any phase
- [x] Dependencies identified (NuGet packages, external services)
- [ ] Risks documented with mitigations -- **[FAIL]**: Open Questions list risks but no mitigations

### Feasibility
- [x] Technical approach is sound (well-researched)
- [x] Dependencies are available (.NET 10, all NuGet packages GA)
- [ ] Scope is realistic -- **[WARNING]**: Phase 1 scope is large (core + skill runtime + Hub client + CLI/TUI + 3 messaging adapters + 2 LLM providers + full infrastructure)

### Alignment
- [x] Consistent with research documents
- [x] Follows .NET conventions
- [x] Supports stated design principles

### Testability
- [ ] Milestones verifiable -- **[FAIL]**: No acceptance criteria per phase
- [ ] Test strategy defined -- **[FAIL]**: Testing mentioned in tech stack (xUnit) but no test strategy section

### Style Compliance
- [x] Active voice throughout
- [x] Tables used for structured data
- [ ] Quantified where possible -- **[WARNING]**: Some vague terms remain ("hundreds of concurrent connections," "~30-80 MB")
