# Analysis: LLM Provider Integration for OpenVEPA

## 1. Objective and Scope

**Objective**: Determine the optimal architecture for integrating multiple LLM providers into OpenVEPA, a C#/.NET 10 personal AI assistant.

**Scope**: Provider-level integration research. Covers orchestration frameworks, direct SDKs, abstraction architecture, and integration requirements. Excludes specific model selection within providers (deferred to configuration time).

**Out of scope**: Prompt engineering strategies, fine-tuning, training, pricing comparisons.

---

## 2. Context

OpenVEPA requires multi-provider LLM support to avoid vendor lock-in and give users flexibility. The .NET AI ecosystem has matured rapidly. Three layers now exist in the Microsoft stack:

1. **Microsoft.Extensions.AI** -- low-level abstractions (`IChatClient`, `IEmbeddingGenerator`)
2. **Microsoft Agent Framework** -- high-level orchestration (agents, workflows, multi-agent, tool calling) built on top of M.E.AI
3. **Direct Provider SDKs** -- native libraries from OpenAI, Anthropic, Google, etc.

The relationship: Microsoft Agent Framework consumes `IChatClient` from Microsoft.Extensions.AI under the hood. MAF is the official successor to both Semantic Kernel (now maintenance mode) and AutoGen, merging enterprise-readiness with multi-agent orchestration.

---

## 3. Approach

**Methodology**: Web research of official documentation, NuGet package registries, GitHub repositories, and developer blogs. Cross-referenced multiple sources per finding.

**Tools Used**: Web search, NuGet Gallery, GitHub repositories, Microsoft Learn documentation, provider API documentation.

**Limitations**: NuGet version numbers and API surfaces change frequently. All version data reflects research conducted in February 2026.

---

## 4. Microsoft Agent Framework

> **Deprecation notice**: Semantic Kernel is now in maintenance mode (critical fixes only, no new features). Microsoft Agent Framework is the official successor, replacing both Semantic Kernel and AutoGen.

### Overview

Microsoft Agent Framework (MAF) is Microsoft's open-source agent orchestration framework. It merges Semantic Kernel's enterprise-readiness with AutoGen's multi-agent orchestration into a single unified platform. MAF builds on top of `IChatClient` from Microsoft.Extensions.AI.

- **Status**: Release Candidate / approaching Stable (as of February 2026)
- **Languages**: .NET and Python
- **License**: Open-source (MIT)
- **Replaces**: Semantic Kernel (orchestration layer) + AutoGen (multi-agent)

### Core NuGet Packages

| Package | Purpose |
|---------|---------|
| `Microsoft.Agents.AI` | Core agent orchestration framework |
| `Microsoft.Agents.AI.Abstractions` | Base interfaces and agent types |
| `Microsoft.Agents.AI.OpenAI` | OpenAI / Azure OpenAI integration |
| `Microsoft.Agents.AI.AzureAI` | Azure AI integration |
| `Microsoft.Agents.AI.CopilotStudio` | Copilot Studio integration |
| `Microsoft.Agents.AI.A2A` | Agent-to-agent protocol support |

### Key Capabilities

- **Type-safe function/tool calling** -- declare tools as C# methods, MAF handles serialization and routing
- **Graph-based workflows** -- sequential, concurrent, and human-in-the-loop execution patterns
- **Multi-provider support** -- Azure OpenAI, OpenAI, Copilot, Claude, Bedrock, Ollama
- **Built-in MCP support** -- via `ModelContextProtocol` + `McpDotNet.Extensions.AI` packages
- **Workflow-centric debugging** -- observability and diagnostics built into the framework
- **Built-in middleware system** -- intercept and extend agent behavior at any point
- **A2A protocol** -- agent-to-agent communication for distributed multi-agent systems
- **IChatClient foundation** -- consumes `IChatClient` from M.E.AI as its provider abstraction

### Relationship to Microsoft.Extensions.AI

MAF and M.E.AI serve different layers of the stack:

| Layer | Component | Role |
|-------|-----------|------|
| Foundation | `IChatClient` (M.E.AI) | Provider abstraction -- talk to any LLM |
| Orchestration | Microsoft Agent Framework | Agent workflows, multi-agent, tool management |

M.E.AI is still the foundation. MAF consumes `IChatClient` implementations. Provider SDKs implement `IChatClient`. MAF adds agent orchestration, workflow graphs, multi-agent coordination, and tool calling management on top.

### MCP Integration

MAF supports Model Context Protocol (MCP) natively through:
- `ModelContextProtocol` NuGet package
- `McpDotNet.Extensions.AI` bridge package

This allows MAF agents to consume MCP servers as tool providers, enabling integration with external tool ecosystems.

### Legacy: Semantic Kernel Reference

Semantic Kernel (SK) was Microsoft's previous AI orchestration SDK (version 1.72.0 at time of deprecation). Key packages included `Microsoft.SemanticKernel.Connectors.OpenAI`, `Microsoft.SemanticKernel.Connectors.AzureOpenAI`, and preview connectors for Anthropic, Google, Ollama, and Mistral. SK used `IChatCompletionService`, `KernelFunction`/`KernelPlugin`, and `IMemoryStore` as its core abstractions. These patterns are superseded by MAF's agent-centric model. Existing SK code can migrate incrementally since both frameworks build on `IChatClient`.

---

## 5. Microsoft.Extensions.AI

### Overview

A foundational abstraction layer providing `IChatClient` and `IEmbeddingGenerator<T>` interfaces. This is the "HTTP client for AI" in .NET -- a thin, composable layer that all higher-level frameworks (including SK) build upon.

- **Current version**: 10.3.0
- **Target framework**: .NET 8.0, .NET Standard 2.0, .NET Framework 4.6.2+
- **Status**: Stable, actively maintained by Microsoft
- **Package**: `Microsoft.Extensions.AI.Abstractions`

### Why This Matters

Microsoft.Extensions.AI is the official standardization direction for .NET AI workloads. Providers implementing `IChatClient` become interchangeable. Microsoft Agent Framework builds on these interfaces as its provider abstraction layer.

**Key insight**: This is an Option D that was not initially listed. OpenVEPA can build on `IChatClient` directly for lightweight scenarios and layer Microsoft Agent Framework on top when orchestration features are needed.

### API Stability Warning

Breaking changes occurred in early 2025 (e.g., `CompleteAsync` renamed to `GetResponseAsync`). The API is stabilizing but not frozen. Pin versions and test on upgrade.

---

## 6. Direct Provider SDKs

### 6.1 Anthropic (Claude)

| Attribute | Detail |
|-----------|--------|
| **Official SDK** | `Anthropic` NuGet package (by Anthropic) |
| **Version** | 12.8.0 |
| **Framework** | .NET Standard 2.0+ (.NET 6/8/10 compatible) |
| **Status** | Official, beta label but production-ready |
| **GitHub** | github.com/anthropics/anthropic-sdk-csharp |

**Capabilities**:
- Chat completions (Messages API)
- Streaming responses
- Tool/function calling
- Batch processing
- Document operations and citations
- Token counting and cost calculations
- Vision (image input in messages)

**Community alternatives**: `Anthropic.SDK` by tghamm (unofficial, version 5.x). Use the official `Anthropic` package for new projects.

**M.E.AI integration**: The official SDK implements `IChatClient` from Microsoft.Extensions.AI.

**Model families**: Claude Opus, Sonnet, Haiku (exact versions selected at configuration time).

### 6.2 OpenAI

| Attribute | Detail |
|-----------|--------|
| **Official SDK** | `OpenAI` NuGet package (by OpenAI, Microsoft co-maintained) |
| **Version** | 2.8.0 |
| **Framework** | .NET Standard 2.0+ |
| **Status** | Stable, production-ready |
| **GitHub** | github.com/openai/openai-dotnet |
| **Azure variant** | `Azure.AI.OpenAI` (adds Azure auth, content filters, On Your Data) |

