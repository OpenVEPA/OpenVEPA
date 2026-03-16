# Analysis: .NET 10 Technology Stack for OpenVEPA

## 1. Objective and Scope

**Objective**: Evaluate C#/.NET 10 as the implementation platform for OpenVEPA, replacing the original Python-based architecture. Identify framework choices, library recommendations, deployment options, and trade-offs.

**Scope**: .NET 10 platform capabilities, project structure conventions, core framework selections, key library ecosystem, deployment strategies, and trade-offs versus Python for an AI assistant application.

**Out of scope**: Detailed API design, database schema, GUI framework selection, Hub protocol design.

---

## 2. Context

OpenVEPA is a personal AI assistant with a skill-first architecture. One main agent (the Assistant) delegates work to composable skills. Skills and additional agents are distributed through a Hub registry. The system requires:

- CLI and TUI as primary interfaces, web GUI as secondary
- Plugin/skill system with runtime discovery and installation
- Multi-LLM provider support (Anthropic, OpenAI, Google, local via Ollama)
- Concurrent task execution with async patterns
- Cross-platform operation (Windows, Linux, macOS)
- Single-command install experience

The existing Python architecture (v0.1.0, proposed) uses `src` layout, Protocol classes (PEP 544), asyncio, and a layered package structure. This analysis informs the .NET 10 equivalent.

---

## 3. Approach

**Methodology**: Web research for current .NET 10 GA features, library comparisons, and community signal. Cross-referenced with OpenVEPA requirements from `project_requirements.md` and `architecture.md`.

**Tools Used**: Web search (Microsoft docs, dev blogs, community benchmarks, NuGet statistics), project documentation review.

**Limitations**: No hands-on benchmarking performed. Library download counts not independently verified. .NET 10 released November 2025; some ecosystem adoption data is early-stage.

---

## 4. .NET 10 Platform Capabilities

### 4.1 LTS Status and Support

.NET 10 is a **Long-Term Support (LTS)** release. Microsoft supports it until **November 2028** (3 years). LTS releases receive security patches, bug fixes, and servicing updates throughout the support window.

| Property | Value |
|----------|-------|
| Release date | November 2025 |
| Support end | November 2028 |
| Support type | LTS (3-year) |
| Language version | C# 14 |

**Relevance to OpenVEPA**: LTS guarantees stability for a personal tool that users install and run long-term. No forced annual upgrades.

### 4.2 Performance

.NET 10 delivers measurable runtime improvements:

- **JIT compiler**: Better inlining, devirtualization of array interface methods, small array stack allocation
- **Hardware acceleration**: AVX10.2 (Intel), Arm64 SVE support for numerically intensive workloads
- **GC improvements**: Reduced pause times on ARM hardware
- **Native AOT**: Leaner binaries, faster startup, no JIT dependency at runtime

ASP.NET Core inference endpoints handle requests 4-10x faster than Python frameworks in published benchmarks, with better memory efficiency for long-running services.

### 4.3 Cross-Platform Support

| Platform | Architecture | Status |
|----------|-------------|--------|
| Windows | x64, ARM64 | Full support |
| Linux | x64, ARM64 | Full support |
| macOS | x64, ARM64 (Apple Silicon) | Full support |

.NET 10 runs natively on all target platforms. A single codebase compiles to platform-specific binaries. No interpreter or VM installation required for self-contained deployments.

### 4.4 Native AOT Compilation

Ahead-of-Time compilation converts C# to native machine code at build time.

**Benefits for OpenVEPA**:
- Sub-100ms startup (versus seconds for JIT warmup)
- No .NET runtime dependency on target machine
- Smaller binary footprint with trimming

**Constraints**:
- Limited reflection support (affects dynamic plugin loading)
- Serialization requires source generators (System.Text.Json supports this)
- Some DI containers require AOT-compatible configurations

**Recommendation**: Use JIT compilation for the main OpenVEPA process (plugin system needs reflection). Evaluate AOT for specific CLI tools or lightweight utilities.

