# OpenVEPA - Project Requirements

> **Single Source of Truth** for all OpenVEPA project requirements.
> Research details live in `docs/research/`. This document captures the decisions.

---

## 1. Overview

### What Is OpenVEPA

OpenVEPA (Virtual Executive Personal Assistant) is a personal AI assistant built on C#/.NET 10. One main agent (the Assistant) handles user requests and delegates work to composable skills. Skills and additional agents are distributed through a Hub registry. Users interact through CLI, TUI, messaging apps, or a web GUI.

### Core Goal

**Autonomous task completion.** The most important goal of OpenVEPA is to fully and autonomously complete any task given by the user. The user defines the goal in a prompt, and the assistant plans, executes, and delivers the result — without requiring constant hand-holding.

### Design Principles

| # | Principle | Meaning |
|---|-----------|---------|
| 1 | **Goal-Driven Autonomy** | Given a task, the assistant plans, executes, and completes it end-to-end. The user provides the goal, the assistant delivers the outcome. |
| 2 | **Token-Efficient** | Minimize LLM token usage at every layer — context packing, progressive loading, caching, and smart prompting. Tokens are the primary cost driver. |
| 3 | **Zero-Config Default** | Works out of the box with no external services. SQLite, file-based storage, in-process queues. |
| 4 | **Protocol-Driven Extensibility** | Skills, agents, and tools integrate through defined interfaces (`ISkill`, `IChatClient`, MCP). |
| 5 | **Skill-First** | Every capability is a skill. Skills are the atomic unit of functionality. |
| 6 | **Lightweight Core** | The core process stays small. Heavy features are optional packages. |
| 7 | **Single-Process First** | One process handles everything by default. Distributed operation is an upgrade path, not a starting point. |
| 8 | **Hub-Distributed** | Skills and agents are packaged, versioned, and shared through a public Hub registry. |

### Technology Platform

| Property | Value |
|----------|-------|
| **Runtime** | .NET 10 (LTS, supported until November 2028) |
| **Language** | C# 14 |
| **Cross-platform** | Windows x64/ARM64, Linux x64/ARM64, macOS x64/ARM64 (Apple Silicon) |

> Research details: `docs/research/dotnet_stack_analysis.md`

---

## 2. Implementation Phases

### Phase 1: Foundation

| Deliverable | Description |
|-------------|-------------|
| **Core Assistant** | Single agent: handles prompts, selects skills, returns results |
| **Skill Runtime** | SKILL.md parser, native + MCP-bridge execution (no hybrid). ALC loader for built-in native skills. |
| **Local Skill Management** | Sideload from local directories. No Hub Client yet (stub interface). |
| **CLI / TUI** | Spectre.Console.Cli, chat mode, streaming responses, skill management commands |
| **LLM Providers** | OpenAI + Ollama via `IChatClient`. Provider swap via config. |
| **One Messaging Channel** | Telegram adapter (single channel proves multi-channel architecture) |
| **Infrastructure** | SQLite/EF Core, file storage, HybridCache, single SignalR hub (AssistantHub) |
| **Scheduled Tasks** | Basic cron jobs via Hangfire (prompt execution on schedule) |
| **Token Tracking** | Log token usage per LLM call. No budget enforcement yet. |
| **Explicit User Preferences** | `openvepa profile set/show/delete`. No implicit learning. |

### Phase 1.5: Hardening (Deferred from Phase 1)

Items unanimously agreed by review agents as too large for Phase 1. Implement after Phase 1 stabilizes.

| Deliverable | Description |
|-------------|-------------|
| **Long-Running Task Checkpointing** | Checkpoint-based resumability for tasks that survive process restarts |
| **Implicit Preference Learning** | Confidence scoring, inference from interactions, confirmation UX |
| **Vector Database** | sqlite-vec integration for embedding-based retrieval |
| **Hybrid Skill Execution** | Native .NET code + MCP server dependencies in a single skill |
| **Cross-Client Session Continuity** | Start on Telegram, continue on TUI (or vice versa) |
| **Hub Client** | Real implementation when OpenVEPA-Hub exists (Phase 1 provides stub interface) |
| **Discord and Email Adapters** | Additional messaging channel adapters beyond Telegram |
| **TaskHub and SystemHub** | Additional SignalR hubs for task lifecycle and system events |
| **Token Budget Enforcement** | Pause/stop/notify when token or cost budgets are exceeded |
| **Briefing Center** | Audio/TTS briefing generation and playback |
| **MCP Server Pooling** | Shared MCP server instances with reference counting (Phase 1: simple start/stop per invocation) |
| **Privacy Routing Rules Engine** | Configuration-driven cloud vs. local LLM routing based on data classification |

### Phase 2: Content and Reach

| Deliverable | Description |
|-------------|-------------|
| **Content Skills** | Summarization, content ingestion (video, podcast, PDF), news/briefing |
| **MCP Bridge** | `McpBridgeSkill` enabling any MCP server to run as an OpenVEPA skill via SKILL.md |
| **Web GUI** | Browser-based interface connected via SignalR JavaScript client |
| **Additional Channels** | Slack, Matrix/Element adapters |
| **LLM Providers (P1)** | Anthropic + Google Gemini integration |

### Phase 3: Multi-Agent and Advanced Orchestration

| Deliverable | Description |
|-------------|-------------|
| **Multi-Agent** | Agent-to-agent communication, Hub-distributed agent packages |
| **Advanced Orchestration** | Microsoft Agent Framework integration for planning, multi-step workflows |
| **Additional Channels** | Signal, WhatsApp adapters (harder APIs, deferred due to complexity) |
| **LLM Providers (P2)** | GitHub Models integration |
| **Developer Tools** | Code Agent, Software Architect Agent, Product Manager Agent |

---

## 3. Core Architecture

### 3.1 Single Assistant Design

OpenVEPA starts with one main agent: the Assistant. This agent acts as the primary orchestrator. It handles user requests directly and delegates to skills. No other agents are required at initial deployment.

Additional agents are installable extensions, not built-in requirements. Users add agents when they need them, from local definitions or from the Hub. Like skills, agents are defined using a markdown-based format (see Section 3.1.1).

**Phase 1 core components:**

| Component | Purpose |
|-----------|---------|
| **Assistant (Main Agent)** | Receives user input, selects skills, executes tasks, returns results |
| **Skill Runtime** | Loads, validates, and executes skills on behalf of the Assistant |
| **Hub Client (stub)** | Stub interface for future Hub connectivity. Real implementation deferred to Phase 1.5 when OpenVEPA-Hub exists. |

**Hub-distributed agents (installed on demand):**

| Agent | Purpose | Earliest Phase |
|-------|---------|----------------|
| **Research Agent** | Deep research via internet, documents, APIs | 1 |
| **Content Ingestion Agent** | Extract information from videos, podcasts, articles, PDFs | 2 |
| **News/Briefing Agent** | Curate daily news summaries, morning briefings (audio + text) | 2 |
| **File Search Agent** | Access and search personal/local files | 2 |
| **Product Manager Agent** | Help define requirements and specifications | 3 |
| **Software Architect Agent** | Design system architecture and technical decisions | 3 |
| **Code Agent** | Write, test, and debug code (multi-agent subsystem) | 3 |

**Future agents (backlog):** Finance/Stock Agent, Calendar/Scheduling Agent, Communication Agent.

#### 3.1.1 Agent Definition Format (`<name>.agent.md`)

Agents are defined using a markdown-based format analogous to SKILL.md. Each agent is a directory containing a `<name>.agent.md` file (e.g., `research-agent.agent.md`) with YAML frontmatter and a Markdown body.

> **Note:** The `AGENT.md` / `AGENTS.md` filename is reserved for project-level context standards (build instructions, code style) and must NOT be used for OpenVEPA agent definitions. OpenVEPA uses `<name>.agent.md` to avoid conflicts.

**Agent directory structure:**

```
research-agent/
├── research-agent.agent.md   # Agent definition (required)
├── references/               # Optional: domain knowledge, guidelines
└── assets/                   # Optional: templates, examples
```

**`<name>.agent.md` frontmatter fields:**

| Field | Required | Description |
|-------|----------|-------------|
| `name` | Yes | Unique identifier (lowercase, hyphen-separated, matches directory). Max 64 chars. |
| `description` | Yes | What the agent does and when to use it. Max 1024 chars. |
| `license` | No | License name or reference to bundled file |
| `metadata` | No | Arbitrary key-value map for extensions (see below) |

**OpenVEPA metadata extensions for agents:**

| Key | Description | Example |
|-----|-------------|---------|
| `openvepa-skills` | Skills this agent requires (must be installed) | `["web-search", "summarize-article"]` |
| `openvepa-autonomy-default` | Default autonomy level (1-4) | `2` |
| `openvepa-llm-requirements` | LLM capabilities needed (streaming, tool-calling, vision) | `["tool-calling", "streaming"]` |
| `openvepa-min-runtime` | Minimum OpenVEPA/.NET version | `"10.0"` |
| `openvepa-category` | Agent category for Hub browsing | `"research"` |

**`<name>.agent.md` Markdown body contains:**

- **System Prompt** — Who/what the agent is (personality, role, expertise)
- **Guidance** — Decision-making rules, boundaries, preferred approaches
- **Hard Restrictions** — What the agent must never do
- **Skill Usage Instructions** — How the agent should use its bound skills
- **Output Format** — Expected response structure

**Example `research-agent.agent.md`:**

```markdown
---
name: research-agent
description: Deep research on any topic via internet, documents, and APIs. Use when the user needs comprehensive analysis, fact-checking, or literature review.
license: MIT
metadata:
  openvepa-skills: ["web-search", "document-analysis", "summarize-article"]
  openvepa-autonomy-default: "3"
  openvepa-llm-requirements: ["tool-calling", "streaming"]
  openvepa-category: "research"
---

# Research Agent

## System Prompt
You are a thorough research analyst. You gather information from multiple sources,
cross-reference facts, and produce well-structured research reports.

## Guidance
- Always cite sources with URLs or document references
- Cross-reference at least 3 independent sources for factual claims
- Distinguish between facts, opinions, and speculation
- Present findings in structured markdown with clear sections

## Hard Restrictions
- Never fabricate sources or citations
- Never present speculation as established fact
- Never access private/restricted content without explicit user permission

## Skill Usage
- Use `web-search` for initial topic exploration and source discovery
- Use `document-analysis` for deep reading of PDFs, articles, and papers
- Use `summarize-article` for condensing long-form content into key findings
```