**Capabilities**:
- Chat completions
- Streaming via `IAsyncEnumerable<T>`
- Function/tool calling with JSON schema bindings
- Structured outputs
- Vision (image input)
- Embeddings generation
- Audio (speech-to-text, text-to-speech)
- Image generation (DALL-E)
- Assistants API (threads, retrieval, tools)
- Batch processing, moderation, fine-tuning

**Azure OpenAI**: Same SDK pattern with `Azure.AI.OpenAI` wrapper. Swap endpoint and credentials to switch between OpenAI and Azure OpenAI. Azure adds Entra ID auth, content filters, region control, and enterprise compliance.

**M.E.AI integration**: First-class `IChatClient` implementation via `Microsoft.Extensions.AI.OpenAI`.

**Model families**: GPT-4o, GPT-4, o1/o3 reasoning models (exact versions selected at configuration time).

### 6.3 Google Gemini

| Attribute | Detail |
|-----------|--------|
| **Official SDK** | `Google.GenAI` NuGet package (by Google) |
| **Framework** | .NET Standard 2.0+ |
| **Status** | Active, official |
| **Endpoints** | Google AI Studio (API key) or Vertex AI (project-based) |

**Capabilities**:
- Chat completions
- Streaming responses
- Function/tool calling
- Multimodal input (text, images, video, audio)
- System instructions
- Structured output with schema support

**Two deployment paths**:
1. **Google AI Studio** -- API key auth, simpler setup, developer-focused
2. **Vertex AI** -- GCP project auth, enterprise features, IAM integration

Both use the same `Google.GenAI` SDK. Switch via constructor parameter:

```csharp
// AI Studio
var client = new Client(apiKey: "KEY");
// Vertex AI
var client = new Client(project: "project-id", location: "us-central1", vertexAI: true);
```

**M.E.AI integration**: `Google.Cloud.VertexAI.Extensions` adapts Gemini for `IChatClient`.

**Community alternatives**: `Google_GenerativeAI` (feature-rich, .NET Standard 2.0+), `Mscc.GenerativeAI`.

**Model families**: Gemini Pro, Flash, Ultra (exact versions selected at configuration time).

### 6.4 Ollama (Local Models)

| Attribute | Detail |
|-----------|--------|
| **Client library** | `OllamaSharp` NuGet package |
| **Version** | 5.4.16 |
| **Framework** | .NET Standard 2.0, .NET 8+ |
| **Status** | Mature, recommended by Microsoft |
| **GitHub** | github.com/awaescher/OllamaSharp |

**Capabilities**:
- Chat completions
- Streaming responses
- Function/tool calling (via source generators)
- Embedding generation
- Model management (pull, list, create, delete)
- Progress reporting during model downloads
- Native AOT compatible

**Local deployment considerations**:
- Ollama runs as a background service on `http://localhost:11434`
- 7B models require approximately 8GB RAM
- 13B models require approximately 16GB RAM
- GPU acceleration supported (NVIDIA CUDA, AMD ROCm, Apple Metal)
- Docker deployment supported for containerized setups
- No internet required after model download

**M.E.AI integration**: OllamaSharp implements `IChatClient` and integrates with Microsoft Agent Framework and .NET Aspire.

**Model families**: Llama, Mistral, Phi, Qwen, Gemma, CodeLlama, DeepSeek, and hundreds of community models. Model selection at configuration time.

### 6.5 GitHub Models

| Attribute | Detail |
|-----------|--------|
| **SDK** | `Azure.AI.Inference` NuGet package |
| **Auth** | GitHub PAT with `models` scope |
| **Status** | GA, actively maintained |

**Overview**: GitHub Models provides inference access to models from multiple providers through a single API. Available models include OpenAI GPT-4o, Microsoft Phi-3/Phi-4, Mistral Large, Meta Llama 3.1, and DeepSeek models.