### 4.5 Single-File Deployment

```xml
<PropertyGroup>
  <PublishSingleFile>true</PublishSingleFile>
  <SelfContained>true</SelfContained>
  <PublishTrimmed>true</PublishTrimmed>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
</PropertyGroup>
```

Produces one executable containing the app, dependencies, and .NET runtime. Users download a single file and run it. No installation wizard needed.

### 4.6 C# 14 Language Features

Relevant new features:
- **Extension blocks and properties**: Cleaner extensibility patterns
- **File-scoped types**: Better encapsulation within single files
- **Improved pattern matching**: More expressive conditional logic

### 4.7 .NET Aspire (Optional)

.NET Aspire is Microsoft's orchestration framework for cloud-native .NET applications. It provides:

- Code-first service composition (replaces Docker Compose for dev)
- Built-in OpenTelemetry (logs, traces, metrics)
- Developer dashboard with real-time service monitoring
- Service discovery and health checks out of the box

**Relevance to OpenVEPA**: Aspire is not required for the core single-process architecture. It becomes relevant if OpenVEPA grows into a multi-service deployment (separate Hub API, background workers, etc.). Consider for Phase 3+ when multi-agent orchestration may benefit from service composition.

**Recommendation**: Do not adopt Aspire initially. Keep it as an upgrade path for distributed deployment scenarios.

---

## 5. Project Structure Conventions

### 5.1 Solution and Project Layout

The standard .NET convention uses a solution file (`.sln`) with multiple project files (`.csproj`). This maps directly to OpenVEPA's modular architecture.

```
OpenVEPA.sln
src/
    OpenVEPA.Core/                    # Core abstractions, interfaces, domain types
        OpenVEPA.Core.csproj
    OpenVEPA.Cli/                     # CLI application entry point
        OpenVEPA.Cli.csproj
        Program.cs
    OpenVEPA.Hub/                     # Hub client, manifest parsing, installation
        OpenVEPA.Hub.csproj
    OpenVEPA.Skills/                  # Built-in skills
        OpenVEPA.Skills.csproj
    OpenVEPA.Providers/               # LLM provider implementations
        OpenVEPA.Providers.csproj
    OpenVEPA.Storage/                 # Storage backends (SQLite, filesystem, vector)
        OpenVEPA.Storage.csproj
    OpenVEPA.Scheduler/               # Task scheduling
        OpenVEPA.Scheduler.csproj
tests/
    OpenVEPA.Core.Tests/
        OpenVEPA.Core.Tests.csproj
    OpenVEPA.Cli.Tests/
        OpenVEPA.Cli.Tests.csproj
    OpenVEPA.Integration.Tests/
        OpenVEPA.Integration.Tests.csproj
docs/
tools/                                # Build scripts, code generators
```

**Dependency direction** (mirrors the Python architecture):
```
Cli --> Core <-- Hub
          ^
          |
   +------+------+
   |      |      |
Providers Storage Scheduler
```

- `Core` depends on nothing except `Microsoft.Extensions.*` abstractions
- All other projects depend on `Core`
- `Providers`, `Storage`, `Scheduler` never depend on each other
- `Skills` depends on `Core` only

### 5.2 Plugin Architecture

Three viable approaches for the skill/plugin system:

| Approach | Mechanism | Isolation | AOT Compatible | Recommendation |
|----------|-----------|-----------|----------------|----------------|
| **AssemblyLoadContext** | Custom ALC per plugin directory | Strong (separate load context) | No (uses reflection) | **Primary choice** |
| **MEF (System.Composition)** | Attribute-based discovery via `[Export]`/`[Import]` | Moderate (shared context by default) | No | Secondary option |
| **Interface scanning** | Manual reflection over plugin DLLs | Weak | No | Simplest fallback |

**Recommended approach**: `AssemblyLoadContext` (ALC) with shared contract interfaces.