**Agent discovery and loading:**

Agents follow the same progressive disclosure pattern as skills:
1. **Metadata** (~100 tokens): Frontmatter loaded at startup for all installed agents
2. **Full definition**: Complete `<name>.agent.md` body loaded when agent is activated
3. **References**: Files from `references/` and `assets/` loaded on demand

### 3.2 Skill-First Architecture

Skills are the primary extensibility mechanism. An agent uses skills to accomplish tasks. Every capability the system offers is packaged as a skill. The SKILL is the core concept. MCP is one way a skill can be implemented internally, not a peer concept (see Section 3.4).

#### 3.2.1 SKILL.md Format (agentskills.io Specification)

Every skill is defined by a `SKILL.md` file following the agentskills.io specification. This is a directory-based format combining YAML frontmatter for machine-readable metadata with Markdown body for human-readable instructions.

**Skill directory structure:**

```
skills/
  web-search/
    SKILL.md              # Skill definition (required)
    bin/                   # .NET assembly (native/hybrid skills)
    config.json            # Default configuration
    README.md              # Extended documentation (optional)
  summarize-article/
    SKILL.md
    bin/
    config.json
```

**SKILL.md frontmatter fields:**

| Field | Required | Description |
|-------|----------|-------------|
| `name` | Yes | Unique skill identifier (e.g., `web-search`) |
| `description` | Yes | What the skill does, in plain language |
| `version` | Yes | Semantic version (e.g., `1.0.0`) |
| `license` | Yes | SPDX license identifier |
| `compatibility` | Yes | Runtime requirements (e.g., `openvepa >= 1.0`) |
| `allowed-tools` | No | Tools this skill is permitted to use |
| `metadata` | No | Extensible key-value block for runtime-specific properties |

**OpenVEPA metadata extensions** (inside the `metadata` field):

| Key | Description |
|-----|-------------|
| `type` | Execution mode: `native`, `mcp-bridge`, or `hybrid` |
| `assembly` | .NET assembly filename (native/hybrid skills) |
| `nuget-package` | NuGet package ID if published |
| `mcp-servers` | List of MCP server dependencies (name, version, transport) |
| `input-schema` | Structured definition of required and optional inputs |
| `output-schema` | Structured definition of what the skill returns |
| `llm-requirement` | Whether the skill needs an LLM call and which capabilities |

**Progressive disclosure:** The runtime loads metadata first (~100 tokens). Full instructions load on skill activation. Resource-heavy content (examples, edge cases) loads on demand. This keeps agent context windows small.

**Example SKILL.md:**

```yaml
---
name: web-search
description: Search the web and return structured results
version: 1.2.0
license: MIT
compatibility: openvepa >= 1.0
allowed-tools:
  - http-client
metadata:
  type: hybrid
  assembly: OpenVEPA.Skills.WebSearch.dll
  mcp-servers:
    - name: brave-search
      version: ">=1.0.0"
      transport: stdio
---
```

```markdown
# Web Search Skill

## Instructions
Search the web using the configured search provider...

## Examples
...

## Edge Cases
...
```

#### 3.2.2 Skill Properties

| Property | Description |
|----------|-------------|
| **Name** | Unique identifier (e.g., `web-search`, `summarize-article`) |
| **Description** | What the skill does, in plain language |
| **Version** | Semantic version |
| **Type** | `native` (.NET assembly), `mcp-bridge` (MCP server wrapper), or `hybrid` (native + MCP deps) |
| **Input Schema** | Structured definition of required and optional inputs |
| **Output Schema** | Structured definition of what the skill returns |
| **Dependencies** | Other skills or MCP servers this skill requires |
| **LLM Requirement** | Whether the skill needs an LLM call and which capabilities (e.g., vision, long context) |
| **Source** | Local, built-in, or Hub reference |

#### 3.2.3 Skill Execution Modes

| Mode | Description | Performance | Use Case |
|------|-------------|-------------|----------|
| **Native** | .NET assembly loaded via `AssemblyLoadContext`, implements `ISkill` | Fastest (in-process) | Core skills, performance-critical operations |
| **MCP Bridge** | SKILL.md references one or more MCP servers. OpenVEPA manages processes and translates between `ISkill` and MCP protocol. | Moderate (IPC overhead) | Community MCP tools, polyglot skills |
| **Hybrid** | Native .NET code implements `ISkill` and uses MCP servers as dependencies for specific capabilities. | Mixed (native + IPC) | Skills needing both .NET logic and external tool access |

All three modes present the same `ISkill` interface to the runtime. The agent sees a unified tool list regardless of execution mode.

#### 3.2.4 Core Interface Contract

All skills implement `ISkill`. The runtime never sees execution mode differences. Native, MCP bridge, and hybrid skills all satisfy this contract.

```csharp
public interface ISkill
{
    SkillManifest Manifest { get; }

    Task<SkillResult> ExecuteAsync(
        SkillInput input,
        SkillExecutionContext context,
        CancellationToken ct);
}
```

Skills that manage MCP server connections or other unmanaged resources should also implement `IAsyncDisposable`. The runtime calls `DisposeAsync` on skill unload.

**SkillManifest** consolidates the metadata parsed from SKILL.md frontmatter into a strongly-typed object:

```csharp
public sealed record SkillManifest(
    string Name,
    string Description,
    SemanticVersion Version,
    SkillType Type,                     // Native, McpBridge, Hybrid
    IReadOnlyList<SkillParameter> Inputs,
    IReadOnlyList<SkillOutput> Outputs,
    SkillPermissions Permissions);
```

**SkillInput** carries the parameters and task context for a single invocation:

```csharp
public sealed record SkillInput(
    IReadOnlyDictionary<string, object?> Parameters,
    string? TaskContext,
    string? UserMessage);
```

**SkillResult** uses a discriminated result pattern. Expected failures (rate limits, bad input) return error results instead of throwing exceptions:

```csharp
public sealed record SkillResult
{
    public bool Success { get; init; }
    public object? Data { get; init; }
    public SkillError? Error { get; init; }
    public TokenUsage? TokenUsage { get; init; }
}

public sealed record SkillError(
    SkillErrorKind Kind,
    string Message,
    Exception? Inner = null);

public enum SkillErrorKind
{
    Transient,      // Retry immediately or with short delay
    Permanent,      // Do not retry; input or config is wrong
    RateLimited,    // Retry with exponential backoff
    AuthFailed,     // Re-authenticate, then retry
    Timeout         // Retry with longer timeout
}
```

**SkillExecutionContext** is injected by the runtime. It provides services a skill may need without creating circular dependencies. Skills never reference providers or the orchestrator directly.

```csharp
public sealed record SkillExecutionContext(
    IChatClient? ChatClient,            // For skills needing LLM calls; null if not required
    ILogger Logger,
    IConfiguration Configuration,       // Skill-specific config section
    UserPreferences UserPreferences,    // Relevant prefs for the task domain
    CancellationToken CancellationToken);
```

`IChatClient` (from Microsoft.Extensions.AI) is optional. Only skills declaring `llm-requirement` in their SKILL.md frontmatter receive a non-null instance. This keeps non-LLM skills free of LLM provider coupling.

#### 3.2.5 Skill Categories

| Category | Examples |
|----------|----------|
| **Research** | Web search, document analysis, fact-checking |
| **Content** | Summarization, translation, text-to-speech |
| **Data** | File parsing, CSV analysis, database queries |
| **Integration** | Email sending, calendar access, API calls, messaging channel adapters |
| **Development** | Code generation, code review, testing |
| **System** | Task scheduling, notification delivery, memory management |

#### 3.2.6 Skill Lifecycle

1. **Discover** -- Browse skills in the Hub or define locally
2. **Install** -- Pull skill package from the Hub or register a local skill
3. **Resolve Dependencies** -- Check for required MCP servers; install missing ones (see Section 3.4)
4. **Configure** -- Set skill-specific parameters (API keys, preferences)
5. **Execute** -- Agent invokes the skill with structured input
6. **Update** -- Hub notifies when new versions are available

#### 3.2.7 Skill Discovery and Loading Flow

```
1. Scan skill directories for SKILL.md files
2. Parse YAML frontmatter (metadata only, ~100 tokens per skill)
3. Register skill name, description, type, and dependencies in local catalog
4. On activation:
   a. Load full SKILL.md instructions into agent context
   b. For native/hybrid: load .NET assembly via AssemblyLoadContext
   c. For mcp-bridge/hybrid: ensure required MCP servers are running (see Section 3.4)
   d. Resolve ISkill implementation
5. On deactivation:
   a. Release agent context
   b. Notify MCP dependency manager that skill no longer needs its servers
```

**Plugin architecture:** Skills are loaded via `AssemblyLoadContext` (ALC) with shared contract interfaces from `OpenVEPA.Core`. Each skill directory gets its own ALC. `isCollectible: true` enables hot-swap skill unloading without process restart.

### 3.3 Hub (Distribution Registry)

The Hub is a distribution registry for skills and agents. It is analogous to NuGet.org or the npm registry. Nothing executes on the Hub. All code is downloaded and runs locally on the user's OpenVEPA instance.

> **Repository scope:** The Hub server/backend is a **separate repository** (`OpenVEPA-Hub`). This repository (`OpenVEPA`) contains only the **Hub Client** — the code that communicates with the Hub to browse, search, install, and update packages. The initial assistant implementation works without Hub connectivity (local-only skills and sideloading).

**Hub concepts:**

| Concept | Description |
|---------|-------------|
| **Registry** | Central index of published skills and agents with metadata, versions, ratings, and MCP server dependency tracking |
| **Package** | A skill directory (SKILL.md + optional .NET assembly + MCP server references), packaged for distribution |
| **Publisher** | Any user or organization that publishes a package to the Hub |
| **Consumer** | An OpenVEPA instance that downloads packages from the Hub |
| **Channel** | Release track (stable, beta, experimental) mapped to version prerelease tags |

**Hub responsibilities (what the Hub does — implemented in `OpenVEPA-Hub` repo):**

- Browse and search skills by category, rating, compatibility, and MCP server dependencies
- Download versioned skill packages
- Track which MCP servers are required by which skills (cross-skill dependency graph)
- Host ratings, reviews, and download counts
- Validate package structure and metadata on publish
- Accept package uploads from publishers