**Integration approaches**:
1. **Azure AI Inference SDK** (`Azure.AI.Inference`) -- direct REST client
2. **.NET Aspire integration** -- declarative resource model with `GitHubModel.OpenAI.OpenAIGpt4oMini` constants
3. **REST API** -- direct HTTP calls to `https://models.github.ai`

**Capabilities**:
- Chat completions
- Streaming responses
- Tool calling (model-dependent)
- Playground for interactive testing before coding

**Strategic value**: Free tier for prototyping. Same models available on Azure for production scale. Useful for development and testing without Azure subscription.

---

## 7. Abstraction Architecture Options

### Option A: Microsoft Agent Framework as the Abstraction

Use MAF's agent system and workflow orchestration as the sole abstraction layer.

```
OpenVEPA --> Microsoft Agent Framework --> Provider Integrations --> LLM APIs
```

| Aspect | Assessment |
|--------|-----------|
| Provider coverage | Azure OpenAI, OpenAI, Copilot, Claude, Bedrock, Ollama |
| Function calling | Built-in type-safe tool calling |
| Workflows | Graph-based (sequential, concurrent, human-in-the-loop) |
| Streaming | Supported through IChatClient |
| Implementation effort | Low -- MAF handles provider differences and orchestration |
| Flexibility | Medium -- tied to MAF patterns |
| Dependency weight | Medium -- MAF dependency tree |
| Risk | Low-Medium -- MAF is RC/approaching Stable, backed by Microsoft |

### Option B: Custom Thin Abstraction

Define a custom `ILlmProvider` interface. Implement per-provider using their direct SDKs.

```
OpenVEPA --> ILlmProvider --> Direct SDKs --> LLM APIs
```

| Aspect | Assessment |
|--------|-----------|
| Provider coverage | Manual per provider |
| Function calling | Manual implementation per provider |
| Streaming | Manual per provider |
| Implementation effort | High -- 5+ provider implementations |
| Flexibility | High -- full control over every detail |
| Dependency weight | Light per provider |
| Risk | High -- maintaining parity across providers is labor-intensive |

### Option C: Hybrid (MAF + Direct SDKs)

Own `ILlmProvider` interface. MAF as one implementation. Direct SDKs for providers where MAF integrations are insufficient.

```
OpenVEPA --> ILlmProvider --> MAF Implementation --> MAF Providers --> LLM APIs
                          \-> Direct SDK Impl --> Provider SDKs --> LLM APIs
```

| Aspect | Assessment |
|--------|-----------|
| Provider coverage | Full -- MAF where it works, direct where it does not |
| Flexibility | High -- escape hatch from MAF when needed |
| Implementation effort | Medium-High -- two integration paths to maintain |
| Dependency weight | Medium -- MAF optional, not mandatory |
| Risk | Medium -- interface must accommodate both patterns |

### Option D: Microsoft.Extensions.AI as Foundation (Recommended)

Build on `IChatClient` from Microsoft.Extensions.AI. Layer Microsoft Agent Framework on top for orchestration features. Use direct SDKs that implement `IChatClient` natively.

```
OpenVEPA --> IChatClient (M.E.AI) --> Provider SDKs (all implementing IChatClient)
         \-> Microsoft Agent Framework (for agents, workflows, multi-agent) --> same IChatClient
```

| Aspect | Assessment |
|--------|-----------|
| Provider coverage | All major providers implement IChatClient |
| Function calling | Via IChatClient + MAF type-safe tool calling when needed |
| Streaming | Built into IChatClient contract |
| Implementation effort | Low-Medium -- providers already implement the interface |
| Flexibility | High -- swap providers via DI, add MAF when needed |
| Dependency weight | Light base, MAF optional add-on |
| Risk | Low -- Microsoft's official standardization direction |

**Why Option D is superior**:

1. `IChatClient` is the convergence point. OpenAI SDK, Anthropic SDK, OllamaSharp, and Google extensions all implement it natively.
2. Microsoft Agent Framework builds on M.E.AI internally. Using M.E.AI as the base means MAF is additive, not foundational.
3. OpenVEPA can start lightweight (direct `IChatClient` calls) and add MAF orchestration incrementally.
4. Provider swap is a DI registration change, not a code change.
5. This is Microsoft's stated direction for the .NET AI ecosystem.

---

## 8. Provider SDK Compatibility Matrix

| Capability | OpenAI | Anthropic | Google Gemini | Ollama | GitHub Models |
|-----------|--------|-----------|---------------|--------|---------------|
| Official .NET SDK | Yes | Yes | Yes | Community (OllamaSharp) | Yes (Azure.AI.Inference) |
| IChatClient support | Yes | Yes | Via extension | Yes | Yes |
| MAF integration | Yes (via OpenAI package) | Yes (via IChatClient) | Yes (via IChatClient) | Yes (via IChatClient) | Yes (via OpenAI compat) |
| Streaming | Yes | Yes | Yes | Yes | Yes |
| Tool/function calling | Yes | Yes | Yes | Yes (model-dependent) | Yes (model-dependent) |
| Vision/multimodal | Yes | Yes | Yes | Model-dependent | Model-dependent |
| Embeddings | Yes | No (use other providers) | Yes | Yes | Model-dependent |
| .NET Standard 2.0 | Yes | Yes | Yes | Yes | Yes |

---

## 9. Key Integration Requirements Assessment

### 9.1 Streaming Responses

**Status**: [PASS] All 5 providers support streaming via `IAsyncEnumerable<T>` or equivalent.

`IChatClient` defines `GetStreamingResponseAsync()` as a first-class method. All evaluated SDKs implement it.

### 9.2 Tool / Function Calling

**Status**: [PASS] All cloud providers support tool calling natively. Ollama support depends on the model.

SK's plugin system provides the highest-level abstraction. `IChatClient` supports tool definitions at the protocol level. MAF provides type-safe tool calling with graph-based workflow orchestration. All three paths work; MAF is the recommended approach for complex scenarios.

### 9.3 Vision / Multimodal Support

**Status**: [PASS] OpenAI, Anthropic, and Gemini support image input. Ollama support is model-dependent. GitHub Models depends on underlying model.

### 9.4 Embedding Generation

**Status**: [WARNING] Anthropic does not offer an embeddings API. Use OpenAI, Gemini, or Ollama for embedding generation.

`IEmbeddingGenerator<T>` from M.E.AI abstracts this. The embedding provider need not be the same as the chat provider.

### 9.5 Cost Tracking Hooks

**Status**: [WARNING] No provider SDK provides built-in cost tracking. Token usage is returned in responses.

**Approach**: Implement a middleware/decorator on `IChatClient` that intercepts responses, extracts token counts, and logs to a cost tracking service. M.E.AI's composable middleware pattern supports this cleanly.

### 9.6 Retry / Fallback Between Providers

**Status**: [PASS] Achievable via middleware pattern.

**Approach**: Implement a `FallbackChatClient` that wraps multiple `IChatClient` instances. On failure, try the next provider. M.E.AI's DelegatingChatClient pattern supports this. Polly integration available for retry policies.

### 9.7 Model Selection at Configuration Time

**Status**: [PASS] All SDKs accept model identifiers as string parameters at runtime.

**Approach**: Store provider + model pairs in configuration (`appsettings.json`, user settings, or database). Resolve at startup or per-request via DI.

---

## 10. Recommendation

### Recommended Architecture: Option D -- Microsoft.Extensions.AI Foundation

Build OpenVEPA's LLM layer on `IChatClient` from Microsoft.Extensions.AI.

**Layer 1 -- Abstraction**: `IChatClient` / `IEmbeddingGenerator<T>` from `Microsoft.Extensions.AI.Abstractions`. This is the contract all provider code programs against.

**Layer 2 -- Providers**: Register provider SDKs that implement `IChatClient` via DI. Swap providers by changing configuration, not code.