```csharp
// Plugin contract (in OpenVEPA.Core)
public interface ISkill
{
    string Name { get; }
    string Description { get; }
    Task<SkillResult> ExecuteAsync(SkillInput input, CancellationToken ct);
}

// Plugin loader
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
        => _resolver = new AssemblyDependencyResolver(pluginPath);

    protected override Assembly? Load(AssemblyName assemblyName)
        => _resolver.ResolveAssemblyToPath(assemblyName) is string path
            ? LoadFromAssemblyPath(path)
            : null;
}
```

Key design decisions:
- `isCollectible: true` enables plugin unloading (hot-swap skills without restart)
- Shared contract assembly (`OpenVEPA.Core`) referenced by both host and plugins
- Each plugin directory gets its own ALC to prevent dependency conflicts
- McMaster.NETCore.Plugins library available as a higher-level wrapper if needed

---

## 6. Core Framework Choices

### 6.1 CLI Framework

| Framework | Type | Strengths | Weaknesses |
|-----------|------|-----------|------------|
| **Spectre.Console.Cli** | Community (Spectre) | Rich TUI (tables, colors, progress bars, prompts), attribute-driven commands, strong docs | Not an official .NET component |
| **System.CommandLine** | Microsoft (official) | Future-proof (.NET core integration), DI/hosting integration, tab completion | Still evolving, no visual output |
| **ConsoleAppFramework** | Community (Cysharp) | Fastest startup, minimal overhead | Smaller ecosystem |

**Recommendation**: **Spectre.Console.Cli** for command parsing + **Spectre.Console** for rendering.

Rationale: OpenVEPA requires both CLI and TUI interfaces. Spectre.Console provides tables, colors, progress bars, selection prompts, and tree rendering natively. The CLI component handles command routing with type-safe, attribute-driven definitions. System.CommandLine can complement for shell tab-completion if needed.

```csharp
// Example command definition
public class StartCommand : AsyncCommand<StartCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-p|--port")]
        [DefaultValue(5000)]
        public int Port { get; init; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        // Start the assistant
    }
}
```

### 6.2 Dependency Injection

**Choice**: `Microsoft.Extensions.DependencyInjection` (built-in).

No third-party container needed. The built-in DI container supports:
- Constructor injection
- Scoped, transient, and singleton lifetimes
- `IServiceCollection` / `IServiceProvider` pattern
- Keyed services (.NET 8+)
- Integration with all `Microsoft.Extensions.*` libraries

```csharp
services.AddSingleton<ISkillRegistry, SkillRegistry>();
services.AddTransient<ILlmProvider, AnthropicProvider>();
services.AddKeyedSingleton<IStorageBackend, SqliteBackend>("sqlite");
```

### 6.3 Configuration

**Choice**: `Microsoft.Extensions.Configuration` with layered sources.

```
Priority (highest to lowest):
1. Command-line arguments
2. Environment variables
3. User secrets (development)
4. appsettings.{Environment}.json
5. appsettings.json
6. Default values in code
```

Maps to strongly-typed options classes via `IOptions<T>`:

```csharp
public class AssistantOptions
{
    public string Name { get; set; } = "VEPA";
    public int AutonomyLevel { get; set; } = 2;
    public int MaxConcurrentTasks { get; set; } = 5;
}

// Registration
services.Configure<AssistantOptions>(config.GetSection("Assistant"));
```

User-specific config stored in `~/.openvepa/config.json` (platform-appropriate path via `Environment.GetFolderPath`).

### 6.4 Logging

**Choice**: `Microsoft.Extensions.Logging` (abstraction) + **Serilog** (implementation).