**Hub boundaries (what the Hub does NOT do):**

- Execute any skill code
- Run MCP servers
- Store user data or configuration
- Make runtime decisions

**Hub Client (built into this repository — `OpenVEPA`):**

The Hub Client is the only Hub-related code in this repository. It provides:

- **Browse** — Search and filter skills/agents by category, rating, and compatibility
- **Install** — Download and register a package with one command or one click in the GUI
- **Update** — Check for and apply updates to installed packages
- **Verify** — Validate package integrity and check for known security issues

> **Note:** Publishing to the Hub is handled by a separate CLI tool in the `OpenVEPA-Hub` repository, or via the Hub's web interface. This repository does not include publish functionality.

**Local-only mode (works without Hub):**

The assistant works fully without Hub connectivity. Skills and agents can be:
- Sideloaded from local directories: `openvepa skill install --path ./my-skill/`
- Installed from local `.opvpkg` files: `openvepa skill install ./my-skill.opvpkg`
- Manually placed in the skills directory

Hub connectivity adds browse/search/install from the public registry but is not required.

**Installation flow:**

```
1. Download   -- Fetch skill package from Hub (or sideload from disk)
2. Verify     -- Check package integrity (hash, signature if available)
3. Extract    -- Unpack skill directory (SKILL.md, bin/, config.json)
4. Register   -- Add skill to local catalog (parse SKILL.md frontmatter)
5. Resolve    -- Check MCP server dependencies against local MCP registry
   a. Already installed? Reuse existing installation (no reinstall)
   b. Missing? Download and install required MCP server(s)
6. Ready      -- Skill available for activation
```

**Hub architecture:**

- The Hub API is a public service. OpenVEPA instances connect to it over HTTPS.
- Packages are versioned using semantic versioning (e.g., `1.2.3`).
- Each package includes a SKILL.md declaring inputs, outputs, dependencies, and required permissions.
- Packages requiring external API keys or services must declare those in the SKILL.md metadata.
- A local-only mode is supported: users can sideload packages from disk without connecting to the Hub.

**Trust and safety:**

- Published packages go through automated validation (schema checks, dependency resolution)
- Community ratings and reviews surface quality packages
- Flagging system for malicious or broken packages
- **Execution model by origin:**
  - *Built-in skills* (shipped with OpenVEPA): run in-process via `AssemblyLoadContext` (native)
  - *Hub-installed or sideloaded skills*: **must** use MCP-bridge execution (process-isolated). This provides OS-level crash isolation and a permission boundary. Native in-process execution is not permitted for external skills.
  - *Phase 2 — code signing*: Hub skills signed by trusted publishers may opt into native in-process execution after signature verification. Until code signing ships, all external skills use MCP-bridge.
- Signed packages (future): cryptographic signatures to verify publisher identity

### 3.4 MCP Integration (Skill Implementation Detail)

> **Phase 1 scope:** Simple start/stop per skill invocation. No process pooling or reference counting. Phase 2: shared MCP server instances with reference counting.

MCP (Model Context Protocol) is a secondary implementation detail, not a peer concept to skills.Skills are the primary abstraction. MCP servers are dependencies that skills can use internally. The `ISkill` interface wraps everything, whether the implementation is native .NET code, an MCP server, or both.

**Relationship to skills:**

```
OpenVEPA Assistant (Host)
    |
    +-- Skill Runtime (manages all skills via ISkill interface)
        |
        +-- [ISkill] core-memory          (native: .NET assembly)
        +-- [ISkill] task-scheduler        (native: .NET assembly)
        +-- [ISkill] web-search            (hybrid: .NET + brave-search MCP server)
        +-- [ISkill] github-tools          (mcp-bridge: wraps github MCP server)
        +-- [ISkill] database-query        (mcp-bridge: wraps sqlite MCP server)
        |
        +-- MCP Dependency Manager (manages MCP server processes)
            +-- brave-search   (used by: web-search)
            +-- github         (used by: github-tools, code-review)
            +-- sqlite         (used by: database-query)
```

**MCP server dependency management:**

The system maintains a local registry of installed MCP servers. Multiple skills can share the same MCP server installation and running instance.

| Operation | Behavior |
|-----------|----------|
| **Install** | Download MCP server binary/package. Register in local MCP registry with name, version, transport. |
| **Deduplication** | Before installing, check local registry. If the required version is already installed, reuse it. |
| **Start on demand** | When a skill activates and needs an MCP server, start the server process if not already running. |
| **Share across skills** | Multiple active skills can use the same running MCP server instance. Reference counting tracks consumers. |
| **Stop when unused** | When the last skill using an MCP server deactivates, stop the server process. |
| **Uninstall protection** | When a skill is uninstalled, its MCP server dependencies are NOT removed if any other installed skill still references them. Only when the last skill referencing an MCP server is uninstalled is the MCP server itself removed. |
| **Update** | New MCP server versions are resolved during skill update. Restart server if version changes. |

**MCP server lifecycle:**

```
1. Skill activates and declares MCP server dependency (from SKILL.md metadata)
2. MCP Dependency Manager checks local registry:
   a. Not installed? Install from configured source.
   b. Installed but not running? Start the process.
   c. Already running? Increment reference count.
3. MCP client connection established (stdio or HTTP transport)
4. Skill uses MCP server via ISkill implementation
5. Skill deactivates:
   a. Decrement reference count
   b. If count reaches 0, stop the MCP server process
6. Skill uninstalled:
   a. Remove skill from MCP server consumers list
   b. If no other installed skill references this MCP server, uninstall the MCP server
   c. If other skills still reference it, keep the MCP server installed
```

**Local MCP registry:**

| Field | Description |
|-------|-------------|
| `name` | MCP server identifier (e.g., `brave-search`, `github`) |
| `version` | Installed version |
| `transport` | `stdio` (local process) or `http` (remote/shared) |
| `path` | Local installation path |
| `status` | `installed`, `running`, `stopped` |
| `consumers` | List of skill names currently using this server |

**MCP SDK:** Official `ModelContextProtocol` NuGet package (Microsoft co-maintained, v1.0.0 stable). Default transport: stdio for local MCP servers. HTTP transport for remote/shared servers.

> Research details: `docs/research/mcp_dotnet_analysis.md`

### 3.5 Project Structure

The solution is organized into focused assemblies. Each assembly has a single responsibility. All dependency arrows point toward Core. No circular references.

**Solution layout:**

```
src/
    OpenVEPA.Core/                # Interfaces, domain types, SkillExecutionContext
    OpenVEPA.Server/              # ASP.NET Core host, SignalR hubs, middleware
    OpenVEPA.Cli/                 # CLI commands, TUI (references Server for self-hosting)
    OpenVEPA.Skills.Runtime/      # Skill loading, ALC management, MCP bridge
    OpenVEPA.Agents.Runtime/      # Agent loading, lifecycle, activation
    OpenVEPA.Hub.Client/          # Hub registry client (browse, install, update)
    OpenVEPA.Providers/           # IChatClient registrations per LLM provider
    OpenVEPA.Storage/             # EF Core, file storage, vector storage
    OpenVEPA.Scheduler/           # Hangfire integration
    OpenVEPA.Channels.Telegram/   # Telegram adapter
    OpenVEPA.Channels.Discord/    # Discord adapter
    OpenVEPA.Channels.Email/      # Email adapter
tests/
    OpenVEPA.Core.Tests/
    OpenVEPA.Skills.Runtime.Tests/
    ...
```

**Dependency rules:**

| Assembly | References | Notes |
|----------|------------|-------|
| **Core** | *(nothing)* | Defines all interfaces: `ISkill`, `IChatClient` wrappers, `IChannelAdapter`, `IDocumentStore` |
| **Server** | Core, Skills.Runtime, Agents.Runtime, Providers, Storage, Scheduler | Composition root. Wires DI, hosts Kestrel and SignalR hubs |
| **Cli** | Server | Self-hosts Kestrel + hubs for single-machine mode |
| **Skills.Runtime** | Core | Skill loading, `AssemblyLoadContext` isolation, MCP bridge |
| **Agents.Runtime** | Core | Agent loading, lifecycle management, activation |
| **Hub.Client** | Core | Hub registry operations (browse, install, update) |
| **Providers** | Core | `IChatClient` registrations per LLM provider |
| **Storage** | Core | EF Core contexts, file storage, vector storage |
| **Scheduler** | Core | Hangfire job definitions and scheduling |
| **Channels.Telegram** | Core | Telegram Bot API adapter. Communicates via SignalR client |
| **Channels.Discord** | Core | Discord gateway adapter. Communicates via SignalR client |
| **Channels.Email** | Core | IMAP/SMTP adapter. Communicates via SignalR client |

**Key rules:**

- All dependency arrows point toward Core. Core depends on nothing.
- No lateral dependencies between Providers, Storage, and Scheduler. Each references Core only.
- Channel adapters are leaf nodes. Each `Channels.*` references Core only and communicates with the server through a SignalR client connection, not through direct business logic coupling.
- No circular references between any assemblies.

---

## 4. Communication Architecture

### 4.1 WebSocket Core (SignalR on ASP.NET Core)

All real-time communication uses SignalR on ASP.NET Core. SignalR provides hub-based RPC, streaming via `IAsyncEnumerable<T>`, auto-reconnect, groups, user tracking, and transport fallback (WebSocket preferred, SSE, Long Polling).

All clients (CLI, TUI, Web GUI, messaging bots) connect to the same SignalR hubs.

**Authentication:** WebSocket connections are secured with bearer tokens. Tokens are generated and managed via the `openvepa` CLI:

```
openvepa token create --name "telegram-bot" --expires 365d
openvepa token list
openvepa token revoke <token-id>
```

**TUI authentication:** The TUI runs locally on the same machine as the OpenVEPA process. It authenticates via an auto-generated ephemeral loopback token (created on startup, stored in a temp file with user-only permissions). No manual token management is required. All remote clients (Web GUI, messaging adapters, external CLI) require a manually created bearer token.

**Single-server default:** All connections are in-memory. Handles hundreds of concurrent connections. Zero configuration.

**Optional scale-out:** Redis backplane (`Microsoft.AspNetCore.SignalR.StackExchangeRedis`) adds cross-server message synchronization with one line of configuration.

**Connection and Rate Limits:**

