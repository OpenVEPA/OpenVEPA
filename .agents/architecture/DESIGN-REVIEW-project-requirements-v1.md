# Design Review: project_requirements.md -- Pre-Implementation Architectural Review

**Reviewer**: Architect Agent
**Date**: 2026-02-03
**Document**: `docs/project_requirements.md` (sections 1-11)
**Research Context**: `docs/research/dotnet_stack_analysis.md`, `mcp_dotnet_analysis.md`, `websocket_communication_analysis.md`, `llm_integration_analysis.md`
**Review Phase**: Pre-Planning (before any code exists)

---

## Executive Summary

The requirements document is architecturally strong. The layered design (Core abstractions, DI-swappable infrastructure, protocol-driven extensibility) follows sound principles. The research documents provide rigorous backing for every major technology choice.

17 findings across 3 severity levels. 4 critical issues require resolution before implementation begins. 8 important concerns should be addressed during early implementation. 5 suggestions can evolve during development.

**Verdict**: [PASS with conditions] -- Proceed to implementation planning after resolving the 4 critical findings.

---

## Findings

### CRITICAL -- Must resolve before implementation

---

#### C1: ISkill Interface Contract Inconsistency

**Section**: 3.2.3, 3.2.6 (requirements) vs. 5.2 (dotnet_stack_analysis) vs. 4.3 (mcp_dotnet_analysis)
**Severity**: Critical

**Issue**: Three different ISkill signatures appear across documents.

| Source | Signature |
|--------|-----------|
| `dotnet_stack_analysis.md` line 188-194 | `Name`, `Description`, `ExecuteAsync(SkillInput, CancellationToken)` |
| `mcp_dotnet_analysis.md` line 253-256 | `Manifest`, `ExecuteAsync(SkillInput, CancellationToken)` |
| `project_requirements.md` section 3.2.3 | Prose only: "All three modes present the same ISkill interface" -- no formal definition |

The requirements document never formally defines ISkill. It delegates to research docs that disagree.

**Recommendation**: Define the canonical ISkill contract in section 3.2 of the requirements document. One definition, one source of truth. The `Manifest` property approach (from the MCP analysis) is better because it consolidates metadata into a single object and avoids property proliferation as the interface grows:

```csharp
public interface ISkill
{
    SkillManifest Manifest { get; }
    Task<SkillResult> ExecuteAsync(SkillInput input, CancellationToken ct);
}
```

This interface also needs: `ValidateInputAsync` for pre-execution validation, and `DisposeAsync` for resource cleanup (IAsyncDisposable). Skills that manage MCP connections or file handles need deterministic cleanup.

---

#### C2: Missing Error Contract for Skills

**Section**: 3.2.3, 5.8
**Severity**: Critical

**Issue**: `SkillResult` is referenced but never defined. The failure handling strategy in section 5.8 specifies retry with exponential backoff, escalation, and fallback. These strategies depend on error classification (transient vs. permanent, retriable vs. fatal). Without a typed error contract, the retry logic cannot distinguish between "network timeout -- retry" and "invalid input -- do not retry."

**Recommendation**: Define SkillResult as a discriminated result type:

```csharp
public record SkillResult
{
    public bool Success { get; init; }
    public object? Data { get; init; }
    public SkillError? Error { get; init; }
}

public record SkillError
{
    public SkillErrorKind Kind { get; init; }  // Transient, Permanent, RateLimited, AuthFailed, Timeout
    public string Message { get; init; }
    public Exception? Inner { get; init; }
}
```

This enables the retry strategy in section 5.8 to branch on `Kind` without catching and inspecting exceptions.

---

#### C3: Circular Dependency Risk -- Skills Needing LLM Access

**Section**: 3.2.2 (LLM Requirement field), 5.1 (Agent Components)
**Severity**: Critical

**Issue**: The dependency graph in `dotnet_stack_analysis.md` line 161-173 shows:

```
Cli --> Core <-- Hub
          ^
     Skills | Providers | Storage | Scheduler
```