- `ILogger<T>` injected via DI everywhere
- Serilog provides structured logging with sinks (console, file, seq)
- Zero-config console logging by default; file logging when configured

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/openvepa-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Logging.AddSerilog();
```

### 6.5 Async Patterns

.NET provides first-class async support without the limitations of Python's asyncio:

| Pattern | Use Case in OpenVEPA |
|---------|---------------------|
| `async`/`await` + `Task<T>` | All I/O operations (LLM calls, file access, HTTP) |
| `System.Threading.Channels` | Producer/consumer for task queue (replaces asyncio.Queue) |
| `IAsyncEnumerable<T>` | Streaming LLM responses token-by-token |
| `Task.WhenAll` / `Task.WhenAny` | Concurrent skill execution |
| `CancellationToken` | Cooperative cancellation across all async operations |
| `SemaphoreSlim` | Concurrency limiting (max concurrent tasks) |

```csharp
// Streaming LLM response
public async IAsyncEnumerable<string> StreamResponseAsync(
    string prompt,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    await foreach (var token in _llmClient.StreamAsync(prompt, ct))
    {
        yield return token;
    }
}

// Task queue using Channels
var channel = Channel.CreateBounded<AgentTask>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait
});
```

No GIL limitation. True parallel execution across CPU cores for compute-bound work.

---

## 7. Key Libraries Ecosystem

### 7.1 HTTP Clients

| Library | Approach | Recommendation |
|---------|----------|----------------|
| **HttpClient + IHttpClientFactory** | Built-in, full control, manual serialization | Use for custom/complex APIs |
| **Refit** | Interface-based, auto-generated clients | **Primary choice** for Hub API and LLM REST APIs |

Refit reduces boilerplate for well-defined REST APIs:

```csharp
public interface IHubApi
{
    [Get("/skills/search")]
    Task<SearchResult> SearchSkillsAsync([Query] string query, CancellationToken ct);

    [Get("/skills/{id}/manifest")]
    Task<SkillManifest> GetManifestAsync(string id, CancellationToken ct);
}

// Registration
services.AddRefitClient<IHubApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://hub.openvepa.dev"));
```

Use raw `HttpClient` with `IHttpClientFactory` for LLM provider SDKs that have their own HTTP handling.

### 7.2 JSON Handling

**Primary**: `System.Text.Json` (built-in).

| Aspect | System.Text.Json | Newtonsoft.Json |
|--------|-----------------|-----------------|
| Performance | 2-3x faster serialization | Baseline |
| Memory | 40-60% less allocation | Higher allocation |
| AOT/trimming | Full support via source generators | Not compatible |
| Features | Covers 90%+ of scenarios in .NET 10 | Richer (JSONPath, LINQ-to-JSON) |
| Dependency | Built-in (zero dependency) | External NuGet package |

```csharp
// Source generator for AOT compatibility
[JsonSerializable(typeof(SkillManifest))]
[JsonSerializable(typeof(AgentConfig))]
internal partial class OpenVepaJsonContext : JsonSerializerContext { }
```

**Recommendation**: Use `System.Text.Json` exclusively. The .NET 10 version closes most feature gaps. Avoid Newtonsoft.Json dependency unless a specific third-party library requires it.

### 7.3 Testing

**Recommendation**: **xUnit** + **NSubstitute** + **FluentAssertions**.

| Component | Choice | Rationale |
|-----------|--------|-----------|
| Test framework | xUnit | Modern, parallel by default, DI-friendly, constructor injection for setup |
| Mocking | NSubstitute | Clean syntax, readable tests, refactor-friendly |
| Assertions | FluentAssertions | Expressive `result.Should().Be(expected)` syntax |
| Integration | `Microsoft.AspNetCore.Mvc.Testing` | For web API integration tests (Phase 3+) |

```csharp
public class SkillRegistryTests
{
    private readonly ISkillRegistry _registry;
    private readonly ILogger<SkillRegistry> _logger;

    public SkillRegistryTests()
    {
        _logger = Substitute.For<ILogger<SkillRegistry>>();
        _registry = new SkillRegistry(_logger);
    }