| Limit | Default | Configurable |
|-------|---------|-------------|
| Max connections per token | 5 | Yes (`config/appsettings.json`) |
| Max messages per connection per minute | 60 | Yes |
| Max message size | 256 KB | Yes |
| Connection idle timeout | 30 minutes | Yes |

Connections exceeding limits receive a `429 Too Many Requests` error. Repeated violations trigger temporary token suspension (configurable threshold).

> Research details: `docs/research/websocket_communication_analysis.md`

### 4.2 Hub Endpoints

> **Phase 1 scope:** Single hub (AssistantHub). TaskHub and SystemHub added in Phase 2.

Three hubs, separated by concern:

| Hub | Purpose | Message Frequency | Auth Required |
|-----|---------|-------------------|---------------|
| `AssistantHub` | Chat messages, LLM streaming, session management | High (streaming tokens) | Yes (token or local TUI) |
| `TaskHub` | Task lifecycle events, progress updates, task CRUD | Medium (status changes) | Yes (token or local TUI) |
| `SystemHub` | Health checks, config changes, system events, notifications | Low (periodic) | Yes (token or local TUI) |

**Communication patterns supported:**

| Pattern | Hub | Use Case |
|---------|-----|----------|
| Request-Response | AssistantHub | Load conversation history |
| Streaming | AssistantHub | Token-by-token LLM output via `IAsyncEnumerable<T>` |
| Push Notification | TaskHub | Task finished while user was away |
| Broadcast | SystemHub | System health to all connected clients |
| Group Targeted | TaskHub | Progress to task watchers only |

### 4.3 Messaging Channels

Each messaging platform is implemented as a **Channel Adapter**: a standalone service bridging platform-specific APIs to the OpenVEPA WebSocket core.

```
[User on Telegram] --> [Telegram Adapter] --> [SignalR Client] --> [OpenVEPA Core]
[User on Discord]  --> [Discord Adapter]  --> [SignalR Client] --> [OpenVEPA Core]
[User on Email]    --> [Email Adapter]    --> [SignalR Client] --> [OpenVEPA Core]
```

**Common interface:**

All adapters implement `IChannelAdapter` with methods for `StartAsync`, `StopAsync`, `SendMessageAsync`, and an `OnMessageReceived` event. Each adapter is a separate assembly. Adapters are configuration-driven and hot-pluggable (enable/disable via `appsettings.json`).

**Platform support:**

| Platform | .NET Library | Phase | Key Features |
|----------|-------------|-------|--------------|
| **Telegram** | `Telegram.Bot` (v22.9+) | 1 | Free, rich bot API, inline keyboards, webhooks, 10yr maturity |
| **Discord** | `Discord.Net` | 1 | Free, slash commands, embeds, components, 9yr maturity |
| **Email** | `MailKit` (v4.15+) | 1 | IMAP/SMTP, IDLE push, OAuth2, .NET Foundation, 40yr protocol |
| **Slack** | `SlackNet` | 2 | Block Kit, modals, Socket Mode, free tier limited |
| **Matrix** | `Matrix.Sdk` | 2 | Self-hosted, federated, privacy-first, .NET SDK immature |
| **WhatsApp** | `HttpClient` (REST) | 3+ | Paid per-message, business verification, 24h window |
| **Signal** | `HttpClient` (via signal-cli) | 3+ | No official bot API, fragile bridge, Java dependency |

> Research details: `docs/research/messaging_channels_analysis.md`

### 4.4 Message Protocol

**Default protocol:** JSON via `System.Text.Json` (built-in, fastest, AOT-compatible).

**Optional protocol:** MessagePack via `AddMessagePackProtocol()` (40-50% smaller payloads, 2-3x faster serialization, binary/not human-readable).

**Message envelope:**

All messages use a consistent envelope with the following fields:

| Field | Type | Purpose |
|-------|------|---------|
| `Id` | string (ULID) | Unique message identifier |
| `Type` | string | Message type: `chat`, `command`, `event`, `error`, `stream_chunk` |
| `CorrelationId` | string | Links request to response across async boundaries |
| `Timestamp` | DateTimeOffset | UTC timestamp |
| `Version` | int | Schema version for forward compatibility |
| `Payload` | T (generic) | Type-safe message content |

### 4.5 Session Management

The assistant supports **multiple concurrent sessions** from different clients and channels. Each session is an independent conversation with its own context, history, and state.

**Core concepts:**

| Concept | Description |
|---------|-------------|
| **Session** | An independent conversation thread with the assistant. Has its own message history, context, and state. |
| **Session ID** | Unique identifier (ULID) for each session. Returned on creation, used for all subsequent interactions. |
| **Session Origin** | The client/channel that created the session (e.g., `telegram:12345`, `tui:local`, `web:token-xyz`) |
| **Session State** | `active` (in progress), `paused` (can be resumed), `archived` (read-only history) |

**Multi-session behavior:**

- A user can have multiple sessions running in parallel (e.g., a research session from Telegram and a coding session from TUI)
- Sessions are independent — each has its own conversation history and agent context
- Sessions persist across restarts (stored in the database)
- A session started from one client can be continued from another (e.g., start on Telegram, continue on TUI)
- Each client connection specifies which session to join or requests a new one

**Session operations (exposed via `AssistantHub`):**

| Operation | Description |
|-----------|-------------|
| `CreateSession` | Start a new session. Returns session ID. |
| `ResumeSession(sessionId)` | Reconnect to an existing session. Loads history and context. |
| `ListSessions` | List all sessions with status, origin, last activity, and summary. |
| `PauseSession(sessionId)` | Mark session as paused. Agent state is preserved. |
| `ArchiveSession(sessionId)` | Mark session as read-only. History preserved, no new messages. |
| `DeleteSession(sessionId)` | Permanently remove session and its history. |
| `GetSessionHistory(sessionId)` | Retrieve message history for a session. Supports pagination. |

**Session routing for channel adapters:**

Each messaging channel adapter maps platform-level conversations to OpenVEPA sessions:
- **Telegram**: One session per chat ID (1:1 mapping). New chat = new session. Same chat = resume session.
- **Discord**: One session per thread/channel. Direct message = personal session.
- **Email**: One session per email thread (by subject/references header).
- **TUI**: User selects or creates sessions via the interface. Default: resume last active session.
- **Web GUI**: Session selector in the UI. Can view and switch between sessions.

**Session storage:**

Sessions are stored in the primary database (SQLite by default, PostgreSQL optional):
- Session metadata: ID, origin, state, created_at, updated_at, summary
- Message history: ordered messages with role, content, timestamp, token counts
- Agent context: active skills, autonomy level, accumulated memory for the session

---

## 5. Agent Framework

### 5.1 Agent Components

Each agent has the following configurable components:

| Component | Description | Scope |
|-----------|-------------|-------|
| **Objective** | What must be achieved | Per-task |
| **System Prompt** | What/who is the agent | Per-agent type |
| **Guidance** | Boundaries and decision-making rules | Per-agent type |
| **Hard Restrictions** | What is NOT allowed in any situation | Global + Per-agent |
| **Environment Context** | User identity, goals, aspirations, and learned preferences (see §5.9) | Global (shared) |
| **Memory/State** | Track progress, history, and learnings | Per-agent instance |

### 5.2 Autonomy Levels

The system supports configurable autonomy levels per agent or globally:

| Level | Name | Behavior |
|-------|------|----------|
| 1 | **Supervised** | Agent proposes actions, waits for user approval before execution |
| 2 | **Semi-Autonomous** | Agent executes low-risk actions, asks approval for high-impact decisions |
| 3 | **Autonomous with Reporting** | Agent executes fully, provides detailed reports after completion |
| 4 | **Fully Autonomous** | Agent thinks, plans, and executes independently ("works while you sleep") |

- Default level is configurable per agent type
- User can override autonomy level per task
- High-risk actions (financial, file deletion, external communications) require explicit configuration to allow at Level 4

**Action Risk Classification:**

| Risk Level | Actions | Autonomy Threshold |
|------------|---------|-------------------|
| **Low** | Read-only operations, web search, summarization, content generation, listing/browsing | Level 1+ (all levels) |
| **Medium** | Sending messages, creating files, making API calls (read/write), modifying preferences | Level 2+ (semi-autonomous and above) |
| **High** | File deletion, financial transactions, external communications with side effects, system config changes | Level 3+ (autonomous with reporting) |
| **Critical** | Bulk operations, credential management, data export, irreversible actions | Level 4 only (fully autonomous) + explicit per-action allowlist in config |

Agents operating below the threshold for an action must request user approval before proceeding.

### 5.3 Task Execution Settings

These settings control how the assistant approaches and executes user tasks. Configurable globally, per-agent, and overridable per-task.

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `clarifyBeforeStart` | `bool` | `true` | When `true`, the assistant asks clarifying questions before starting work. When `false`, it proceeds directly with best interpretation. |
| `continueWithoutAsking` | `bool` | `false` | When `true`, the assistant completes the entire task without pausing for user input ("just continue"). Combined with autonomy level 3-4. |
| `taskTimeLimit` | `TimeSpan?` | `null` (no limit) | Maximum wall-clock time for a task. Agent pauses and reports status when limit is reached. `null` = unlimited. |
| `llmBudget` | `object?` | `null` (no limit) | Token/cost budget for a task. See LLM Budget below. |
| `maxRetries` | `int` | `3` | Maximum retry attempts for failed sub-steps before escalation. |

**LLM Budget settings** (nested under `llmBudget`):

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `maxTokensPerTask` | `int?` | `null` | Maximum total tokens (input + output) for a single task. |
| `maxCostPerTask` | `decimal?` | `null` | Maximum cost in USD for a single task (computed from provider pricing). |
| `maxTokensPerDay` | `int?` | `null` | Daily token cap across all tasks. |
| `maxCostPerDay` | `decimal?` | `null` | Daily cost cap in USD. |
| `warnAtPercent` | `int` | `80` | Notify user when this percentage of budget is consumed. |
| `action` | `string` | `"pause"` | What to do when budget is hit: `"pause"` (wait for user), `"stop"` (abort task), `"notify"` (warn and continue). |

When a budget limit is reached, the assistant checkpoints its progress and either pauses (awaiting user decision) or stops. Progress is never lost.

### 5.4 Long-Running Tasks

> **Phase 1 scope:** Tasks complete or fail with Hangfire retry. Full checkpoint/resume in Phase 2.

Tasks can run for hours or days(e.g., multi-step research, large code generation, data processing). The system ensures they are **resumable** and **durable**.