Skills depend on Core. Core defines ISkill. But section 3.2.2 states skills can declare an "LLM Requirement" field, meaning some skills need to invoke an LLM during execution. The LLM provider (IChatClient) is managed by the Provider layer. If Skills reference Providers, the dependency direction breaks:

```
Core <-- Skills --> Providers --> Core  (circular)
```

**Recommendation**: Introduce a `SkillExecutionContext` that the runtime passes to skills at invocation time. Skills never reference the Providers project directly. Instead, the context provides pre-configured services:

```csharp
public interface ISkill
{
    SkillManifest Manifest { get; }
    Task<SkillResult> ExecuteAsync(SkillInput input, SkillExecutionContext context, CancellationToken ct);
}

// Defined in Core
public class SkillExecutionContext
{
    public IChatClient? ChatClient { get; init; }
    public ILogger Logger { get; init; }
    public IConfiguration SkillConfig { get; init; }
    // Future: memory, user context, etc.
}
```

The Skill Runtime (in Core or a host-level assembly) populates the context before invoking the skill. The skill consumes services without knowing their origin. This is the same pattern as ASP.NET Core's HttpContext.

---

#### C4: Project Structure Incomplete -- Missing Assemblies

**Section**: dotnet_stack_analysis.md section 5.1 (Project Layout)
**Severity**: Critical

**Issue**: The project layout defines 7 source projects. The requirements document adds 3 major subsystems that have no project home:

| Subsystem | Defined In | Missing Project |
|-----------|-----------|-----------------|
| SignalR Hubs (AssistantHub, TaskHub, SystemHub) | Section 4.2 | No `OpenVEPA.Communication` or `OpenVEPA.Server` |
| Agent Runtime (loading, activation, lifecycle) | Section 3.1, 5.1-5.7 | No `OpenVEPA.Agents` |
| Channel Adapters (Telegram, Discord, Email) | Section 4.3 | No `OpenVEPA.Channels.*` |

The CLI project (`OpenVEPA.Cli`) cannot host SignalR hubs, agent runtime, and channel adapters without becoming a monolith. These concerns require separate assemblies to maintain the dependency direction principle.

**Recommendation**: Expand the project structure:

```
src/
    OpenVEPA.Core/              # Interfaces, domain types, SkillExecutionContext
    OpenVEPA.Server/            # ASP.NET Core host, SignalR hubs, middleware
    OpenVEPA.Cli/               # CLI commands, TUI (references Server for self-hosting)
    OpenVEPA.Skills.Runtime/    # Skill loading, ALC management, MCP bridge
    OpenVEPA.Agents.Runtime/    # Agent loading, lifecycle, multi-agent coordination
    OpenVEPA.Hub.Client/        # Hub registry client (browse, install, update)
    OpenVEPA.Providers/         # IChatClient registrations per provider
    OpenVEPA.Storage/           # EF Core, file storage, vector storage
    OpenVEPA.Scheduler/         # Hangfire integration
    OpenVEPA.Channels.Telegram/ # Telegram adapter (separate assembly)
    OpenVEPA.Channels.Discord/  # Discord adapter
    OpenVEPA.Channels.Email/    # Email adapter
```

Key dependency rules:
- `OpenVEPA.Server` references `Core`, `Skills.Runtime`, `Agents.Runtime`, `Providers`, `Storage`, `Scheduler`
- `OpenVEPA.Cli` references `Server` (self-hosts Kestrel + hubs)
- Each `Channels.*` project references `Core` only (communicates via SignalR client)
- No channel adapter references another channel adapter

---

### IMPORTANT -- Address during early implementation

---

#### I1: IChannelAdapter Interface Underspecified

**Section**: 4.3
**Severity**: Important

**Issue**: Section 4.3 states all adapters implement `IChannelAdapter` with `StartAsync`, `StopAsync`, `SendMessageAsync`, and `OnMessageReceived`. But the interface lacks:

1. **Session mapping**: How does the adapter resolve a platform conversation (Telegram chat ID) to an OpenVEPA session ID? Section 4.5 describes this per-platform, but the interface contract does not enforce it.
2. **Message format translation**: Platform messages (Telegram markdown, Discord embeds) differ from the internal MessageEnvelope. Where does translation happen?
3. **Delivery guarantees**: Does `SendMessageAsync` mean "delivered to platform API" or "delivered to user"?

**Recommendation**: Extend the interface definition in the requirements:

```csharp
public interface IChannelAdapter : IAsyncDisposable
{
    string ChannelId { get; }  // "telegram", "discord", "email"
    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
    Task SendMessageAsync(string sessionId, OutboundMessage message, CancellationToken ct);
    event Func<InboundMessage, Task> OnMessageReceived;
    Task<string> ResolveSessionIdAsync(PlatformContext context, CancellationToken ct);
}
```

The `ResolveSessionIdAsync` method formalizes the session mapping logic that section 4.5 describes narratively.

---

#### I2: Session-Task Relationship Crosses Hub Boundaries

**Section**: 4.2, 4.5, 5.4
**Severity**: Important

**Issue**: Sessions are managed in AssistantHub (section 4.5). Tasks are managed in TaskHub (section 4.2). But tasks belong to sessions. When a task completes, the notification must route to the session's connected clients. This creates a cross-hub dependency.

The websocket research doc (section 4.3) justifies three hubs on the basis that "clients connect only to what they need." But a dashboard monitoring tasks still needs the session context to display which conversation spawned each task.

**Recommendation**: Define a shared session-scoped service (`ISessionContext`) that both hubs can access. Tasks carry a `SessionId` field. The TaskHub resolves the session's SignalR group to push notifications. This avoids hub-to-hub coupling while maintaining the session-task relationship.

Alternative: Accept that session ID is a cross-cutting identifier passed with every hub method call, similar to how HTTP APIs pass auth tokens.

---

#### I3: MCP Reference Counting Lacks Concurrency Safety

**Section**: 3.4 (MCP server lifecycle)
**Severity**: Important

**Issue**: The MCP dependency manager uses reference counting to track which skills use each MCP server. Section 3.4 describes: "Increment reference count" on activation, "decrement reference count" on deactivation, "if count reaches 0, stop the MCP server process."

In a concurrent system where multiple skills activate/deactivate simultaneously (section 5.6), this reference counting needs:
1. Atomic increment/decrement operations
2. Crash recovery (what if the process dies between decrement and stop?)
3. Timeout handling (what if an MCP server hangs during shutdown?)

**Recommendation**: Use `ConcurrentDictionary` with atomic operations for the reference count. Persist the consumer list to the MCP registry file (`mcp-servers/registry.json`) on every change. On startup, reconcile the registry against actually running processes. Add a configurable shutdown timeout (default 10 seconds) with force-kill fallback.

---

#### I4: Token Budget Race Condition Across Concurrent Tasks

**Section**: 5.3 (LLM Budget), 5.6 (Concurrent Task Execution)
**Severity**: Important

**Issue**: Per-day budget limits (`maxTokensPerDay`, `maxCostPerDay`) are shared resources. Multiple concurrent tasks consume tokens simultaneously. Without atomic budget accounting, two tasks could each check "am I within budget?" at the same moment, both see headroom, and both proceed to exceed the daily limit.

**Recommendation**: Implement a centralized `IBudgetLedger` service with atomic `TryReserve(estimatedTokens)` and `Commit(actualTokens)` operations. The ledger holds the daily running total in memory with periodic persistence to SQLite. Use `SemaphoreSlim` or `lock` for thread safety. Reserve pessimistically (estimate high), commit actual after LLM response.

---

#### I5: Dual Queue System Boundary Undefined

**Section**: 5.4, 5.5, 5.6, 8.2
**Severity**: Important

**Issue**: Two queuing mechanisms exist:

| Queue | Technology | Purpose |
|-------|-----------|---------|
| Task dispatch | `System.Threading.Channels` (in-memory) | Agent task dispatch, concurrent execution |
| Durable/scheduled | Hangfire (SQLite-backed) | Scheduled tasks, checkpoint-based resumability |