    [Fact]
    public async Task RegisterSkill_WithValidManifest_AddsToRegistry()
    {
        var skill = Substitute.For<ISkill>();
        skill.Name.Returns("web-search");

        await _registry.RegisterAsync(skill);

        var found = await _registry.FindAsync("web-search");
        found.Should().NotBeNull();
        found!.Name.Should().Be("web-search");
    }
}
```

### 7.4 AI/LLM Integration

Two complementary libraries from Microsoft:

| Library | Level | Use Case |
|---------|-------|----------|
| **Microsoft.Extensions.AI** | Low-level abstraction | `IChatClient`, `IEmbeddingGenerator` interfaces, middleware pipeline, provider-agnostic |
| **Microsoft.SemanticKernel** | High-level orchestration | Agent workflows, planning, plugins, prompt templates, RAG |

**Recommendation**: Use `Microsoft.Extensions.AI` as the foundation layer. It aligns with the OpenVEPA architecture of provider-agnostic `ILlmProvider` interfaces. Evaluate Semantic Kernel for multi-agent orchestration in Phase 3.

```csharp
// Provider-agnostic LLM access
public interface ILlmProvider
{
    IChatClient CreateChatClient();
    IEmbeddingGenerator<string, Embedding<float>>? CreateEmbeddingGenerator();
}

// Anthropic implementation
public class AnthropicProvider : ILlmProvider
{
    public IChatClient CreateChatClient()
        => new AnthropicChatClient(_apiKey, "claude-sonnet-4-20250514");
}

// Registration with DI
services.AddChatClient(builder => builder
    .UseDistributedCache(redis)
    .UseOpenTelemetry("openvepa-llm")
    .Use(_provider.CreateChatClient()));
```

---

## 8. Deployment Options

### 8.1 Primary: NuGet Global Tool

```bash
dotnet tool install -g openvepa
openvepa start
```

| Property | Detail |
|----------|--------|
| Prerequisites | .NET 10 SDK or runtime installed |
| Install location | `~/.dotnet/tools/` |
| Update | `dotnet tool update -g openvepa` |
| Uninstall | `dotnet tool uninstall -g openvepa` |
| Cross-platform | Windows, Linux, macOS |

This mirrors the Python experience of `pip install openvepa && openvepa start`. .NET 10 adds support for self-contained and AOT-compiled tool packages on NuGet, eliminating the runtime dependency.

### 8.2 Secondary: Single-File Binary

```bash
# Build for target platform
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true

# Result: single executable, no dependencies
./openvepa start
```

Publish matrix:

| Target | RID | Binary |
|--------|-----|--------|
| Windows x64 | `win-x64` | `openvepa.exe` |
| Windows ARM64 | `win-arm64` | `openvepa.exe` |
| Linux x64 | `linux-x64` | `openvepa` |
| Linux ARM64 | `linux-arm64` | `openvepa` |
| macOS x64 | `osx-x64` | `openvepa` |
| macOS ARM64 | `osx-arm64` | `openvepa` |

Distribute via GitHub Releases. Users download the binary for their platform.

### 8.3 Tertiary: Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
COPY publish/ /app/
ENTRYPOINT ["/app/openvepa"]
```

Docker is optional. Useful for server deployments or users who prefer containerized tools.

### 8.4 Comparison with Python Distribution

| Aspect | Python (`pip install`) | .NET (`dotnet tool install`) | .NET (single-file) |
|--------|----------------------|------------------------------|---------------------|
| Runtime required | Python 3.x | .NET 10 runtime | None |
| Install command | `pip install openvepa` | `dotnet tool install -g openvepa` | Download + run |
| Update | `pip install --upgrade` | `dotnet tool update -g` | Re-download |
| Dependency conflicts | Common (venv recommended) | Isolated by design | Zero dependencies |
| Binary size | N/A (interpreted) | ~5-15 MB (framework-dependent) | ~30-80 MB (self-contained + trimmed) |
| Startup time | 500ms-2s | 100-500ms (JIT) | <100ms (AOT) |

---