| Feature | Description |
|---------|-------------|
| **Checkpointing** | Tasks periodically save state to the database (plan, completed steps, partial results, context). |
| **Resumability** | If the process restarts (crash, update, user restart), tasks resume from the last checkpoint. No work is lost. |
| **Progress Tracking** | Real-time status via SignalR. Percentage, current step, estimated remaining time. |
| **Durable Queues** | Task queue persisted via Hangfire (SQLite-backed). Survives process restarts. |
| **Pause / Resume** | User can explicitly pause and resume any task via CLI or UI. |
| **Cancel** | User can cancel any task. Partial results are preserved and accessible. |

**Checkpoint data saved per task:**
- Task ID, goal/prompt, and full plan
- Completed steps with outputs
- Remaining steps
- Accumulated context (trimmed to minimize tokens on resume)
- Token/cost usage so far
- Timestamp and duration

### 5.5 Scheduled Tasks (Cron Jobs)

Scheduled tasks are a core feature. Users configure recurring jobs via CLI, TUI, or API. Powered by Hangfire (SQLite-backed by default).

| Feature | Description |
|---------|-------------|
| **Cron expressions** | Standard cron syntax for schedule definition (e.g., `0 6 * * *` for daily at 6 AM) |
| **Named schedules** | User-friendly names: `openvepa schedule add "morning-briefing" --cron "0 6 * * *" --prompt "..."` |
| **Task prompt** | Each scheduled task has a prompt/goal that the assistant executes at the scheduled time |
| **Agent binding** | Optionally bind a scheduled task to a specific agent (default: main Assistant) |
| **History** | Execution history with results, duration, and token usage per run |
| **Missed runs** | Configurable policy: skip missed, run once on resume, or backfill all missed |
| **Enable/Disable** | Toggle schedules without deleting them |
| **CLI management** | `openvepa schedule list/add/remove/enable/disable/history` |

**Built-in schedule templates:**
- Morning briefing (daily summary of news, calendar, tasks)
- Inbox digest (periodic email/message summary)
- Data backup (scheduled database + config export)
- Custom user-defined tasks (any prompt on any schedule)

### 5.6 Concurrent Task Execution

- Multiple tasks run in parallel via `System.Threading.Channels` and `Task.WhenAll`/`Task.WhenAny`
- The Assistant coordinates and monitors all concurrent tasks
- Resource management: configurable limits on concurrent tasks via `SemaphoreSlim`
- Task prioritization: higher priority tasks get resources first
- Dependency handling: tasks can wait for other tasks to complete
- Progress tracking: real-time visibility into all running tasks via SignalR push

**Task Queue Architecture:**

Hangfire owns task **persistence**. Channels own task **execution**. Every user-initiated task enters Hangfire first (durability guarantee), then Hangfire dispatches execution to a `Channel<T>`-based worker pool. This gives every task crash recovery (Hangfire) and every execution path concurrency control (Channels with `SemaphoreSlim`).

| Layer | Technology | Responsibility |
|-------|-----------|----------------|
| Persistence | Hangfire (SQLite) | Durable storage, retry policy, scheduling, crash recovery |
| Execution | System.Threading.Channels | In-memory dispatch, concurrency limits, prioritization |

### 5.7 Multi-Agent Communication (Phase 3)

When multiple agents are active:

- Agents communicate via structured messages routed through SignalR hubs
- The Assistant manages task routing and agent coordination
- Agents can request help from other agents through the Assistant
- Agents can spawn sub-tasks that create new agent instances
- Agent packages are Hub-distributed (install on demand)
- A2A (Agent-to-Agent) protocol may be adopted if a suitable standard matures

### 5.8 Failure Handling

| Strategy | Behavior |
|----------|----------|
| **Retry** | Exponential backoff with configurable max attempts (Polly integration) |
| **Escalation** | Repeated failures escalate to the Assistant for re-routing |
| **Fallback** | Alternative LLM provider if primary fails (via `FallbackChatClient` middleware) |
| **Notification** | User notification for critical failures (based on autonomy level, via SignalR push) |

### 5.9 User Profile and Learned Preferences

> **Phase 1 scope:** Explicit preferences only (`openvepa profile set`). Implicit learning, confidence scoring, and confirmation UX deferred to Phase 2.

The assistant builds a persistent profileof the user over time. Preferences are **learned from interactions** (implicit) or **stated by the user** (explicit). This profile enables the assistant to make better autonomous decisions, reduce clarifying questions, and personalize outputs.

#### Preference Categories

| Category | Examples | How Learned |
|----------|----------|-------------|
| **Personal** | Name, timezone, language, location | Stated or inferred from device/channel |
| **Communication** | Preferred response length, formality, level of detail | Inferred from feedback ("too verbose", "more detail") |
| **Aesthetic** | Preferred colors, fonts, themes, visual style | Stated ("I like dark mode", "use blue") |
| **Content** | News topics, industry focus, reading level | Inferred from requests and engagement |
| **Shopping / Products** | Preferred brands, sizes, price range, quality vs. value | Stated or inferred from purchase-related tasks |
| **Food / Lifestyle** | Dietary restrictions, cuisine preferences, taste preferences | Stated or inferred |
| **Risk Tolerance** | Conservative vs. aggressive (financial, decisions, recommendations) | Stated or inferred from choices over time |
| **Work Style** | Preferred tools, frameworks, coding style, documentation level | Inferred from task patterns |
| **Scheduling** | Preferred meeting times, morning/evening person, busy periods | Inferred from calendar and task patterns |
| **Domain Knowledge** | Expertise areas, topics that need more/less explanation | Inferred from question complexity and corrections |

#### How Preferences Are Managed

| Feature | Description |
|---------|-------------|
| **Implicit learning** | The assistant observes patterns across sessions and infers preferences. Example: user always asks for concise answers → store `communication.responseLength = "concise"`. |
| **Explicit capture** | User states a preference directly: "I'm vegetarian", "I prefer dark mode", "my risk tolerance is low". Stored immediately with high confidence. |
| **Confirmation** | Before acting on a low-confidence inferred preference for the first time, the assistant confirms: "I noticed you tend to prefer concise answers — should I default to that?" |
| **Confidence scoring** | Each preference has a confidence level (0.0–1.0). Explicit statements = 1.0. Inferred from single observation = 0.3. Reinforced over multiple sessions = higher. |
| **Override per-task** | User can override any preference in a specific task: "This time, give me a detailed answer." Does not change the stored preference. |
| **Review and edit** | User can view, edit, and delete learned preferences via CLI (`openvepa profile`) or UI. Full transparency — no hidden profiling. |
| **Export / Import** | Profile exportable as JSON for backup, migration, or sharing across instances. |
| **Override precedence** | Explicit preferences always override inferred ones. An inferred preference is never written to the database if an explicit preference already exists for the same key. When a user explicitly sets a value, any existing inferred entry for that key is replaced and confidence is set to 1.0. |

#### Storage Model

Preferences are stored in the SQLite database (`data/openvepa.db`) as structured key-value entries with metadata:

```
Table: user_preferences
─────────────────────────────────────────────────────
| id          | category.key path (e.g., "food.dietary_restrictions")  |
| value       | JSON-encoded value (string, array, object)             |
| confidence  | 0.0–1.0 (how certain the assistant is)                 |
| source      | "explicit" | "inferred" | "confirmed"                 |
| source_ref  | Session/message ID where preference was learned        |
| created_at  | When first learned                                     |
| updated_at  | When last reinforced or modified                       |
| expires_at  | Optional TTL for time-sensitive preferences             |
─────────────────────────────────────────────────────
```

#### Token Efficiency Integration

The user profile is a key tool for **token minimization** (see §6.4):

- **Reduce clarifying questions** — If the assistant knows the user's risk tolerance is "conservative", it doesn't need to ask before recommending a safe option.
- **Compact context injection** — Only relevant preferences are injected into the LLM context per task. A shopping task gets shopping preferences; a coding task gets work style preferences. Not the entire profile.
- **Progressive loading** — At task start, load only the preference categories relevant to the task domain (~50–200 tokens). Full profile is never loaded into context.

#### CLI Commands

```bash
openvepa profile show                          # Show all learned preferences
openvepa profile show --category food           # Show preferences in a category
openvepa profile set food.dietary "vegetarian"  # Explicitly set a preference
openvepa profile delete food.dietary            # Delete a preference
openvepa profile export > profile.json          # Export profile as JSON
openvepa profile import < profile.json          # Import profile from JSON
openvepa profile reset                          # Clear all learned preferences
```

---

## 6. LLM Configuration

### 6.1 Abstraction Layer (Microsoft.Extensions.AI / IChatClient)

OpenVEPA builds its LLM layer on `IChatClient` from `Microsoft.Extensions.AI`. This is Microsoft's official standardization direction for .NET AI workloads.

**Layer 1 -- Abstraction:** `IChatClient` and `IEmbeddingGenerator<T>` from `Microsoft.Extensions.AI.Abstractions`. All provider code programs against these interfaces.

**Layer 2 -- Providers:** Register provider SDKs that implement `IChatClient` via DI. Swap providers by changing configuration, not code.

**Layer 3 -- Orchestration (Phase 3):** Microsoft Agent Framework (MAF) layers on top for agent orchestration, workflows, multi-agent coordination, tool calling, MCP support, and A2A protocol. MAF builds on `IChatClient` implementations from M.E.AI, so it is additive, not foundational. Replaces Semantic Kernel orchestration (now in maintenance mode).

> **Clarification — Semantic Kernel vs. MAF scope:** MAF replaces SK *orchestration* (pipelines, planners, agents). SK *infrastructure connectors* (e.g., vector store implementations of `Microsoft.Extensions.VectorData`) are still used where they provide the best first-party .NET implementation.

> Research details: `docs/research/llm_integration_analysis.md`

### 6.2 Supported Providers

> **NOTE:** Exact model selection is a configuration-time decision, not hardcoded. The table lists model families, not specific version numbers. Users configure their preferred model via `appsettings.json` or the GUI.