**Layer 3 -- Orchestration (when needed)**: Add Microsoft Agent Framework for agent orchestration, graph-based workflows, multi-agent coordination, and tool calling management. MAF consumes `IChatClient` implementations, so it layers on top without replacing the foundation. MAF also provides built-in MCP support via `ModelContextProtocol` + `McpDotNet.Extensions.AI`.

> **Note**: Semantic Kernel previously occupied this layer. SK is now in maintenance mode (critical fixes only). Microsoft Agent Framework is the official successor, merging SK's enterprise patterns with AutoGen's multi-agent orchestration.

### Conceptual Architecture

```
+--------------------------------------------------+
|                   OpenVEPA                        |
|                                                   |
|  +--------------------------------------------+  |
|  |         Application Logic                   |  |
|  +--------------------------------------------+  |
|       |                    |                      |
|       v                    v                      |
|  +----------+    +-------------------+            |
|  | IChatClient|  | Microsoft Agent   |            |
|  | (M.E.AI)  |  | Framework (Agents,|            |
|  |           |  | Workflows, A2A)   |            |
|  +----------+    +-------------------+            |
|       |                    |                      |
|       v                    v                      |
|  +--------------------------------------------+  |
|  |         DI-registered Providers             |  |
|  |  +--------+ +--------+ +--------+ +------+ |  |
|  |  | OpenAI | |Anthropic| | Gemini | |Ollama| |  |
|  |  +--------+ +--------+ +--------+ +------+ |  |
|  +--------------------------------------------+  |
+--------------------------------------------------+
```

### Provider Priority for Phase 1

| Priority | Provider | Rationale |
|----------|----------|-----------|
| P0 | OpenAI | Most mature .NET SDK. First-class MAF integration. Broadest model range. Azure OpenAI as enterprise option. |
| P0 | Ollama | Local/offline capability is essential for a personal assistant. Privacy-sensitive users need this. Zero API cost. |
| P1 | Anthropic | Official .NET SDK is production-ready. Claude models excel at reasoning and long context. |
| P1 | Google Gemini | Official SDK available. Strong multimodal capabilities. Competitive pricing. |
| P2 | GitHub Models | Useful for development/testing. Free tier prototyping. Lower priority for production. |

### SDK Selection per Provider

| Provider | Recommended SDK | Reason |
|----------|----------------|--------|
| OpenAI | `OpenAI` NuGet + `Microsoft.Extensions.AI.OpenAI` | Official, stable, IChatClient native |
| Azure OpenAI | `Azure.AI.OpenAI` + `Microsoft.Agents.AI.OpenAI` | Same as OpenAI with Azure-specific features + MAF integration |
| Anthropic | `Anthropic` NuGet (official) | Official SDK, IChatClient support |
| Google Gemini | `Google.GenAI` + `Google.Cloud.VertexAI.Extensions` | Official SDK with M.E.AI adapter |
| Ollama | `OllamaSharp` | Mature, IChatClient support, SK compatible |
| GitHub Models | `Azure.AI.Inference` | Official, same pattern as Azure AI |

### Do Not Use Legacy Semantic Kernel Connectors as Primary Integration

Use direct SDKs implementing `IChatClient` instead. Reasons:

1. Semantic Kernel is now in maintenance mode. No new features, critical fixes only.
2. Microsoft Agent Framework is the official successor for orchestration needs.
3. Direct SDKs give access to provider-specific features that abstractions may not expose.
4. Direct SDKs update faster when providers ship new capabilities.
5. MAF consumes `IChatClient`, so it benefits from direct SDK quality without requiring SK connectors.

Reserve Microsoft Agent Framework for what it does best: agent orchestration, graph-based workflows, multi-agent coordination, and tool calling management. Let the SDKs handle the transport.

---

## 11. Conclusion

**Verdict**: Proceed with Option D architecture.

**Confidence**: High.