## 9. Trade-offs: C#/.NET 10 vs Python for AI Assistant

### 9.1 Advantages of C#/.NET 10

| Advantage | Detail | Impact |
|-----------|--------|--------|
| **Performance** | 4-10x faster HTTP handling, true multi-threading (no GIL), lower memory | Faster LLM response processing, more concurrent tasks per instance |
| **Type safety** | Compile-time error detection, refactoring confidence, IDE support | Fewer runtime bugs, safer plugin system |
| **Single binary** | Self-contained executable, no runtime dependency | Simpler distribution, no "works on my machine" |
| **Native async** | First-class `async`/`await`, Channels, `IAsyncEnumerable` | Natural fit for streaming LLM responses and concurrent tasks |
| **Mature DI ecosystem** | `Microsoft.Extensions.*` is battle-tested and standardized | Plugin system, configuration, logging all follow one pattern |
| **Microsoft AI investment** | `Microsoft.Extensions.AI`, Semantic Kernel, Azure AI integration | First-class LLM tooling from the platform vendor |
| **Deployment** | `dotnet tool install`, single-file publish, Docker | Multiple distribution channels, all cross-platform |

### 9.2 Disadvantages of C#/.NET 10

| Disadvantage | Detail | Mitigation |
|--------------|--------|------------|
| **Smaller AI library ecosystem** | Python has more AI/ML libraries (HuggingFace, LangChain, spaCy, etc.) | OpenVEPA consumes LLM APIs, not training models. `Microsoft.Extensions.AI` covers the primary use case. |
| **LLM SDK availability** | Some providers release Python SDKs first, C# SDKs follow weeks/months later | All major providers (OpenAI, Anthropic, Google) have official or community .NET SDKs. REST API fallback is always available. |
| **Community size for AI** | Python dominates AI discussion forums, tutorials, and examples | .NET AI community is growing fast. Microsoft's investment in Semantic Kernel and Extensions.AI drives adoption. |
| **Prototyping speed** | C# requires more upfront structure than Python | Compensated by fewer runtime errors and easier refactoring |
| **Learning curve** | Contributors familiar with Python may need onboarding | C# syntax is approachable for Python developers. Strong IDE tooling reduces friction. |

### 9.3 Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| LLM provider drops C# SDK support | Low | Medium | REST API wrapper; providers are expanding .NET support, not contracting |
| Plugin system incompatible with AOT | Medium | Low | Use JIT for main process; AOT only for standalone tools |
| Community contribution barrier | Medium | Medium | Good documentation, familiar patterns (DI, async/await), strong IDE support |
| .NET 10 EOL before OpenVEPA matures | Very Low | Low | 3-year LTS window; .NET 12 (next LTS) will be available by 2027 |

---

## 10. Recommendations

| Priority | Recommendation | Rationale | Effort |
|----------|----------------|-----------|--------|
| P0 | Target .NET 10 (LTS) with C# 14 | 3-year support, best performance, latest language features | Foundational |
| P0 | Use `Microsoft.Extensions.DependencyInjection` for DI | Built-in, zero-dependency, ecosystem standard | Low |
| P0 | Use `Microsoft.Extensions.Configuration` for config | Layered config (JSON, env vars, CLI args, user secrets) | Low |
| P0 | Use `Microsoft.Extensions.Logging` + Serilog | Structured logging, multiple sinks, DI integration | Low |
| P0 | Use `System.Text.Json` for JSON | Built-in, fastest, AOT-compatible | Low |
| P1 | Use Spectre.Console.Cli for CLI + TUI | Rich terminal UI, attribute-driven commands, active community | Medium |
| P1 | Use `AssemblyLoadContext` for plugin system | Isolation, unloading, version independence per plugin | Medium |
| P1 | Use xUnit + NSubstitute for testing | Modern, parallel, readable mocks | Low |
| P1 | Use `Microsoft.Extensions.AI` for LLM abstraction | Provider-agnostic, Microsoft-backed, DI-integrated | Medium |
| P1 | Use Refit for typed HTTP clients | Reduces boilerplate for Hub API and REST integrations | Low |
| P2 | Distribute as `dotnet tool` + GitHub Release binaries | Two distribution channels covering different user preferences | Medium |
| P2 | Evaluate Semantic Kernel for Phase 3 multi-agent | Higher-level orchestration when complexity warrants it | Deferred |
| P3 | Evaluate .NET Aspire if multi-service deployment needed | Code-first orchestration for distributed scenarios | Deferred |

