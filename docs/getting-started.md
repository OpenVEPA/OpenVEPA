# Getting Started with OpenVEPA

OpenVEPA (**V**irtual **E**xecutive **P**ersonal **A**ssistant) is a free, open-source personal AI assistant that runs locally on your machine. It connects to LLM providers (OpenAI, Ollama, etc.), supports extensible skills and agents, and communicates over WebSockets via SignalR.

This guide walks you through setting up, building, running, and developing OpenVEPA.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Clone and Build](#clone-and-build)
3. [Configuration](#configuration)
4. [Running OpenVEPA](#running-openvepa)
5. [CLI Commands](#cli-commands)
6. [Installing as a System Service](#installing-as-a-system-service)
7. [Docker Installation](#docker-installation)
8. [Project Structure](#project-structure)
9. [Architecture Overview](#architecture-overview)
10. [Adding Skills](#adding-skills)
11. [Adding Agents](#adding-agents)
12. [Running Tests](#running-tests)
13. [Development Workflow](#development-workflow)
14. [Troubleshooting](#troubleshooting)

---

## Prerequisites

| Requirement | Version | Notes |
|---|---|---|
| **.NET SDK** | **10.0** or later | [Download](https://dotnet.microsoft.com/download/dotnet/10.0) |
| **Git** | Any recent | For cloning the repository |
| **LLM Provider** | — | Either [Ollama](https://ollama.ai) (local, free) or an OpenAI API key |

> **Recommended for local development:** Install [Ollama](https://ollama.ai) and pull a model:
> ```bash
> ollama pull llama3.2
> ```

---

## Clone and Build

```bash
git clone https://github.com/OpenVEPA/OpenVEPA.git
cd OpenVEPA

# Restore dependencies and build the solution
dotnet build
```

The solution uses `OpenVEPA.slnx` (the .NET 10 XML solution format). All projects target `net10.0` with C# 14.

Verify everything is working:

```bash
dotnet test
```

You should see **113 tests passing** across 4 test projects.

---

## Configuration

### Data Directory

OpenVEPA stores all data under `~/.openvepa/` by default:

```
~/.openvepa/
├── data/
│   ├── openvepa.db          # SQLite database (sessions, tokens, preferences, audit logs)
│   └── documents/            # File-based document store
├── skills/                   # Installed skill definitions (SKILL.md files)
└── agents/                   # Agent definitions (<name>.agent.md files)
```

Override the location with the `OPENVEPA_HOME` environment variable:

```bash
export OPENVEPA_HOME=/path/to/my/openvepa
```

### appsettings.json

The default configuration is in `src/OpenVEPA.Cli/appsettings.json`:

```json
{
  "Providers": {
    "DefaultProvider": "ollama",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "ModelId": "llama3.2"
    }
  },
  "Skills": {
    "SkillsDirectory": "skills"
  },
  "OpenVEPA": {
    "Agents": {
      "AgentsDirectory": "agents"
    }
  },
  "Scheduler": {
    "Enabled": true,
    "DashboardEnabled": false,
    "Schedules": []
  }
}
```

### Using OpenAI instead of Ollama

Add your API key to configuration (environment variable or `appsettings.json`):

```json
{
  "Providers": {
    "DefaultProvider": "openai",
    "OpenAi": {
      "ApiKey": "sk-...",
      "ModelId": "gpt-4o",
      "Endpoint": "https://api.openai.com/v1"
    }
  }
}
```

Or via environment variable:

```bash
export Providers__OpenAi__ApiKey=sk-...
export Providers__DefaultProvider=openai
```

> **Security:** Never commit API keys. Use environment variables or user secrets.

---

## Running OpenVEPA

### Start the server

```bash
cd src/OpenVEPA.Cli
dotnet run
```

This starts Kestrel (HTTP server) with SignalR, the Hangfire scheduler, and the CLI interface. You'll see:

```
  ___                  __     _______ ____   _
 / _ \ _ __   ___ _ __ \ \   / / ____|  _ \ / \
| | | | '_ \ / _ \ '_ \ \ \ / /|  _| | |_) / _ \
| |_| | |_) |  __/ | | | \ V / | |___| __/ ___ \
 \___/| .__/ \___|_| |_|  \_/  |_____|_| /_/   \_\
      |_|
Server started. Press Ctrl+C to stop.
```

### Interactive chat

```bash
dotnet run -- chat
```

This opens an interactive REPL where you can converse with the assistant:

```
Created session: a1b2c3d4...
Type exit or quit to leave.

You> What can you help me with?
Assistant> I can help with a variety of tasks...
```

Resume an existing session:

```bash
dotnet run -- chat --session <session-id>
```

---

## CLI Commands

| Command | Description |
|---|---|
| `openvepa` or `openvepa start` | Start the server (default command) |
| `openvepa chat [--session <id>]` | Interactive chat REPL |
| `openvepa status` | Show system status (skills, sessions, schedules) |
| `openvepa setup` | Run the initial setup wizard (TUI or WebUI) |
| `openvepa service install` | Install as OS service (Windows/Linux/macOS) with guided setup |
| `openvepa service uninstall` | Uninstall the OS service |
| `openvepa service status` | Show service installation status |
| `openvepa skill list` | List all loaded skills |
| `openvepa profile show` | Show all user preferences |
| `openvepa profile set <key> <value> [--category <cat>]` | Set a preference |
| `openvepa profile delete <key>` | Delete a preference |
| `openvepa token create <name>` | Create an API access token |
| `openvepa token list` | List all tokens |
| `openvepa token revoke <id>` | Revoke a token |
| `openvepa schedule list` | List scheduled tasks |

> **Tip:** During development, prefix commands with `dotnet run --` (e.g., `dotnet run -- chat`).

---

## Installing as a System Service

OpenVEPA can run as a background service on Windows, Linux, and macOS.

### Install

```bash
openvepa service install
```

This command:
1. Detects your platform (Windows/Linux/macOS)
2. Asks how you'd like to be guided through setup:
   - **Terminal (TUI)** — Interactive terminal wizard (works on headless servers)
   - **Browser (WebUI)** — Opens a setup page in your browser (for GUI environments)
   - **Skip** — Uses default configuration (Ollama on localhost)
3. Writes configuration to `~/.openvepa/`
4. Installs the service using the platform-native mechanism:
   - **Windows:** Windows Service via `sc.exe`
   - **Linux:** systemd unit at `/etc/systemd/system/openvepa.service`
   - **macOS:** launchd plist at `~/Library/LaunchAgents/com.openvepa.assistant.plist`

### Setup Only (No Service)

To configure OpenVEPA without installing as a service:

```bash
openvepa setup
```

### Service Management

```bash
openvepa service status      # Check if installed and running
openvepa service uninstall   # Remove the service
```

> **Note:** On Linux, service installation requires root (`sudo openvepa service install`). On macOS, user-level LaunchAgents don't require root. On Windows, run as Administrator.

---

## Docker Installation

Docker is the fastest way to deploy OpenVEPA, especially for servers and headless environments. A single command starts the assistant with security hardening, automatic restarts, and optional Ollama integration.

### Quick Start

```bash
# Start OpenVEPA — on first run, open http://localhost:8371 to access the setup wizard
docker compose up -d

# Or pre-configure with environment variables (skips the wizard)
OPENVEPA_PROVIDER=ollama OPENVEPA_MODEL=llama3.2 docker compose --profile with-ollama up -d
```

### With a Local Ollama LLM

```bash
# Start OpenVEPA + Ollama sidecar
docker compose --profile with-ollama up -d

# Pull a model into Ollama
docker exec openvepa-ollama ollama pull llama3.2
```

### Key Environment Variables

| Variable | Default | Description |
|---|---|---|
| `OPENVEPA_PROVIDER` | *(empty)* | `ollama` or `openai`. When set, auto-generates config on first run. |
| `OPENVEPA_MODEL` | *(empty)* | Model identifier (e.g., `llama3.2`, `gpt-4o`) |
| `OPENVEPA_API_KEY` | *(empty)* | API key for OpenAI or other cloud providers |
| `OPENVEPA_PORT` | `8371` | HTTP port for the WebUI and API |
| `SSH_ENABLED` | `false` | Enable SSH access into the container |

### Data Persistence

All data (SQLite database, skills, agents, config) is stored in the `openvepa-data` Docker volume at `/home/openvepa/.openvepa`. This volume survives container restarts and upgrades.

> **Full guide:** See **[Docker Deployment Guide](docker.md)** for security hardening, GPU support, reverse proxy examples, backup procedures, and troubleshooting.

---

## Project Structure

```
OpenVEPA/
├── Directory.Build.props          # Shared build settings (net10.0, C# 14)
├── OpenVEPA.slnx                  # Solution file
├── docs/
│   ├── project_requirements.md    # Single source of truth for requirements
│   ├── getting-started.md         # This file
│   └── research/                  # Architecture research documents
├── src/
│   ├── OpenVEPA.Core              # Interfaces, records, enums (zero dependencies)
│   ├── OpenVEPA.Storage           # EF Core + SQLite, session/token/preference stores
│   ├── OpenVEPA.Providers         # LLM providers (OpenAI, Ollama) + token tracking
│   ├── OpenVEPA.Skills.Runtime    # SKILL.md parser, AssemblyLoadContext loader, MCP bridge
│   ├── OpenVEPA.Agents.Runtime    # Agent.md parser, assistant agent, tool-calling loop
│   ├── OpenVEPA.Server            # SignalR hub, token auth, health checks
│   ├── OpenVEPA.Scheduler         # Hangfire integration, cron jobs
│   ├── OpenVEPA.Cli               # Entry point: Spectre.Console CLI + self-hosted Kestrel
│   ├── OpenVEPA.Channels.Telegram # (Placeholder) Telegram channel adapter
│   ├── OpenVEPA.Channels.Discord  # (Placeholder) Discord channel adapter
│   ├── OpenVEPA.Channels.Email    # (Placeholder) Email channel adapter
│   └── OpenVEPA.Hub.Client        # (Placeholder) Hub marketplace client
└── tests/
    ├── OpenVEPA.Core.Tests        # 49 tests — type validation, record equality
    ├── OpenVEPA.Storage.Tests     # 30 tests — SQLite stores, write queue, file store
    ├── OpenVEPA.Skills.Runtime.Tests # 19 tests — SKILL.md parsing, directory scanning
    └── OpenVEPA.Server.Tests      # 15 tests — auth, health checks
```

### Dependency Flow

```
Core ← Storage ← Providers ← Skills.Runtime ← Agents.Runtime ← Server ← Cli
                                                                Scheduler ↗
```

`Core` depends on nothing. Every other project depends on `Core`. The `Cli` project references everything and is the composition root.

---

## Architecture Overview

### Key Design Patterns

| Pattern | Where | Why |
|---|---|---|
| **Single-writer SQLite** | `DatabaseWriteQueue` | All writes go through a `Channel<T>`-based queue to avoid SQLite lock contention |
| **Plugin isolation** | `NativeSkillLoader` | Skills loaded via `AssemblyLoadContext` (collectible) for safe unloading |
| **DelegatingChatClient** | `TokenTrackingChatClient` | Wraps any `IChatClient` to transparently track token usage |
| **Keyed DI** | `ProviderServiceExtensions` | `IChatClient` registered with keys `"openai"`, `"ollama"` |
| **Tool-calling loop** | `AssistantAgent` | LLM → extract function calls → execute skills → feed results back (max 10 iterations) |

### WebSocket Communication

The server exposes a SignalR hub at `/hub/assistant` with these methods:

| Hub Method | Description |
|---|---|
| `SendMessage(sessionId, message)` | Send message, receive complete response |
| `StreamMessage(sessionId, message)` | Stream response tokens via `IAsyncEnumerable` |
| `CreateSession(title)` | Create a new conversation session |
| `ListSessions()` | List all sessions |
| `GetSessionHistory(sessionId, page, pageSize)` | Get paginated message history |

### Authentication

- **Remote clients:** Bearer token in `Authorization` header or `access_token` query param. Tokens are PBKDF2-hashed in SQLite.
- **Local TUI:** Ephemeral loopback token generated on startup, valid only from `127.0.0.1`/`::1`. Written to a temp file for CLI auto-auth.

---

## Adding Skills

Skills use the [SKILL.md format](https://agentskills.io). Place `.md` files in the skills directory (`~/.openvepa/skills/` by default).

### Example: `echo.md`

```markdown
---
name: echo
version: "1.0.0"
description: Echoes the input text back
openvepa-type: native
parameters:
  - name: input
    type: string
    description: The text to echo
    required: true
outputs:
  - name: result
    type: string
    description: The echoed text
permissions:
  network: false
  fileSystem: false
---
# Echo Skill

A simple skill that returns whatever text is given to it.
```

### Skill Types

| Type | Description |
|---|---|
| `native` | .NET assembly loaded via AssemblyLoadContext — implement `ISkill` |
| `mcp-bridge` | Wraps an MCP server as a skill (Phase 1: stub) |
| `hybrid` | Combines native logic with MCP delegation (Phase 1.5) |

### Native Skill Implementation

Create a class library targeting `net10.0` that implements `ISkill`:

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

Build it, place the DLL alongside the SKILL.md in the skill directory.

---

## Adding Agents

Agent definitions use the `<name>.agent.md` format. Place them in `~/.openvepa/agents/`.

### Example: `assistant.agent.md`

```markdown
---
name: assistant
description: General-purpose personal assistant
system_prompt: |
  You are OpenVEPA, a helpful personal assistant. Be concise,
  accurate, and proactive. Complete tasks autonomously when possible.
skills:
  - echo
  - web-search
llm:
  preferred_provider: ollama
  preferred_model: llama3.2
  max_tokens: 4096
---
# Assistant Agent

The default assistant agent for OpenVEPA.
```

If no agent files are found, a built-in default assistant is used automatically.

---

## Running Tests

```bash
# Run all tests
dotnet test

# Run a specific test project
dotnet test tests/OpenVEPA.Core.Tests

# Run with verbose output
dotnet test --verbosity normal

# Run with coverage (coverlet)
dotnet test --collect:"XPlat Code Coverage"
```

### Test Stack

| Tool | Purpose |
|---|---|
| **xUnit** | Test framework |
| **FluentAssertions** | Readable assertions (`result.Should().Be(...)`) |
| **NSubstitute** | Mocking (`Substitute.For<ISkillRuntime>()`) |
| **coverlet** | Code coverage collection |

---

## Development Workflow

### Building

```bash
# Full solution build
dotnet build

# Build a specific project
dotnet build src/OpenVEPA.Core
```

### Code Style

- **Nullable reference types** enabled everywhere
- **File-scoped namespaces** (`namespace Foo;`)
- **TreatWarningsAsErrors** — no warnings allowed
- **XML doc comments** on all public members
- **ConfigureAwait(false)** on all `await` calls in library code
- Constructor null-checks with `?? throw new ArgumentNullException(...)`

### Adding a New Project

1. Create the project:
   ```bash
   dotnet new classlib -n OpenVEPA.MyModule -o src/OpenVEPA.MyModule
   ```
2. Remove the auto-generated `Class1.cs`
3. Add it to the solution:
   ```bash
   dotnet sln OpenVEPA.slnx add src/OpenVEPA.MyModule
   ```
4. Reference `Core` (minimum dependency):
   ```xml
   <ProjectReference Include="..\OpenVEPA.Core\OpenVEPA.Core.csproj" />
   ```
5. `Directory.Build.props` automatically applies shared settings (target framework, nullable, etc.)

### Key Files to Know

| File | What It Does |
|---|---|
| `src/OpenVEPA.Core/Skills/ISkill.cs` | The most important interface — defines the skill contract |
| `src/OpenVEPA.Agents.Runtime/AssistantAgent.cs` | The heart of the assistant — LLM tool-calling loop |
| `src/OpenVEPA.Storage/DatabaseWriteQueue.cs` | Single-writer SQLite pattern |
| `src/OpenVEPA.Cli/Program.cs` | Composition root — all DI wiring happens here |
| `docs/project_requirements.md` | Full requirements specification (~1,500 lines) |

---

## Troubleshooting

### "OpenAI provider requested but no API key is configured"

Set the API key via environment variable or `appsettings.json`:

```bash
export Providers__OpenAi__ApiKey=sk-...
```

Or switch to Ollama (free, local):

```bash
ollama pull llama3.2
# Then set DefaultProvider to "ollama" in appsettings.json (this is the default)
```

### "Could not find a part of the path '~/.openvepa/data/'"

The directory is created automatically on first run. If it fails, ensure you have write permissions to your home directory, or set `OPENVEPA_HOME` to a writable location.

### Build warnings or errors

The project uses `TreatWarningsAsErrors`. If you see build failures:

1. Ensure you're on .NET 10 SDK: `dotnet --version` should show `10.x.x`
2. Run `dotnet restore` before building
3. Check that no nullable reference warnings are present

### Tests fail with SQLite errors

Storage tests use in-memory SQLite. If they fail, ensure `Microsoft.EntityFrameworkCore.Sqlite` is properly restored:

```bash
dotnet restore
dotnet test tests/OpenVEPA.Storage.Tests
```

---

## What's Next

OpenVEPA is under active development. Current Phase 1 delivers:

- ✅ Core interfaces and type system
- ✅ SQLite storage with single-writer pattern
- ✅ OpenAI + Ollama LLM providers with token tracking
- ✅ SKILL.md parser and native skill loader
- ✅ Agent runtime with tool-calling loop
- ✅ SignalR server with token authentication
- ✅ Hangfire scheduler for cron jobs
- ✅ Spectre.Console CLI with 11 commands
- ✅ 113 unit tests

**Coming in Phase 1.5:**
- Task resumability and checkpointing
- Implicit user preference learning
- Vector search (sqlite-vec)
- Hub client for marketplace
- Discord and email channel adapters
- LLM budget enforcement

See `docs/project_requirements.md` for the full roadmap.

---

## License

MIT — see [LICENSE](../LICENSE) for details.