**Rationale**: Microsoft.Extensions.AI is the official convergence point for .NET AI integrations. All major provider SDKs implement its interfaces. Microsoft Agent Framework layers on top for orchestration without lock-in. This architecture gives OpenVEPA maximum flexibility with minimum coupling.

### User Impact

- **What changes for you**: Provider selection becomes a configuration choice, not a code change. Adding a new provider means registering one NuGet package and one DI binding.
- **Effort required**: Medium. Define the DI registration patterns, implement middleware (cost tracking, fallback, logging), and configure providers.
- **Risk if ignored**: Building on a custom or SK-only abstraction increases migration cost. Semantic Kernel is now in maintenance mode. New investment should target Microsoft Agent Framework as the orchestration layer.

---

## 12. Appendices

### Sources Consulted

- Microsoft Agent Framework: github.com/microsoft/agents
- Microsoft Semantic Kernel NuGet profile (legacy): nuget.org/profiles/SemanticKernel
- Semantic Kernel GitHub releases (legacy, maintenance mode): github.com/microsoft/semantic-kernel/releases
- Microsoft Learn -- Semantic Kernel (legacy): learn.microsoft.com/en-us/semantic-kernel/
- Anthropic C# SDK: github.com/anthropics/anthropic-sdk-csharp
- Anthropic NuGet: nuget.org/packages/Anthropic/
- OpenAI .NET SDK: github.com/openai/openai-dotnet
- OpenAI NuGet: nuget.org/packages/OpenAI/
- Azure.AI.OpenAI docs: azuresdkdocs.z19.web.core.windows.net/dotnet/Azure.AI.OpenAI/
- Google GenAI .NET SDK: googleapis.github.io/dotnet-genai/
- Google Cloud blog on .NET SDK: cloud.google.com/blog/topics/developers-practitioners/introducing-google-gen-ai-net-sdk
- OllamaSharp: github.com/awaescher/OllamaSharp
- OllamaSharp NuGet: nuget.org/packages/OllamaSharp
- GitHub Models docs: docs.github.com/en/github-models/quickstart
- Azure AI Inference SDK: devblogs.microsoft.com/dotnet/azure-ai-model-catalog-dotnet-inference-sdk/
- Microsoft.Extensions.AI NuGet: nuget.org/packages/Microsoft.Extensions.AI.Abstractions/
- SK + M.E.AI blog series: devblogs.microsoft.com/semantic-kernel/semantic-kernel-and-microsoft-extensions-ai-better-together-part-1/
- Microsoft Agent Framework announcement: devblogs.microsoft.com/dotnet/introducing-microsoft-agent-framework/

### Data Transparency

- **Found**: SDK versions, framework targets, feature matrices, provider connector statuses, official documentation for all 5 providers, architecture guidance from Microsoft, MAF package inventory and capability matrix
- **Not Found**: Benchmark data comparing MAF overhead vs direct SDK performance. No independent latency measurements for provider integrations. Anthropic embeddings API status unclear (likely does not exist). MAF stable release date not confirmed.

### Version Reference (as of research date)

| Package | Version | TFM |
|---------|---------|-----|
| Microsoft.Agents.AI | RC (approaching Stable) | net8.0+ |
| Microsoft.Agents.AI.Abstractions | RC (approaching Stable) | net8.0+ |
| Microsoft.Agents.AI.OpenAI | RC (approaching Stable) | net8.0+ |
| Microsoft.SemanticKernel (legacy, maintenance mode) | 1.72.0 | net8.0, netstandard2.0 |
| Microsoft.Extensions.AI.Abstractions | 10.3.0 | net8.0, netstandard2.0, net462 |
| OpenAI | 2.8.0 | netstandard2.0 |
| Azure.AI.OpenAI | 2.2.0-beta.5 | netstandard2.0 |
| Anthropic | 12.8.0 | netstandard2.0 |
| Google.GenAI | latest | netstandard2.0 |
| OllamaSharp | 5.4.16 | netstandard2.0, net8.0 |
| Azure.AI.Inference | latest | netstandard2.0 |