---

## 11. Conclusion

**Verdict**: [PROCEED] with C#/.NET 10

**Confidence**: High

**Rationale**: .NET 10 provides a strong match for OpenVEPA's requirements. The platform delivers LTS stability, cross-platform deployment, first-class async patterns, and a maturing AI integration story through `Microsoft.Extensions.AI`. The skill/plugin system maps naturally to .NET's `AssemblyLoadContext` isolation model. The primary risk (smaller AI library ecosystem compared to Python) is mitigated because OpenVEPA consumes LLM APIs rather than training models.

### What Changes for You

- **Install experience**: `dotnet tool install -g openvepa` or download a single binary
- **Performance**: Faster startup, lower memory usage, true parallel task execution
- **Plugin development**: Skills are .NET class libraries implementing `ISkill` interface
- **Effort required**: Full rewrite from Python architecture (no incremental migration path)
- **Risk if deferred**: Python architecture proceeds with GIL limitations, runtime dependency management challenges, and slower execution for concurrent workloads

---

## 12. Appendices

### Sources Consulted

- [Announcing .NET 10 - .NET Blog](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/)
- [.NET 10 Release: What's New (LTS)](https://benjamin-abt.com/blog/2025/11/08/dotnet-10-release/)
- [Microsoft.Extensions.AI NuGet](https://www.nuget.org/packages/Microsoft.Extensions.AI/)
- [Semantic Kernel GitHub](https://github.com/microsoft/semantic-kernel)
- [Semantic Kernel and Microsoft.Extensions.AI: Better Together](https://devblogs.microsoft.com/semantic-kernel/semantic-kernel-and-microsoft-extensions-ai-better-together-part-1/)
- [Create a .NET Core application with plugins (Microsoft Learn)](https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support)
- [McMaster.NETCore.Plugins](https://github.com/natemcmaster/DotNetCorePlugins)
- [Spectre.Console.Cli Documentation](https://spectreconsole.net/cli)
- [System.CommandLine reset and ecosystem](https://github.com/dotnet/command-line-api/issues/2338)
- [Benchmarking System.Text.Json vs Newtonsoft.Json in .NET 10](https://jkrussell.dev/blog/system-text-json-vs-newtonsoft-json-benchmark/)
- [Refit in .NET: Building Robust API Clients](https://www.milanjovanovic.tech/blog/refit-in-dotnet-building-robust-api-clients-in-csharp)
- [Packaging self-contained and native AOT .NET tools for NuGet](https://andrewlock.net/exploring-dotnet-10-preview-features-7-packaging-self-contained-and-native-aot-dotnet-tools-for-nuget/)
- [C# Deserves a Seat at the Enterprise AI Table](https://matthewkruczek.ai/blog/csharp-python-enterprise-ai)
- [Working with LLMs in .NET using Microsoft.Extensions.AI](https://www.milanjovanovic.tech/blog/working-with-llms-in-dotnet-using-microsoft-extensions-ai)

### Data Transparency

- **Found**: .NET 10 LTS confirmation, AOT/single-file deployment capabilities, library benchmarks, SDK availability for all major LLM providers, plugin architecture documentation, CLI framework comparisons
- **Not found**: Independent benchmark data for OpenVEPA-specific workloads, NuGet download trends for Microsoft.Extensions.AI (too new), real-world .NET AI assistant projects at comparable scale for case study comparison