| Provider | Model Families | .NET Package | Priority | Notes |
|----------|---------------|-------------|----------|-------|
| **OpenAI** | GPT-4o, GPT-4, o1/o3 reasoning | `OpenAI` + `Microsoft.Extensions.AI.OpenAI` | P0 | Most mature .NET SDK. Azure OpenAI via `Azure.AI.OpenAI`. |
| **Ollama (Local)** | Llama, Mistral, Phi, Qwen, Gemma, DeepSeek | `OllamaSharp` | P0 | Local/offline. Zero API cost. Privacy-sensitive data. |
| **Anthropic** | Claude Opus, Sonnet, Haiku | `Anthropic` (official) | P1 | Strong reasoning, long context. No embeddings API. |
| **Google Gemini** | Gemini Pro, Flash, Ultra | `Google.GenAI` | P1 | Multimodal. AI Studio or Vertex AI deployment. |
| **GitHub Models** | Multi-provider (OpenAI, Phi, Llama, Mistral) | `Azure.AI.Inference` | P2 | Free tier for prototyping. Same models on Azure for production. |

### 6.3 Provider Requirements

| Requirement | Status | Implementation |
|-------------|--------|----------------|
| **Streaming** | All providers support `IAsyncEnumerable<T>` | Built into `IChatClient.GetStreamingResponseAsync()` |
| **Tool/Function Calling** | All cloud providers; Ollama model-dependent | Via `IChatClient` tool definitions + MAF tool support |
| **Vision/Multimodal** | OpenAI, Anthropic, Gemini native; Ollama model-dependent | Image input via `IChatClient` message content |
| **Embeddings** | OpenAI, Gemini, Ollama | `IEmbeddingGenerator<T>`. Anthropic has no embeddings API. |
| **Fallback** | Achievable via middleware | `FallbackChatClient` wrapping multiple `IChatClient` instances |
| **Cost Tracking** | Token usage returned in responses | Middleware/decorator on `IChatClient` intercepts and logs usage |
| **Privacy Routing** | Configuration-driven | Data classification rules determine cloud vs. local routing |

### 6.4 Token Minimization Strategy

> **Phase 1 scope:** Progressive loading and LLM caching. Advanced strategies (model routing, conversation summarization) evolve in Phase 2.

Token efficiency is a first-class design goalacross every layer. Tokens are the primary cost driver for paid LLM providers. OpenVEPA minimizes token usage through:

| Strategy | Layer | Description |
|----------|-------|-------------|
| **Progressive context loading** | Agent/Skill | Load agent/skill definitions in tiers: metadata (~100 tokens) first, full body only when activated. Never load all skills into context. |
| **Context window packing** | Agent | Include only relevant context for each LLM call. Trim conversation history, summarize old turns. |
| **LLM response caching** | Infrastructure | Cache identical or similar queries via HybridCache. Configurable TTL per query type. |
| **Prompt templates** | Agent | Pre-optimized system prompts. Avoid verbose instructions. Test and measure token usage per prompt. |
| **Selective tool results** | Skill | Skills return structured, concise results. Never dump raw data into context — summarize first. |
| **Conversation summarization** | Session | Summarize long conversations when context exceeds 75% of the model window. Preserves recent turns verbatim, compresses older turns into a ~500-token summary, and extracts key metadata. See Context Window Management below. |
| **Embedding-based retrieval** | RAG | Retrieve only the most relevant chunks instead of feeding entire documents to the LLM. |
| **Budget tracking** | Task | Per-task and per-day token/cost budgets with configurable limits (see §5.3 Task Execution Settings). |
| **Model routing** | Provider | Use cheaper/smaller models for simple tasks (classification, extraction) and expensive models only for complex reasoning. |
| **User profile injection** | Agent | Inject only task-relevant user preferences (~50–200 tokens) instead of asking clarifying questions. See §5.9. |

**Context Window Management:**
When a conversation exceeds **75% of the active model's context window**, the system triggers context compression:

1. **Recent turns preserved**: The last N turns (configurable, default 10) are kept verbatim in the context.
2. **Older turns summarized**: A dedicated LLM call summarizes all turns before the preserved window into a concise summary (targeting ~500 tokens).
3. **Summary injection**: The summary replaces older turns in the next LLM call: `[System Prompt] [Summary of earlier conversation] [Recent 10 turns] [New user message]`.
4. **User indicator**: When summarization occurs, the system logs it and optionally notifies the user: "Context was compressed to fit the model window."
5. **Metadata preserved**: Key facts extracted during summarization (user preferences mentioned, decisions made, action items) are tagged and stored separately for retrieval.

> **Implementation note:** Token minimization strategies will be extended further as patterns emerge during development. This is an evolving concern, not a one-time design.

### 6.5 LLM Interaction Audit Log

Every LLM call is logged to the database (`data/openvepa.db` → `llm_audit_log` table):

| Field | Type | Description |
|-------|------|-------------|
| `id` | ULID | Unique log entry ID |
| `timestamp` | DateTime | When the call was made |
| `provider` | string | Provider name (openai, ollama, anthropic) |
| `model` | string | Model identifier used |
| `input_tokens` | int | Input/prompt token count |
| `output_tokens` | int | Output/completion token count |
| `estimated_cost_usd` | decimal? | Estimated cost (null for local models) |
| `latency_ms` | int | Round-trip latency |
| `session_id` | string? | Associated session |
| `task_id` | string? | Associated task |
| `skill_name` | string? | Skill that triggered the call |
| `success` | bool | Whether the call succeeded |
| `error_message` | string? | Error details if failed |

Retention: 30 days of detailed logs (configurable). Monthly aggregates retained indefinitely.

---

## 7. User Interfaces

### 7.1 CLI / TUI (Spectre.Console -- Primary Interface)

The CLI and TUI are the primary user interfaces, built with Spectre.Console.Cli.

| Feature | Implementation |
|---------|---------------|
| **Command routing** | Spectre.Console.Cli with type-safe, attribute-driven command definitions |
| **Rich terminal output** | Tables, colors, progress bars, selection prompts, tree rendering |
| **LLM streaming** | Token-by-token display via SignalR .NET client streaming |
| **Tab completion** | System.CommandLine integration as complement (if needed) |

### 7.2 Web GUI (Phase 2)

- Browser-based interface connected via `@microsoft/signalr` JavaScript client
- Same hub API as CLI/TUI (client-agnostic hubs)
- Responsive design for desktop and tablet
- Mobile access: read-only dashboard initially, full control in future
- GUI framework selection is an open question (see Section 11)

### 7.3 Messaging Apps (via Channel Adapters)

See Section 4.3. Users interact with the assistant from Telegram, Discord, Email, Slack, Matrix, or other supported platforms. Each channel adapter bridges platform APIs to the SignalR core.

### 7.4 Core UI Features

These features are available across all interfaces (CLI/TUI, Web, messaging) at varying fidelity:

| Feature | Description |
|---------|-------------|
| **Dashboard** | Overview of active tasks, agent status, recent results |
| **Task Management** | Create, view, prioritize, and manage tasks/ideas |
| **Agent Configuration** | Configure agents, autonomy levels, LLM assignments |
| **Skill Manager** | Browse installed skills, install from Hub, configure parameters |
| **Results Viewer** | View analysis results, reports, and agent outputs |
| **History/Logs** | Full audit trail of agent actions and decisions |
| **Notifications** | Alerts for completed tasks, failures, approval requests (via SignalR push) |

### 7.5 Briefing Center

| Feature | Description |
|---------|-------------|
| **Briefing History** | Archive of all generated briefings with date/time |
| **Audio Playback** | Play briefings directly in GUI (text-to-speech generated) |
| **Text View** | Read briefing content alongside audio |
| **Email Delivery** | Option to send briefing via email with playback link (via Email adapter) |
| **Scheduling** | Configure when briefings are generated (e.g., 6 AM daily, via Hangfire) |

### 7.6 First Run Experience

When a user runs `openvepa start` for the first time, the system must detect the absence of a configuration directory and launch a guided setup wizard before entering normal operation.

**First launch detection**: Check whether `~/.openvepa/` exists. If the directory is missing, trigger the first-run setup flow instead of normal startup.

**Setup wizard** (TUI via Spectre.Console):

| Step | Action | Details |
|------|--------|---------|
| 1. Welcome | Display welcome message | Brief product description and overview of setup steps |
| 2. LLM Provider | Configure language model | Auto-detect local Ollama at `localhost:11434`. Prompt for OpenAI API key (optional). Allow skip to run in limited mode without an LLM. |
| 3. Config Directory | Create default directory structure | Generate `~/.openvepa/` with subdirectories for config, skills, and data |
| 4. Auth Token | Generate loopback token | Create initial authentication token for local TUI access |
| 5. Confirmation | Summarize choices | Display selected provider, config path, and token location |

**Default built-in skills**: The assistant ships with a set of built-in skills that work without the Skill Hub:

- Basic conversation (greetings, small talk, help text)
- Simple web search (available only when an API key is configured)

**Graceful degradation** when no LLM provider is configured:

- The assistant explains which features require an LLM and guides the user to `openvepa config provider add`
- Built-in help and documentation remain accessible
- Skill listing and management commands continue to work

**Quick start after setup**: Once the wizard completes, the assistant enters chat mode and greets the user with a summary of available capabilities.

---

## 8. Infrastructure

### 8.1 Deployment

**Primary: .NET Global Tool**

```bash
dotnet tool install -g openvepa
openvepa start
```

Requires .NET 10 runtime. Cross-platform (Windows, Linux, macOS). Update: `dotnet tool update -g openvepa`. Uninstall: `dotnet tool uninstall -g openvepa`.

**Alternative: Self-contained single-file binary**

```bash
# Download platform-specific binary from GitHub Releases
./openvepa start
```

