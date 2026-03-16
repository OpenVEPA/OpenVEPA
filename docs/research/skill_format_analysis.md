# Analysis: SKILL.md Format Adoption for OpenVEPA

## 1. Objective and Scope

**Objective**: Document the SKILL.md format specification (agentskills.io v1.0), define OpenVEPA-specific extensions, and design the skill execution and distribution model.

**Scope**: Format specification, OpenVEPA metadata extensions, MCP server dependency management, Hub distribution model, skill execution modes, and discovery/loading flow.

**Out of scope**: Implementation code, Hub API design, ISkill interface definition, LLM routing.

**Key decisions already made:**
1. MCP is secondary to skills. MCP is an implementation detail within a skill, not the skill itself.
2. Hub tracks installed MCP servers. If a new skill needs an MCP server already installed locally, it is not reinstalled.
3. Hub is distribution-only. Nothing runs from the Hub. Everything is installed locally.
4. Skills use the SKILL.md format.

---

## 2. SKILL.md Format Specification

Source: [agentskills.io/specification](https://agentskills.io/specification), [github.com/agentskills/agentskills](https://github.com/agentskills/agentskills), [github.com/anthropics/skills](https://github.com/anthropics/skills).

### 2.1 Directory Structure

A skill is a directory containing at minimum a `SKILL.md` file:

```
skill-name/
├── SKILL.md          # Required: YAML frontmatter + Markdown instructions
├── scripts/          # Optional: executable code the agent can run
├── references/       # Optional: additional documentation loaded on demand
└── assets/           # Optional: templates, data files, schemas
```

The directory name MUST match the `name` field in the SKILL.md frontmatter.

### 2.2 SKILL.md File Format

The file starts with YAML frontmatter (delimited by `---`), followed by Markdown body content.

**Example:**

```yaml
---
name: pdf-processing
description: Extract text and tables from PDF files, fill forms, merge documents. Use when working with PDF documents or when the user mentions PDFs, forms, or document extraction.
license: Apache-2.0
compatibility: Requires pdfplumber and access to local filesystem.
metadata:
  author: example-org
  version: "1.0"
allowed-tools: pdfplumber pdftk
---

# PDF Processing

## When to use this skill

Use this skill to extract information from PDFs...
```

### 2.3 Frontmatter Fields

| Field | Required | Constraints | Purpose |
|-------|----------|-------------|---------|
| `name` | Yes | Max 64 chars. Lowercase alphanumeric + hyphens. No leading/trailing hyphens. No consecutive hyphens (`--`). Must match directory name. | Unique skill identifier |
| `description` | Yes | Max 1024 chars. Non-empty. Should describe what the skill does AND when to use it. | Discovery and activation |
| `license` | No | Short name or bundled file reference | Legal clarity |
| `compatibility` | No | Max 500 chars. Environment requirements. | System prerequisites |
| `metadata` | No | Arbitrary YAML key-value map (string keys to string values) | Extensibility point |
| `allowed-tools` | No | Space-delimited tool list. Experimental. | Pre-approved tool invocations |

**Name validation rules:**
- 1-64 characters
- Lowercase letters (`a-z`), numbers (`0-9`), and hyphens (`-`) only
- Must not start or end with `-`
- Must not contain consecutive hyphens (`--`)
- Must match the parent directory name exactly

**Description guidance:**
- Include specific keywords that help agents identify relevant tasks
- Describe both what the skill does and when to use it
- Good: "Extracts text and tables from PDF files, fills PDF forms, and merges multiple PDFs. Use when working with PDF documents or when the user mentions PDFs, forms, or document extraction."
- Poor: "Helps with PDFs."

### 2.4 Body Content

The Markdown body after frontmatter contains skill instructions. No format restrictions. Write whatever helps agents perform the task.

Recommended sections:
- Step-by-step instructions
- Examples of inputs and outputs
- Common edge cases

The agent loads the entire body when the skill activates. Keep it under 500 lines. Move detailed reference material to separate files.

### 2.5 Progressive Disclosure

The spec defines a three-tier loading model:

| Tier | What | Token Budget | When Loaded |
|------|------|-------------|-------------|
| 1. Metadata | `name` and `description` fields | ~100 tokens | Startup, all skills |
| 2. Instructions | Full SKILL.md body | < 5000 tokens recommended | Skill activation |
| 3. Resources | Files in `scripts/`, `references/`, `assets/` | As needed | On demand during execution |

This model minimizes context window consumption. Only metadata is loaded at startup. Full instructions load on activation. Supporting files load on demand.

### 2.6 File References

Reference other files using relative paths from the skill root:

```markdown
See [the reference guide](references/REFERENCE.md) for details.

Run the extraction script:
scripts/extract.py
```

Keep references one level deep from SKILL.md. Avoid deeply nested reference chains.

### 2.7 Validation

The `skills-ref` reference library provides validation:

```bash
skills-ref validate ./my-skill
```

Checks:
- SKILL.md frontmatter is valid YAML
- `name` and `description` fields present and within constraints
- `name` matches parent directory
- Naming convention compliance (lowercase, hyphens, no consecutive hyphens)
- Referenced files exist (if specified)

---

## 3. OpenVEPA Extensions to SKILL.md

OpenVEPA extends the spec through the `metadata` field. The spec explicitly allows arbitrary key-value mappings in `metadata`. All OpenVEPA-specific keys use the `openvepa-` prefix to avoid conflicts with other implementations.

### 3.1 Extended Metadata Schema

```yaml
---
name: web-search
description: Search the web and return structured results. Use when the user asks a question requiring current information or internet lookup.
license: MIT
compatibility: Requires internet access.
metadata:
  # Standard metadata
  author: openvepa
  version: "1.2.0"

  # OpenVEPA extensions
  openvepa-type: mcp-bridge                       # Execution mode: native | mcp-bridge | hybrid
  openvepa-mcp-servers:                            # MCP servers this skill depends on
    - name: web-search
      package: "@anthropic/mcp-web-search"
      version: "^1.0"
      transport: stdio                             # stdio | http
    - name: brave-search
      package: "@anthropic/mcp-brave-search"
      version: "^1.0"
      transport: stdio
      optional: true                               # Skill works without this server
  openvepa-dotnet-assembly: WebSearchSkill.dll     # For native/hybrid skills
  openvepa-nuget-package: OpenVEPA.Skills.WebSearch # NuGet package reference
  openvepa-min-runtime: "10.0"                     # Minimum .NET version
  openvepa-category: research                      # Skill category for Hub browsing
  openvepa-permissions:                            # Declared permissions
    - network
    - filesystem-read
---
```

### 3.2 Extension Field Reference

| Field | Applies To | Constraints | Purpose |
|-------|-----------|-------------|---------|
| `openvepa-type` | All skills | `native`, `mcp-bridge`, or `hybrid` | Determines execution mode |
| `openvepa-mcp-servers` | `mcp-bridge` and `hybrid` | Array of server dependency objects | MCP server requirements |
| `openvepa-dotnet-assembly` | `native` and `hybrid` | DLL filename | Assembly to load via ALC |
| `openvepa-nuget-package` | All skill types | NuGet package ID | Package reference for Hub |
| `openvepa-min-runtime` | `native` and `hybrid` | Semver string | Minimum .NET runtime version |
| `openvepa-category` | All skills | One of: `research`, `content`, `data`, `integration`, `development`, `system` | Hub categorization |
| `openvepa-permissions` | All skills | Array of permission identifiers | Security declaration |

### 3.3 MCP Server Dependency Object

Each entry in `openvepa-mcp-servers`:

| Field | Required | Description |
|-------|----------|-------------|
| `name` | Yes | Logical name matching the MCP server identity |
| `package` | Yes | Package identifier (npm scope/name, NuGet ID, Docker image) |
| `version` | Yes | Semver range (e.g., `^1.0`, `>=2.0.0 <3.0.0`) |
| `transport` | No | `stdio` (default) or `http` |
| `optional` | No | `true` if skill degrades gracefully without this server. Default: `false` |
| `config` | No | Key-value map of server configuration (env vars, args) |

### 3.4 Execution Type Examples

**Native skill (in-process .NET):**

```yaml
metadata:
  openvepa-type: native
  openvepa-dotnet-assembly: CoreMemory.dll
  openvepa-nuget-package: OpenVEPA.Skills.CoreMemory
  openvepa-min-runtime: "10.0"
  openvepa-category: system
```

**MCP bridge skill (wraps external MCP server):**

```yaml
metadata:
  openvepa-type: mcp-bridge
  openvepa-mcp-servers:
    - name: github
      package: "@modelcontextprotocol/server-github"
      version: "^1.0"
      transport: stdio
  openvepa-category: development
```

**Hybrid skill (.NET code + MCP dependency):**

```yaml
metadata:
  openvepa-type: hybrid
  openvepa-dotnet-assembly: ResearchSkill.dll
  openvepa-nuget-package: OpenVEPA.Skills.Research
  openvepa-min-runtime: "10.0"
  openvepa-mcp-servers:
    - name: web-search
      package: "@anthropic/mcp-web-search"
      version: "^1.0"
      transport: stdio
  openvepa-category: research
```

---

## 4. MCP Server Dependency Management

### 4.1 Design Principle

MCP servers are shared infrastructure, not per-skill resources. Multiple skills can declare a dependency on the same MCP server. The system tracks installed servers globally and reuses them.

### 4.2 Local MCP Server Registry

OpenVEPA maintains a local registry of installed MCP servers. Stored as a JSON file at `~/.openvepa/mcp-servers.json`:

```json
{
  "servers": [
    {
      "name": "web-search",
      "package": "@anthropic/mcp-web-search",
      "installedVersion": "1.2.0",
      "installPath": "~/.openvepa/mcp-servers/web-search/",
      "transport": "stdio",
      "command": "npx",
      "args": ["-y", "@anthropic/mcp-web-search@1.2.0"],
      "installedAt": "2025-07-10T14:30:00Z",
      "usedBySkills": ["web-search", "research-agent"],
      "status": "installed"
    },
    {
      "name": "github",
      "package": "@modelcontextprotocol/server-github",
      "installedVersion": "1.0.3",
      "installPath": "~/.openvepa/mcp-servers/github/",
      "transport": "stdio",
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-github@1.0.3"],
      "installedAt": "2025-07-09T10:00:00Z",
      "usedBySkills": ["github-tools"],
      "status": "installed"
    }
  ]
}
```

**Registry fields per server:**

| Field | Purpose |
|-------|---------|
| `name` | Logical name (matches skill dependency declarations) |
| `package` | Package identifier |
| `installedVersion` | Exact installed version |
| `installPath` | Local filesystem path |
| `transport` | `stdio` or `http` |
| `command` | Executable command to start the server |
| `args` | Command arguments |
| `installedAt` | Installation timestamp |
| `usedBySkills` | List of skill names depending on this server |
| `status` | `installed`, `updating`, `broken` |

### 4.3 Dependency Resolution Flow

When a skill is installed:

```
1. Parse SKILL.md → extract openvepa-mcp-servers list
2. For each MCP server dependency:
   a. Check local registry: is this server already installed?
   b. If installed: check version compatibility (semver range match)
      - Compatible → add skill to usedBySkills → done
      - Incompatible → attempt update if no other skill needs the old version
      - Incompatible + other skills need old version → install side-by-side (versioned path)
   c. If not installed: install the MCP server package
      - npm: npm install --prefix ~/.openvepa/mcp-servers/[name]/ [package]@[version]
      - NuGet: dotnet tool install [package] --tool-path ~/.openvepa/mcp-servers/[name]/
      - Docker: docker pull [image]:[version]
   d. Register in local MCP server registry
   e. Add skill to usedBySkills
3. Skill installation complete
```

### 4.4 Shared MCP Server Instances

MCP server processes are shared across skills that use them.

**Lifecycle rules:**

| Event | Action |
|-------|--------|
| Skill activates, needs MCP server | Check if server process is running. If not, start it. |
| Another skill activates, needs same server | Reuse running process. Increment reference count. |
| Skill deactivates | Decrement reference count. If zero, stop server after idle timeout. |
| Idle timeout (configurable, default 5 minutes) | Stop the MCP server process to free resources. |
| All skills uninstalled for a server | Remove server from registry. Delete install directory. |

**Process management:**

```
MCP Server Process Pool
├── web-search (PID 1234, ref_count: 2, started: 14:30)
│   ├── used by: web-search skill
│   └── used by: research-agent skill
├── github (PID 1235, ref_count: 1, started: 14:35)
│   └── used by: github-tools skill
└── filesystem (not running, ref_count: 0, last used: 14:20)
```

**Connection multiplexing:**

For stdio transport: one process per MCP server. All skills share the same process. The MCP client within OpenVEPA routes requests to the correct server process.

For HTTP transport: one HTTP endpoint per MCP server. Multiple skills connect as clients to the same endpoint.

### 4.5 MCP Server Lifecycle Commands

```bash
# List installed MCP servers
openvepa mcp list

# Server details
openvepa mcp info web-search

# Manual install (outside skill installation)
openvepa mcp install @anthropic/mcp-web-search --version "^1.0"

# Update a server
openvepa mcp update web-search

# Remove a server (fails if skills depend on it)
openvepa mcp remove web-search

# Remove a server and warn about dependent skills
openvepa mcp remove web-search --force
```

---

## 5. Hub Distribution Model

### 5.1 Core Principle

The Hub is a registry. It stores metadata and downloadable packages. Nothing executes on the Hub. All execution happens on the user's local machine.

**Analogy comparison:**

| Concept | NuGet.org | npm registry | OpenVEPA Hub |
|---------|-----------|-------------|-------------|
| Role | Package registry | Package registry | Skill and agent registry |
| Execution | None | None | None |
| Download | NuGet packages (.nupkg) | Tarballs (.tgz) | Skill packages |
| Install target | Local project | Local node_modules | Local ~/.openvepa/skills/ |
| Metadata | Package manifest | package.json | SKILL.md frontmatter |
| Versioning | Semver | Semver | Semver |

### 5.2 Package Format

A Hub package is a distributable archive containing:

```
web-search-1.2.0.opvpkg
├── SKILL.md                           # Required: skill definition
├── scripts/                           # Optional: executable scripts
├── references/                        # Optional: documentation
├── assets/                            # Optional: templates, data
├── lib/                               # Optional: compiled .NET assembly
│   └── net10.0/
│       └── WebSearchSkill.dll
├── MANIFEST.json                      # Required: package metadata
└── CHANGELOG.md                       # Optional: version history
```

**MANIFEST.json:**

```json
{
  "packageId": "openvepa.skills.web-search",
  "version": "1.2.0",
  "skillName": "web-search",
  "type": "mcp-bridge",
  "publishedAt": "2025-07-10T14:00:00Z",
  "publisher": "openvepa",
  "checksum": "sha256:abc123...",
  "dependencies": {
    "mcpServers": [
      {
        "name": "web-search",
        "package": "@anthropic/mcp-web-search",
        "version": "^1.0"
      }
    ],
    "nugetPackages": [],
    "minRuntime": "10.0"
  },
  "permissions": ["network"],
  "channel": "stable"
}
```

### 5.3 Version Management

| Feature | Behavior |
|---------|----------|
| Versioning | Semantic versioning (major.minor.patch) |
| Channels | `stable`, `beta`, `experimental` (mapped to semver prerelease tags) |
| Update checking | Hub client polls for new versions on configurable schedule |
| Update install | Download new package, validate, replace old version |
| Rollback | Previous version retained in `~/.openvepa/skills/.archive/`. `openvepa skill rollback [name]` restores it. |
| Pinning | `openvepa skill pin [name] [version]` prevents auto-update |

### 5.4 Installation Flow

```
1. User: openvepa skill install web-search
2. Hub client queries Hub API: GET /api/skills/web-search/latest
3. Hub returns package metadata (version, checksum, download URL)
4. Hub client downloads package archive
5. Verify checksum (SHA-256)
6. Extract to ~/.openvepa/skills/web-search/
7. Parse SKILL.md frontmatter
8. Resolve MCP server dependencies (see Section 4.3)
9. For native/hybrid: verify .NET runtime compatibility
10. Register skill in local skill registry
11. Skill available for use
```

### 5.5 Local-Only Mode

Users can install skills without the Hub:

```bash
# Install from local directory
openvepa skill install --path ./my-custom-skill/

# Install from local archive
openvepa skill install --file ./my-skill-1.0.0.opvpkg
```

Local skills follow the same SKILL.md format. They participate in the same registry. The only difference is the package source.

---

## 6. How Skills Execute in OpenVEPA

### 6.1 Three Execution Modes

| Mode | `openvepa-type` | Process Model | Use Case |
|------|-----------------|--------------|----------|
| **Native** | `native` | In-process via AssemblyLoadContext | Performance-critical, core skills, deep OpenVEPA integration |
| **MCP Bridge** | `mcp-bridge` | Child process(es) via MCP protocol | Community MCP servers, cross-language skills |
| **Hybrid** | `hybrid` | In-process assembly + child MCP process(es) | .NET skill that also needs external MCP tools |

### 6.2 Native Execution

```
OpenVEPA Process
├── AssemblyLoadContext: core-memory
│   └── CoreMemory.dll → implements ISkill
├── AssemblyLoadContext: task-scheduler
│   └── TaskScheduler.dll → implements ISkill
└── AssemblyLoadContext: llm-router
    └── LlmRouter.dll → implements ISkill
```

**Characteristics:**
- .NET assembly loaded via `AssemblyLoadContext` with `isCollectible: true`
- Implements `ISkill` interface from `OpenVEPA.Core`
- Direct access to OpenVEPA services (memory, config, LLM, SignalR hubs)
- Each skill directory gets its own ALC to prevent dependency conflicts
- Hot-swap: unload ALC and reload updated assembly without process restart
- SKILL.md provides metadata and instructions; the assembly provides execution

**SKILL.md role for native skills:** The SKILL.md body serves as the LLM-readable instructions. The Assistant reads SKILL.md to understand when and how to use the skill. The assembly provides the actual execution logic. SKILL.md and the DLL work together.

### 6.3 MCP Bridge Execution

```
OpenVEPA Process
├── McpBridgeSkill: web-search
│   └── MCP Client → stdio → web-search MCP server (PID 1234)
├── McpBridgeSkill: github-tools
│   └── MCP Client → stdio → github MCP server (PID 1235)
└── McpBridgeSkill: database-query
    └── MCP Client → http → database MCP server (port 8080)
```

**Characteristics:**
- No .NET assembly for the skill itself
- `McpBridgeSkill` (built into OpenVEPA) wraps any MCP server
- OpenVEPA starts the MCP server as a child process (stdio) or connects to it (HTTP)
- MCP protocol handles tool discovery (`tools/list`) and execution (`tools/call`)
- SKILL.md provides the metadata and instructions; the MCP server provides execution
- The LLM sees the same unified tool interface as native skills

**SKILL.md role for MCP bridge skills:** The SKILL.md body is the primary instruction set. It tells the Assistant when to use the skill, what parameters to pass, and what outputs to expect. The MCP server provides the tools, but SKILL.md is how the Assistant knows to use them.

### 6.4 Hybrid Execution

```
OpenVEPA Process
├── AssemblyLoadContext: research-agent
│   └── ResearchSkill.dll → implements ISkill
│       └── internally creates MCP Client → web-search server (PID 1234)
```

**Characteristics:**
- .NET assembly loaded in-process (like native)
- Assembly also depends on one or more MCP servers
- The skill manages its own MCP client connections internally
- Useful when: .NET code provides orchestration logic, MCP servers provide tools
- Example: a research skill written in C# that orchestrates web search (MCP) + summarization (in-process LLM call) + memory storage (native OpenVEPA API)

---

## 7. Skill Discovery and Loading Flow

### 7.1 Full Lifecycle

```
Phase 1: Startup (Metadata Only)
┌─────────────────────────────────────────────────────────┐
│ 1. Scan ~/.openvepa/skills/ directories                 │
│ 2. For each directory: read SKILL.md YAML frontmatter   │
│    - Parse: name, description, metadata                 │
│    - Extract: openvepa-type, openvepa-mcp-servers       │
│    - Cost: ~100 tokens per skill (metadata only)        │
│ 3. Build in-memory skill registry                       │
│    - name → description → type → dependencies           │
│ 4. Present skill list to LLM for tool selection         │
└─────────────────────────────────────────────────────────┘

Phase 2: Activation (Full Instructions)
┌─────────────────────────────────────────────────────────┐
│ 5. LLM selects a skill based on user request            │
│ 6. Load full SKILL.md body (Markdown instructions)      │
│    - Cost: < 5000 tokens (recommended limit)            │
│ 7. Parse instructions for execution guidance             │
└─────────────────────────────────────────────────────────┘

Phase 3: Execution Setup (Type-Dependent)
┌─────────────────────────────────────────────────────────┐
│ For native skills:                                       │
│ 8a. Load .NET assembly via AssemblyLoadContext           │
│ 8b. Resolve ISkill implementation                        │
│ 8c. Inject OpenVEPA services via DI                     │
│                                                          │
│ For mcp-bridge skills:                                   │
│ 8a. Check MCP server registry → is server running?      │
│ 8b. If not running: start MCP server process             │
│ 8c. Create MCP client connection                         │
│ 8d. Handshake: capability negotiation                    │
│ 8e. Discover tools: tools/list                           │
│                                                          │
│ For hybrid skills:                                       │
│ 8a. Load .NET assembly (same as native)                 │
│ 8b. Start required MCP servers (same as mcp-bridge)     │
│ 8c. Inject MCP client into skill via DI                 │
└─────────────────────────────────────────────────────────┘

Phase 4: Execution
┌─────────────────────────────────────────────────────────┐
│ 9. Execute skill with structured input                   │
│ 10. Skill returns structured result                     │
│ 11. Result presented to LLM / user                      │
└─────────────────────────────────────────────────────────┘

Phase 5: Cleanup
┌─────────────────────────────────────────────────────────┐
│ 12. Decrement MCP server reference count                │
│ 13. If ref_count == 0: start idle timer                 │
│ 14. On idle timeout: stop MCP server process            │
│ 15. Native assemblies remain loaded (ALC stays warm)    │
│     - Unload only on skill update or explicit uninstall │
└─────────────────────────────────────────────────────────┘
```

### 7.2 Startup Performance

| Skill Count | Metadata Load (estimated) | Token Cost |
|------------|--------------------------|------------|
| 10 skills | < 50ms | ~1000 tokens |
| 50 skills | < 200ms | ~5000 tokens |
| 100 skills | < 500ms | ~10000 tokens |

Metadata loading reads only YAML frontmatter. No assembly loading, no MCP server starts, no network calls. This keeps startup fast regardless of installed skill count.

### 7.3 Activation Latency

| Execution Type | First Activation | Subsequent Activations |
|---------------|-----------------|----------------------|
| Native | 50-200ms (assembly load) | < 5ms (cached) |
| MCP Bridge | 500-2000ms (process start + handshake) | < 50ms (process reuse) |
| Hybrid | 500-2000ms (assembly + process start) | < 50ms (both cached) |

MCP server processes remain running after first activation. The idle timeout (default: 5 minutes) determines when they shut down. Subsequent activations within the timeout window reuse the running process.

---

## 8. Alignment with Existing Architecture

### 8.1 Mapping to Project Requirements

| Requirement (project_requirements.md) | SKILL.md Mapping |
|---------------------------------------|-----------------|
| `ISkill` interface | Native skills implement ISkill. SKILL.md provides metadata + instructions. |
| `McpBridgeSkill` | MCP bridge skills wrap MCP servers. SKILL.md declares dependencies via `openvepa-mcp-servers`. |
| Hub distributes NuGet + MCP packages | Hub distributes `.opvpkg` archives containing SKILL.md + optional assembly + MCP references. |
| AssemblyLoadContext per skill | Each native/hybrid skill directory maps to one ALC. |
| `isCollectible: true` | ALC supports hot-swap. SKILL.md is re-read on skill update. |
| Skill categories | `openvepa-category` metadata field maps to the 6 categories defined in requirements. |
| Skill lifecycle (discover, install, configure, execute, update) | SKILL.md format + Hub distribution model covers all 5 lifecycle stages. |

### 8.2 What SKILL.md Replaces

Before SKILL.md adoption, skill metadata was defined only through `ISkill.Manifest` in code. SKILL.md provides a human-readable and LLM-readable representation that exists alongside the code.

| Before | After |
|--------|-------|
| Skill metadata in C# code only | Metadata in SKILL.md frontmatter (YAML) + C# code |
| LLM instructions embedded in code or config | LLM instructions in SKILL.md body (Markdown) |
| No standard format across platforms | Standard format shared with other agent ecosystems |
| Discovery requires loading assemblies | Discovery reads YAML frontmatter only |

### 8.3 Compatibility with MCP Analysis

The MCP analysis (`docs/research/mcp_dotnet_analysis.md`) recommended Option C (Hybrid Bridge). SKILL.md adoption aligns with this:

- Option C's native skills = `openvepa-type: native`
- Option C's MCP bridge skills = `openvepa-type: mcp-bridge`
- Option C's hybrid = `openvepa-type: hybrid`
- The `McpBridgeSkill` wrapper pattern is preserved
- MCP servers are tracked as shared dependencies, not per-skill resources

---

## 9. Recommendations

| Priority | Recommendation | Rationale | Effort |
|----------|----------------|-----------|--------|
| P0 | Adopt SKILL.md format as the skill definition standard | Aligns with emerging industry standard. Enables cross-platform skill sharing. | Low |
| P0 | Implement `openvepa-type` metadata extension | Required for the three execution modes (native, mcp-bridge, hybrid). | Low |
| P0 | Implement `openvepa-mcp-servers` metadata extension | Required for MCP dependency resolution. | Medium |
| P1 | Build MCP server local registry (`mcp-servers.json`) | Enables shared MCP server tracking and prevents duplicate installations. | Medium |
| P1 | Build SKILL.md frontmatter parser | Read YAML frontmatter at startup for skill discovery. | Medium |
| P1 | Define `.opvpkg` package format | Required for Hub distribution. | Medium |
| P2 | Implement MCP server process pool | Shared instances with reference counting and idle timeout. | High |
| P2 | Build SKILL.md validation tooling | Validate skills at install time and publish time. | Low |

---

## 10. Conclusion

**Verdict**: Proceed. Adopt the SKILL.md format specification from agentskills.io.

**Confidence**: High.

**Rationale**: The SKILL.md spec provides a lightweight, standard format for skill metadata and instructions. The `metadata` field enables OpenVEPA-specific extensions without forking the spec. The three-tier progressive disclosure model (metadata, instructions, resources) aligns with OpenVEPA's need to minimize startup cost while maximizing LLM context efficiency. The format is adopted by Anthropic's Claude ecosystem and growing in other agent platforms, reducing vendor lock-in risk.

### User Impact

- **What changes for you**: Skills gain a standard, portable definition format. Third-party skill authors use the same format as other agent ecosystems.
- **Effort required**: Low for core adoption (YAML parser + directory scanner). Medium for full MCP dependency management.
- **Risk if ignored**: OpenVEPA skills become incompatible with the emerging agent skill ecosystem. Skill authors must learn a proprietary format instead of a shared standard.

---

## 11. Appendices

### Sources Consulted

- [agentskills.io/specification](https://agentskills.io/specification) - Official SKILL.md format spec
- [github.com/anthropics/skills](https://github.com/anthropics/skills) - Anthropic skills repository
- [github.com/agentskills/agentskills](https://github.com/agentskills/agentskills) - Agent Skills community repo and spec source (docs/specification.mdx)
- `docs/project_requirements.md` - OpenVEPA project requirements (sections 3.2, 3.3, 3.4)
- `docs/research/mcp_dotnet_analysis.md` - MCP integration analysis (sections 4.1-4.3, 6.1)

### Data Transparency

- **Found**: Full SKILL.md specification from agentskills.io via GitHub API (`agentskills/agentskills/docs/specification.mdx`). Project requirements and MCP analysis for architecture alignment.
- **Not Found**: Adoption metrics for SKILL.md format outside Anthropic ecosystem. Performance benchmarks for YAML frontmatter parsing at scale (estimates are projections). No published `.opvpkg` format exists; format proposed here is original design.
