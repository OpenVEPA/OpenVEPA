# OpenVEPA - Project Overview

## What is OpenVEPA?
OpenVEPA is a free, open-source personal AI assistant designed to be installable on any platform and OS.

## Tech Stack
- **Primary Language**: CSharp
- **License**: See LICENSE file in project root

## Project Status
The project is actively developed with a full C# solution containing:
- **OpenVEPA.Core**: Domain records (AgentDefinition, AgentLlmConfig, AgentLlmRequirements, etc.)
- **OpenVEPA.Providers**: LLM provider factories (Ollama, OpenAI, Google), AgentChatClientFactory, token tracking
- **OpenVEPA.Agents.Runtime**: Agent YAML/MD parser, agent execution
- **OpenVEPA.Server**: REST API endpoints, DashboardPageHtml (WebUI), setup wizard
- **OpenVEPA.Storage**: SQLite audit logging

## Architecture
- 10 supported providers: Ollama, OpenAI, Google, Anthropic, Mistral, Groq, Azure, Cohere, Together, Perplexity
- Most providers use OpenAI-compatible SDK with custom endpoints
- Dual config format: legacy per-provider sections OR multi-instance Instances[] array
- Provider resolution: AgentChatClientFactory with ConcurrentDictionary cache, auto-clear on config change
- Decorator chain: Provider -> TokenTrackingChatClient -> ConfiguredChatClient -> FallbackChatClient
- Runtime config reload via IOptionsMonitor without restart