No .NET runtime dependency. Publish targets: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`. Trimmed binary size: ~30-80 MB.

**Optional: Docker**

```bash
docker run -v ~/.openvepa:/data openvepa/openvepa:latest
```

Useful for server deployments or users who prefer containerized tools.

**Process Modes:**

| Command | Behavior |
|---------|----------|
| `openvepa start` | Foreground process with TUI attached. Ctrl+C to stop. |
| `openvepa start --daemon` | Background service (detached). Logs to file. |
| `openvepa stop` | Sends shutdown signal to running daemon. |
| `openvepa status` | Shows whether the service is running, uptime, active tasks. |

### 8.2 Default Stack (Zero External Services)

All components below ship with OpenVEPA. No extra installation or external services needed.

| Category | Technology | .NET Package | Purpose |
|----------|-----------|-------------|---------|
| **Primary Database** | SQLite (WAL mode) via EF Core | `Microsoft.EntityFrameworkCore.Sqlite` | Sessions, messages, tasks, audit logs, runtime user preferences |
| **Document Store** | File-based JSON | `System.Text.Json` (BCL) | Agent outputs, flexible configs, conversations |
| **Cache** | HybridCache (L1 memory + L2 in-memory) | `Microsoft.Extensions.Caching.Hybrid` | Agent state, LLM response cache, session data |
| **Vector Database** | SQLite + sqlite-vec | `Microsoft.Extensions.VectorData` + SK connector (`Microsoft.SemanticKernel.Connectors.Sqlite`) | Semantic search, RAG, conversation memory |
| **Message Queue** | System.Threading.Channels | BCL (no package) | Agent task dispatch, producer/consumer |
| **Scheduler** | Hangfire (SQLite storage) | `Hangfire.Core` + `Hangfire.Storage.SQLite` | Scheduled tasks, recurring jobs, briefings |
| **Web Server** | Kestrel (ASP.NET Core) | BCL (no package) | HTTP, WebSocket, SignalR hosting |

**Total external services to run:** 0

**SQLite Concurrency and WAL Management:**
- Write serialization: All database writes go through a single `DbContext` writer backed by a `Channel<T>` write queue. This avoids `SQLITE_BUSY` errors from concurrent writers. Reads are unrestricted (WAL mode supports unlimited concurrent readers).
- `busy_timeout`: Set to 5000ms as fallback safety net.
- WAL checkpointing: Periodic `PRAGMA wal_checkpoint(TRUNCATE)` every 5 minutes via background service. Prevents unbounded WAL file growth.
- Connection string: `Data Source=openvepa.db;Mode=ReadWriteCreate;Cache=Shared;Journal Mode=WAL`

> Research details: `docs/research/dotnet_infrastructure_analysis.md`

> Hangfire Dashboard is disabled by default. Enable via configuration. When enabled, accessible from localhost only. Authentication required (reuses token-based auth).

### 8.3 Optional Extensions

Users enable these when they need more scale, durability, or specialized capabilities. Each extension is opt-in through configuration. All swap via DI registration (no business logic changes).

| Category | Technology | When to Use | .NET Package |
|----------|-----------|-------------|-------------|
| **Relational DB** | PostgreSQL | Multi-user, >5 GB data, cross-host access | `Npgsql.EntityFrameworkCore.PostgreSQL` |
| **Document Store** | LiteDB | >1,000 docs/collection, need LINQ queries | `LiteDB` |
| **Document Store** | MongoDB | >50K docs, multi-process writes, aggregation | `MongoDB.Driver` |
| **Cache** | Redis | Multi-process, pub/sub, distributed coordination | `Microsoft.Extensions.Caching.StackExchangeRedis` |
| **Vector Database** | Qdrant | >1M vectors, HNSW index, multi-tenant | `Microsoft.Extensions.VectorData` + SK connector (`Microsoft.SemanticKernel.Connectors.Qdrant`) |
| **Message Queue** | MassTransit + RabbitMQ | Multi-process agents, delivery guarantees, sagas | `MassTransit.RabbitMQ` |
| **Scheduler** | Quartz.NET | Calendar scheduling, distributed workers | `Quartz` |
| **Local LLM Runtime** | Ollama | Privacy-sensitive data, offline use | `OllamaSharp` |
| **TTS Engine** | TBD | Audio briefing generation | TBD |

### 8.4 Vector DB Requirements

- Code against `Microsoft.Extensions.VectorData` abstraction (first-party Microsoft)
- Support multiple embedding models (embedding provider can differ from chat provider)
- Namespace/collection separation per data type
- Efficient similarity search with metadata filtering
- Default (SQLite + sqlite-vec) handles up to ~1M 128-dim vectors with brute-force KNN
- Production upgrade to Qdrant for HNSW index and sub-millisecond queries at scale

### 8.5 Configuration and Data Storage

OpenVEPA separates **installation/system configuration** (JSON config files) from **runtime data** (database). Config files are human-readable, version-controllable, and editable with any text editor. Runtime data is managed by the application.

#### Data Directory Resolution

The data directory (`OPENVEPA_HOME`) is resolved in order:
1. `OPENVEPA_HOME` environment variable (if set)
2. `~/.openvepa/` on Linux and macOS
3. `%APPDATA%\openvepa\` on Windows

On first run, the directory is auto-created with user-only permissions (`700` on Unix, ACL on Windows). If the path is not writable, startup fails with a clear error message.

#### Data Directory Layout

```
~/.openvepa/                              # OpenVEPA data directory (OPENVEPA_HOME)
├── config/                               # ── Installation Configuration (JSON) ──
│   ├── appsettings.json                  # Main application settings (ports, logging, task defaults, budgets)
│   ├── providers.json                    # LLM provider configurations (models, endpoints, routing, pricing)
│   ├── channels.json                     # Messaging channel adapter configs (Telegram, Discord, etc.)
│   └── schedules.json                    # Scheduled tasks / cron jobs
│
├── agents/                               # ── Installed Agents ──
│   ├── assistant/                        # Built-in main agent
│   │   ├── assistant.agent.md            # Agent definition
│   │   └── config.json                   # Agent-specific settings (autonomy, default LLM, enabled skills)
│   └── research-agent/                   # Hub-installed or sideloaded agent
│       ├── research-agent.agent.md
│       ├── config.json
│       └── references/
│
├── skills/                               # ── Installed Skills ──
│   ├── web-search/                       # Example skill
│   │   ├── SKILL.md                      # Skill definition
│   │   ├── config.json                   # Skill-specific settings (API keys, preferences, parameters)
│   │   └── bin/                          # .NET assemblies (if native skill)
│   └── summarize-article/
│       ├── SKILL.md
│       └── config.json
│
├── mcp-servers/                          # ── MCP Server Installations ──
│   ├── registry.json                     # MCP server registry (installed servers, versions, consumers)
│   └── brave-search/                     # Example MCP server installation
│       └── ...                           # Server binary/package files
│
├── data/                                 # ── Runtime Data (Database) ──
│   ├── openvepa.db                       # SQLite database (sessions, messages, tasks, audit, preferences, access tokens)
│   └── documents/                        # File-based document storage (agent outputs, uploads)
│
└── logs/                                 # ── Log Files ──
    └── openvepa-YYYYMMDD.log
