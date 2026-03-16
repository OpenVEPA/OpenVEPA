# OpenVEPA

**Virtual Executive Personal Assistant** — a free, open-source personal AI assistant that runs locally on your machine.

Built on .NET 10, OpenVEPA connects to LLM providers (OpenAI, Ollama), supports extensible skills and agents, and communicates over WebSockets via SignalR.

## Quick Start

### From Source

```bash
# Prerequisites: .NET 10 SDK, Ollama (recommended)
ollama pull llama3.2

# Clone and build
git clone https://github.com/OpenVEPA/OpenVEPA.git
cd OpenVEPA
dotnet build

# Start the assistant
cd src/OpenVEPA.Cli
dotnet run

# Or start an interactive chat
dotnet run -- chat
```

### Docker

```bash
# Quick start with Docker
docker compose up -d
# Open http://localhost:8371 for setup wizard

# With local Ollama LLM
docker compose --profile with-ollama up -d
docker exec openvepa-ollama ollama pull llama3.2

# Or pre-configure via environment variables
OPENVEPA_PROVIDER=openai OPENVEPA_MODEL=gpt-4o OPENVEPA_API_KEY=sk-... docker compose up -d
```

## Documentation

- **[Getting Started](docs/getting-started.md)** — setup, configuration, CLI commands, project structure
- **[Docker Deployment](docs/docker.md)** — Docker Compose, Ollama, GPU, reverse proxy, security
- **[Project Requirements](docs/project_requirements.md)** — full specification and roadmap

## Features

- 🤖 **Autonomous task completion** — give it a goal and it works toward it
- 🐳 **Docker-first** — one-command deployment with docker-compose
- 🔧 **Extensible skills** — SKILL.md format with native .NET and MCP bridge support
- 🧩 **Configurable agents** — define behavior via `.agent.md` files
- 💬 **Multi-session chat** — parallel conversations from CLI, Telegram, or any SignalR client
- 🔒 **Token-based auth** — PBKDF2-hashed tokens with loopback auto-auth for local use
- ⏰ **Scheduled tasks** — cron jobs powered by Hangfire
- 📊 **Token tracking** — monitor LLM usage across all providers
- 👤 **User preferences** — remembers your preferences for personalized responses

## License

MIT — see [LICENSE](LICENSE) for details.