The boundary between them is not defined. Which tasks go where? If a user sends "research quantum computing" via chat, does the task enter Channels (immediate) or Hangfire (durable)? If it enters Channels and the process crashes, the task is lost despite section 5.4 promising resumability.

**Recommendation**: All user-initiated tasks enter Hangfire first (durability guarantee). Hangfire dispatches execution to a Channel-based worker pool. The Channel is the execution mechanism; Hangfire is the persistence mechanism. This gives every task crash recovery and every execution path concurrency control.

Document this as: "Hangfire owns task persistence. Channels own task execution. Every task is a Hangfire job that runs via a Channel consumer."

---

#### I6: User Preference Service Lacks Architectural Boundary

**Section**: 5.9
**Severity**: Important

**Issue**: User preference learning is a cross-cutting concern. The preference system needs to:
- Observe every agent interaction (cross-agent)
- Store preferences with confidence scoring (storage layer)
- Inject relevant preferences into LLM context (agent layer)
- Allow user review/edit (UI layer)

Section 5.9 describes the data model and CLI commands but does not define where the inference logic lives architecturally. Without a service boundary, preference inference will fragment across agent implementations.

**Recommendation**: Define `IUserProfileService` in Core with clear methods:

```csharp
public interface IUserProfileService
{
    Task<UserPreferences> GetRelevantPreferencesAsync(string taskDomain, CancellationToken ct);
    Task RecordObservationAsync(string sessionId, PreferenceObservation observation, CancellationToken ct);
    Task SetExplicitPreferenceAsync(string key, object value, CancellationToken ct);
}
```

The service is injected into the SkillExecutionContext (see C3). Preference inference runs as a post-interaction pipeline step in the Agent Runtime, not inside individual skills.

---

#### I7: Missing Document Storage Abstraction

**Section**: 8.2, 8.3
**Severity**: Important

**Issue**: The infrastructure design follows "define an interface, provide a default, swap via configuration" for relational data (EF Core), cache (HybridCache), vectors (Microsoft.Extensions.VectorData), and queues (Channels to MassTransit). But document storage has no abstraction:

| Storage | Default | Upgrade | Abstraction |
|---------|---------|---------|-------------|
| Relational | SQLite (EF Core) | PostgreSQL | `DbContext` |
| Cache | HybridCache | Redis | `IDistributedCache` |
| Vectors | sqlite-vec | Qdrant | `Microsoft.Extensions.VectorData` |
| Documents | File-based JSON | LiteDB/MongoDB | **None** |

Section 8.2 specifies "File-based JSON" for agent outputs and documents. Section 8.3 lists LiteDB and MongoDB as upgrades. Without an `IDocumentStore` interface, the file-based implementation will leak into business logic.

**Recommendation**: Define a minimal document store abstraction in Core:

```csharp
public interface IDocumentStore
{
    Task<T?> GetAsync<T>(string collection, string id, CancellationToken ct);
    Task UpsertAsync<T>(string collection, string id, T document, CancellationToken ct);
    Task DeleteAsync(string collection, string id, CancellationToken ct);
    IAsyncEnumerable<T> QueryAsync<T>(string collection, Expression<Func<T, bool>> filter, CancellationToken ct);
}
```

Default: file-per-document in `data/documents/{collection}/{id}.json`. Upgrade: LiteDB or MongoDB.

---

#### I8: No Health Check or Readiness Protocol

**Section**: 4.2 (SystemHub), 8.1 (Deployment)
**Severity**: Important

**Issue**: SystemHub exposes `GetStatus()` and `HealthUpdate()`. But there is no definition of what "healthy" means. For Docker deployments (section 8.1), container orchestrators need HTTP health check endpoints. For the TUI dashboard, the system needs to report which subsystems are operational.

**Recommendation**: Define a health model:

```
GET /health       -> 200 OK / 503 Unhealthy (for container orchestrators)
GET /health/ready -> checks: database reachable, at least one LLM provider configured, SignalR accepting connections
GET /health/live  -> checks: process is responsive
```