```

#### What Goes Where

| Data Type | Storage | Format | Reason |
|-----------|---------|--------|--------|
| **App settings** (ports, logging, feature flags, task defaults) | `config/appsettings.json` | JSON | Editable, overridable via env vars, version-controllable |
| **Task execution defaults** (clarify, continue, time limits, budgets) | `config/appsettings.json` → `TaskDefaults` | JSON | Global defaults for §5.3 settings |
| **LLM provider config** (endpoints, model selection, routing, pricing) | `config/providers.json` | JSON | Rarely changes at runtime, reviewed before deployment |
| **LLM budget limits** (token/cost caps per task/day) | `config/providers.json` → `Budget` | JSON | Cost control for paid providers |
| **Channel adapter config** (Telegram token, Discord bot ID, allowed users) | `config/channels.json` | JSON | Per-adapter, sensitive values via user-secrets/env vars |
| **Scheduled tasks** (cron jobs, recurring prompts) | `config/schedules.json` | JSON | User-defined cron jobs, editable |
| **Agent definitions** (system prompt, skills, autonomy) | `agents/<name>/<name>.agent.md` | Markdown | Human-readable, version-controllable, standard format |
| **Agent settings** (override autonomy, default LLM, enabled skills) | `agents/<name>/config.json` | JSON | Per-agent tuning, editable |
| **Skill definitions** (what the skill does, instructions) | `skills/<name>/SKILL.md` | Markdown | Standard format, human-readable |
| **Skill settings** (API keys, preferences, parameters) | `skills/<name>/config.json` | JSON | Per-skill configuration, editable |
| **MCP server registry** (installed servers, versions, ref counts) | `mcp-servers/registry.json` | JSON | Tracks installations, shared across skills |
| **Access tokens** | `data/openvepa.db` → `access_tokens` table | SQLite (bcrypt-hashed) | Bcrypt with per-token random salt. Token displayed once on creation, no recovery (revoke and recreate). Managed via CLI. |
| **Sessions and messages** | `data/openvepa.db` | SQLite | High-frequency writes, queryable, relational |
| **Task state and checkpoints** | `data/openvepa.db` | SQLite | Long-running task resumability, checkpoint data |
| **Tasks and task queue** | `data/openvepa.db` | SQLite | Runtime state, priority ordering, status tracking |
| **Token/cost usage logs** | `data/openvepa.db` | SQLite | Per-task and per-day token accounting |
| **Audit logs** | `data/openvepa.db` | SQLite | Append-only, queryable history |
| **User preferences** (runtime) | `data/openvepa.db` | SQLite | Changed via UI at runtime, per-session overrides |
| **User profile** (learned preferences) | `data/openvepa.db` → `user_preferences` | SQLite | Implicit/explicit preferences with confidence scores. See §5.9. |
| **Agent outputs and documents** | `data/documents/` | JSON/Markdown files | Inspectable, variable size, not relational |
| **Log files** | `logs/` | Text (Serilog) | Rotated, structured logging |

#### Configuration Layering

Settings are resolved in priority order (highest wins):

```
1. CLI arguments          (--port 8080)
2. Environment variables  (OPENVEPA__PORT=8080)
3. User secrets           (dotnet user-secrets, for sensitive values)
4. config/*.json files    (appsettings.json, providers.json, channels.json)
5. Built-in defaults      (hardcoded sensible defaults)
```

This uses `Microsoft.Extensions.Configuration` with the standard provider chain. The `__` (double underscore) separator maps environment variables to nested JSON keys.

#### Agent and Skill Config Examples

**Agent config** (`agents/research-agent/config.json`):
```json
{
  "autonomyLevel": 3,
  "clarifyBeforeStart": false,
  "continueWithoutAsking": true,
  "defaultProvider": "anthropic",
  "defaultModel": "claude-sonnet",
  "enabledSkills": ["web-search", "document-analysis", "summarize-article"],
  "maxConcurrentTasks": 3,
  "taskTimeLimit": "04:00:00",
  "llmBudget": {
    "maxTokensPerTask": 500000,
    "maxCostPerTask": 5.00,
    "action": "pause"
  }
}
```

**Skill config** (`skills/web-search/config.json`):
```json
{
  "searchProvider": "brave",
  "maxResults": 10,
  "safeSearch": true
}
```

> **Note:** Sensitive values (API keys, tokens) **must** use environment variables or `dotnet user-secrets`, not plaintext in config files. For example, the Brave Search API key should be set via the `BRAVE_SEARCH_API_KEY` environment variable or `dotnet user-secrets set "Skills:WebSearch:ApiKey" "<value>"`. The application reads these through the standard `Microsoft.Extensions.Configuration` layering (see resolution order above). There is no custom `${env:}` interpolation syntax.

### 8.6 Data Lifecycle and Migration

#### Schema Migration

- **EF Core Migrations** applied automatically on startup.
- **Pre-migration backup**: before any migration, automatically back up `openvepa.db` to `data/backups/openvepa-YYYYMMDD-HHMMSS.db`.
- **Migration failure**: rollback to backup, log error, start in read-only mode with user notification.
- **Version compatibility**: the database tracks schema version. OpenVEPA v1.2 can read v1.0 data (forward migrations). Downgrade is not supported (backup first).

#### Backup and Restore

| Command | Description |
|---------|-------------|
| `openvepa backup` | Creates timestamped archive of entire `~/.openvepa/` directory (config + data + skills + agents). |
| `openvepa backup --data-only` | Backs up the `data/` directory only. |
| `openvepa restore <archive>` | Restores from archive (confirms before overwriting). |

- **Auto-backup triggers**: `dotnet tool update`, any schema migration, `openvepa restore`.
- **Backup location**: `data/backups/` (configurable via `Storage:BackupDirectory`).
- **Retention**: keep last 5 backups by default (configurable via `Storage:BackupRetentionCount`).

#### Data Retention

| Data Type | Policy | Default |
|-----------|--------|---------|
| **Sessions** | Archive to compressed storage after configurable period. | 90 days |
| **Application logs** | Serilog file sink with daily rotation. | Retain 30 days |
| **LLM audit logs** | Timestamped request/response records. | Retain 30 days (configurable) |
| **Token usage logs** | Aggregate monthly; retain daily detail. | 30 days daily, then monthly aggregates |

---

## 9. Security and Privacy

### 9.1 Data Classification

| Category | Cloud LLM Allowed | Examples |
|----------|-------------------|----------|
| **Public** | Yes | General research, public news |
| **Private** | Local only | Personal files, financial data |
| **Configurable** | User decides | Project-specific data |

Privacy rules determine which data categories route to cloud LLMs versus local models (Ollama). Configuration-driven, per-skill and per-task.

### 9.2 Authentication and Access

**WebSocket token authentication:**

WebSocket connections are secured with bearer tokens. The `openvepa` CLI is the primary tool for token management:

| Command | Description |
|---------|-------------|
| `openvepa token create --name <name> [--expires <duration>]` | Generate a new access token with optional expiry |
| `openvepa token list` | List all active tokens with name, created date, expiry, last used |
| `openvepa token revoke <token-id>` | Revoke a specific token immediately |
| `openvepa token rotate <token-id>` | Revoke old token and issue a replacement |

**Token properties:**
- Tokens are opaque strings (cryptographically random, base64url-encoded)
- Stored as **bcrypt** hashes with per-token random salt in `data/openvepa.db` → `access_tokens` table
- The plaintext token is displayed **exactly once** on creation. There is no recovery mechanism; revoke and recreate if lost.
- Each token has a name (for identification), creation date, optional expiry, and last-used timestamp
- Tokens are passed as bearer tokens in the SignalR connection query string or HTTP header

**Authentication rules by client type:**

| Client | Auth Required | Method |
|--------|--------------|--------|
| **TUI (local)** | Yes (automatic) | Auto-generated ephemeral loopback token created on startup, stored in a temp file with user-only permissions (`600`). No manual token management required. Token is regenerated each launch. |
| **Web GUI** | Yes | Bearer token in SignalR connection |
| **Messaging adapters** | Yes | Each adapter configured with its own token |
| **External CLI** | Yes | Token passed via `--token` flag or environment variable |

**Additional access controls:**
- API key management for external LLM providers via `Microsoft.Extensions.Configuration` user secrets
- Encrypted storage for sensitive credentials (Data Protection API)
- Token-per-adapter: each messaging channel adapter gets its own token (revoke one without affecting others)

### 9.3 Hub Security

| Control | Description |
|---------|-------------|
| **Permission Scoping** | Packages from the Hub run in a defined permission scope |
| **Resource Declarations** | Skills must declare what system resources they access (network, filesystem, etc.) |
| **User Approval** | Users approve permissions on install |
| **Local-Only Mode** | Sideload packages from disk without connecting to the Hub. Air-gapped environments. |
| **Process Isolation** | MCP-bridge and hybrid skills run MCP servers in separate processes (crash isolation, permission boundary) |
| **Assembly Isolation** | Native skills load in separate `AssemblyLoadContext` (dependency isolation). Native execution is restricted to built-in skills only. |
| **External Skill Isolation** | Hub-installed and sideloaded skills **must** use MCP-bridge execution (process-isolated). Native in-process execution requires code signing (Phase 2). |

### 9.4 Channel Security

| Control | Description |
|---------|-------------|
| **Per-Adapter Auth** | Each messaging channel adapter authenticates against its platform (bot tokens, OAuth2) |
| **Allowed Users** | Configuration whitelist of allowed user IDs per channel (e.g., `AllowedUsers` in Telegram config) |
| **Message Validation** | Inbound messages validated before forwarding to assistant core |
| **Token Security** | Bot tokens stored in user secrets or environment variables, never in source code |
| **HTTPS** | All external API communication over TLS |

---

## 10. Technology Stack Summary

Master reference table covering all technology choices:

| Area | Technology | NuGet Package | Notes |
|------|-----------|--------------|-------|
| **Platform** | .NET 10 (LTS), C# 14 | -- | Cross-platform. LTS until Nov 2028. |
| **CLI / TUI** | Spectre.Console.Cli | `Spectre.Console.Cli` | Rich terminal UI + command routing |
| **Dependency Injection** | Microsoft.Extensions.DI | `Microsoft.Extensions.DependencyInjection` | Built-in. Constructor injection, keyed services. |
| **Configuration** | Microsoft.Extensions.Configuration | `Microsoft.Extensions.Configuration` | Layered: CLI args > env vars > user secrets > JSON files. See §8.5 for directory layout. |
| **Logging** | Microsoft.Extensions.Logging + Serilog | `Serilog.AspNetCore` | Structured logging. Console + file sinks. |
| **JSON** | System.Text.Json | BCL (built-in) | Source generators for AOT. 2-3x faster than Newtonsoft. |
| **HTTP Clients** | Refit + HttpClient | `Refit` + `Microsoft.Extensions.Http` | Refit for Hub API. IHttpClientFactory for provider SDKs. |
| **ORM / Database** | EF Core + SQLite | `Microsoft.EntityFrameworkCore.Sqlite` | Runtime data only (sessions, messages, tasks, audit). PostgreSQL upgrade via Npgsql. |
| **Cache** | HybridCache | `Microsoft.Extensions.Caching.Hybrid` | L1 memory + L2 configurable. Redis upgrade via DI swap. |
| **Vectors** | SQLite + sqlite-vec | `Microsoft.Extensions.VectorData` + SK connector (`Microsoft.SemanticKernel.Connectors.Sqlite`) | Qdrant upgrade via `Microsoft.Extensions.VectorData` abstraction |
| **Queue** | System.Threading.Channels | BCL (built-in) | MassTransit + RabbitMQ upgrade path |
| **Scheduler** | Hangfire (SQLite) | `Hangfire.Core` + `Hangfire.Storage.SQLite` | Built-in web dashboard. Quartz.NET for enterprise. |
| **WebSocket / Real-time** | SignalR on ASP.NET Core | `Microsoft.AspNetCore.SignalR.Client` (client) | JSON default, MessagePack optional. Redis backplane for scale. |
| **LLM Abstraction** | Microsoft.Extensions.AI | `Microsoft.Extensions.AI.Abstractions` | `IChatClient` / `IEmbeddingGenerator<T>` |
| **LLM Orchestration** | Microsoft Agent Framework (Phase 3) | `Microsoft.Agents.AI` | Agent orchestration, workflows, multi-agent. Replaces SK orchestration (maintenance mode). SK infrastructure connectors retained for vector stores. Additive to M.E.AI. |
| **MCP** | Official C# SDK | `ModelContextProtocol` | Skill implementation detail: MCP servers as skill dependencies |
| **Skill Format** | SKILL.md (agentskills.io spec) | -- | YAML frontmatter + Markdown. Directory-based. Progressive disclosure. |
| **Agent Format** | `<name>.agent.md` (OpenVEPA spec) | -- | YAML frontmatter + Markdown. Per-directory. `AGENT.md` reserved for project context standards. |
| **Testing** | xUnit + NSubstitute + FluentAssertions | `xunit` + `NSubstitute` + `FluentAssertions` | Parallel by default, DI-friendly, expressive assertions |

---

## 11. Open Questions

| # | Question | Context |
|---|----------|---------|
| 1 | **Web GUI framework?** | React, Vue, Blazor, or other? Needs SignalR JS client integration. |
| 2 | **Hub API design?** | REST, GraphQL, or both? Must support browse, install, publish, search. |
| 3 | **Hub hosting?** | Central public instance, self-hosted registry option, or both? |
| 4 | ~~**Skill sandboxing?**~~ | **RESOLVED** — See §3.3 Trust and Safety and §9.3 Hub Security. External skills (Hub-installed or sideloaded) must use MCP-bridge execution (process-isolated). Built-in skills only may run native in-process. Phase 2 introduces code signing for trusted native Hub skills. |
| 5 | ~~**Long-running tasks across restarts?**~~ | **RESOLVED** — See §5.4 Long-Running Tasks. Checkpoint-based resumability with Hangfire durable queues. |
| 6 | ~~**Backup and restore strategy?**~~ | **RESOLVED** — See §8.6 Data Lifecycle and Migration. CLI commands (`openvepa backup`, `openvepa restore`), auto-backup before migrations, configurable retention. |
| 7 | **TTS engine for briefings?** | .NET-compatible text-to-speech library selection. |
| 8 | **Native AOT for CLI tools?** | Main process uses JIT (plugin system needs reflection). AOT for standalone utilities? |