Use ASP.NET Core's built-in `IHealthCheck` interface and `MapHealthChecks()`. Each subsystem registers a health check (database, LLM, scheduler, MCP servers). SystemHub's `HealthUpdate` pushes the aggregated result.

---

### SUGGESTIONS -- Can evolve during implementation

---

#### S1: Consider Single Config File for Zero-Config Default

**Section**: 8.5
**Severity**: Suggestion

**Issue**: The default config splits across 4+ files: `appsettings.json`, `providers.json`, `channels.json`, `schedules.json`, plus per-agent and per-skill `config.json`. For a "zero-config default" system (Design Principle 3), this is a lot of files to manage before the user has done anything.

**Recommendation**: Start with a single `appsettings.json` containing all sections. Split into separate files only when the single file becomes unwieldy. `Microsoft.Extensions.Configuration` supports multiple sources. You can add `providers.json` later without changing any code:

```csharp
builder.Configuration.AddJsonFile("config/appsettings.json", optional: false);
builder.Configuration.AddJsonFile("config/providers.json", optional: true);  // optional override
```

The key insight: the configuration layering system supports this evolution. You do not need to commit to the split-file layout at design time.

---

#### S2: AssemblyLoadContext Unloading Fragility

**Section**: 3.2.6
**Severity**: Suggestion

**Issue**: Section 3.2.6 specifies `isCollectible: true` for hot-swap skill unloading. In practice, ALC unloading in .NET is fragile. Any leaked reference (timer callback, event handler, static field, closure capturing a service) prevents unloading silently. The unload succeeds from the caller's perspective, but the assembly stays in memory.

**Recommendation**: Document this as a known limitation. Implement unloading as best-effort with logging when an ALC fails to unload within a timeout. Provide a fallback: `openvepa skill reload --force` restarts the process. Do not promise seamless hot-swap until the implementation proves reliable under testing.

---

#### S3: Agent-to-Agent Communication Needs Supervision Model

**Section**: 5.7
**Severity**: Suggestion

**Issue**: Phase 3 describes agents spawning sub-tasks that create new agent instances. This creates a tree of agents. Without a supervision model, failures in child agents have no defined propagation path. If a sub-agent fails, does the parent agent retry? Abort? How deep can nesting go?

**Recommendation**: When Phase 3 design begins, define an agent supervision strategy. Options:
1. **Flat**: All agents report to the Assistant. No nesting beyond one level.
2. **Hierarchical**: Parent agents supervise children with configurable failure policies (restart, escalate, ignore).
3. **Erlang-style**: Let it crash, supervisor restarts with clean state.

Defer the decision but document it as a required ADR before Phase 3 implementation.

---

#### S4: Version Negotiation for Hub Packages

**Section**: 3.3 (Hub), 3.2.1 (SKILL.md `compatibility` field)
**Severity**: Suggestion

**Issue**: SKILL.md includes `compatibility: openvepa >= 1.0`. The Hub installation flow (section 3.3) verifies package structure but does not define runtime version checking. What happens when a user running OpenVEPA 1.0 installs a skill requiring 1.2? Silent failure? Blocked install?

**Recommendation**: The Hub Client should compare the skill's `compatibility` constraint against the running OpenVEPA version. Block installation with a clear error if the constraint is not satisfied. Implement using NuGet's `VersionRange` parsing (already available in .NET).

---

#### S5: Consider Explicit Startup Dependency Graph

**Section**: 3.2.6, 3.4, 4.1, 8.2
**Severity**: Suggestion

**Issue**: System startup involves initializing multiple subsystems in a specific order: database migration, configuration loading, skill discovery, MCP server registry reconciliation, SignalR hub startup, channel adapter activation. The requirements document does not define startup ordering or failure handling during boot.

**Recommendation**: Use `IHostedService` with explicit ordering. Define a startup sequence:

```
1. Configuration loading
2. Database migration (EF Core)
3. Skill discovery (scan directories, parse SKILL.md frontmatter)
4. MCP registry reconciliation (stop orphaned servers, update status)
5. LLM provider health check (verify at least one provider responds)
6. SignalR hub activation
7. Channel adapter startup (each adapter independently)
8. Scheduler activation (Hangfire)
```

Each step logs success/failure. Steps 7-8 are independent and can fail without blocking the core. A failed channel adapter does not prevent the TUI from working.

---

## Cross-Cutting Observations

### What the Architecture Gets Right

1. **MCP as implementation detail, not peer concept** (section 3.4). This is the correct inversion. Skills are the primary abstraction. MCP is plumbing. This prevents the architecture from being coupled to a 1.5-year-old protocol.

2. **Progressive disclosure for token efficiency** (sections 3.2.1, 3.2.6, 6.4). Loading skill metadata (~100 tokens) at startup and full body on activation is a concrete, measurable optimization. This pattern is consistently applied to both skills and agents.

3. **IChatClient as the LLM foundation** (section 6.1). Building on M.E.AI with MAF as an additive layer (not foundational) is the correct architecture. If MAF changes direction, OpenVEPA can swap the orchestration layer without rewriting provider integrations.

4. **Hub as a separate repository** (section 3.3). Clean boundary. The main repo contains only the Hub Client. This prevents Hub server concerns from bleeding into the assistant.

5. **Three SignalR hubs separated by concern** (section 4.2). AssistantHub (high frequency), TaskHub (medium), SystemHub (low). Clients subscribe to what they need. The websocket research doc correctly notes this is reversible (30-minute refactor to merge).

6. **DI-swappable infrastructure** (section 8.2-8.3). SQLite to PostgreSQL, Channels to MassTransit, in-memory to Redis. Each upgrade is a DI registration change. The pattern is applied consistently across all infrastructure categories.

### Dependency Direction Summary

```
                    OpenVEPA.Cli
                        |
                   OpenVEPA.Server
                   /    |    \     \
          Skills.Runtime |  Agents.Runtime  Channels.*
                  |      |      |
             [Core interfaces: ISkill, IChatClient, IChannelAdapter, IDocumentStore]
                  |      |      |
              Providers Storage Scheduler
```

Direction is correct: all arrows point toward Core. No lateral dependencies between Providers, Storage, and Scheduler. Channel adapters are leaf nodes that communicate only via SignalR client (no direct Core dependency for business logic).

### Risk Assessment

| Risk | Probability | Impact | Finding |
|------|------------|--------|---------|
| ISkill interface churn during Phase 1 | High | Medium | C1, C2, C3 |
| Token budget overrun in concurrent tasks | Medium | High | I4 |
| Task loss on crash (Channels are in-memory) | Medium | High | I5 |
| ALC unloading failures blocking skill updates | Medium | Low | S2 |
| Config file sprawl confusing new users | Low | Low | S1 |

---

## Required ADRs

The following architectural decisions should be formalized as ADRs before or during Phase 1:

| ADR | Topic | Blocking? |
|-----|-------|-----------|
| ADR-001 | ISkill interface canonical definition | Yes -- blocks all skill implementation |
| ADR-002 | SkillExecutionContext pattern for service injection | Yes -- blocks skills needing LLM access |
| ADR-003 | Task persistence: Hangfire-first with Channel execution | Yes -- blocks task resumability |
| ADR-004 | Project/assembly structure and dependency rules | Yes -- blocks solution scaffolding |
| ADR-005 | Error contract and retry classification for skills | No -- can iterate during Phase 1 |
| ADR-006 | Token budget concurrency control mechanism | No -- needed before concurrent tasks ship |

---

## Next Steps

1. Resolve C1-C4 by updating `project_requirements.md` with formal interface definitions and project structure.
2. Create ADR-001 through ADR-004 before solution scaffolding.
3. Route to **milestone-planner** for Phase 1 implementation planning with these findings as constraints.

Architecture review complete. Recommend orchestrator routes to **milestone-planner** for Phase 1 planning with this review as input.
