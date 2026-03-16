# Infrastructure Analysis: OpenVEPA .NET 10 Default Stack

## 1. Objective and Scope

**Objective**: Define a minimal-dependency default infrastructure stack for OpenVEPA on .NET 10 / C# that works out of the box with zero external services, while providing clear upgrade paths to production-grade alternatives.

**Scope**: Covers 8 infrastructure categories: primary database, document/file storage, caching, vector database, task scheduling, message queue/background jobs, and hosting. Each category specifies a default (zero-config) and optional upgrade with migration triggers.

**Constraint**: The previous Python-focused analysis proposed lightweight defaults with optional extensions. This analysis applies the same principle to the .NET ecosystem, leveraging built-in framework abstractions that make swapping implementations a DI registration change rather than a code rewrite.

**Target Runtime**: .NET 10 (November 2025 release), C# 14, ASP.NET Core 10.

---

## 2. Context

OpenVEPA is a personal AI assistant. The primary deployment target is a single user running on their own hardware. The .NET ecosystem provides stronger built-in abstractions than Python for infrastructure swapping. Interfaces like `IDistributedCache`, `IHostedService`, and `Microsoft.Extensions.VectorData` are first-party, well-tested, and supported by Microsoft.

This means .NET gets closer to zero-friction backend swapping than Python ever could with hand-rolled Protocol classes.

Key advantages of .NET 10 for this use case:
- Single binary deployment via `dotnet publish` (AOT or self-contained)
- Built-in DI container with interface-based service registration
- First-party abstractions for caching, hosting, and vector data
- Kestrel web server built in (no Uvicorn/Gunicorn equivalent needed)
- `System.Threading.Channels` built into the runtime (no asyncio queue equivalent needed)
- HybridCache GA in .NET 10 (L1 + L2 caching out of the box)

---

## 3. Approach

**Methodology**: Web research on current benchmarks, official Microsoft documentation, NuGet package data, community comparisons, and the .NET ecosystem landscape for 2024-2025.

**Tools Used**: Web search for performance data (EF Core SQLite benchmarks, LiteDB comparisons, Hangfire vs Quartz.NET, System.Threading.Channels benchmarks, vector database .NET options). Microsoft Learn documentation. NuGet package statistics.

**Limitations**: Benchmarks cited are from third-party sources and vary by hardware. OpenVEPA-specific load patterns do not yet exist. Some .NET 10 features (HybridCache, Microsoft.Extensions.VectorData) are GA but relatively new.

---

## 4. Category Analysis

### 4.1 Primary Database (Structured Data)

**Use cases**: Tasks, configurations, audit logs, agent state, user preferences.

| Aspect | Default: SQLite via EF Core | Optional: PostgreSQL via EF Core (Npgsql) | Alternative: LiteDB |
|--------|---------------------------|------------------------------------------|---------------------|
| Setup | NuGet: `Microsoft.EntityFrameworkCore.Sqlite` | NuGet: `Npgsql.EntityFrameworkCore.PostgreSQL` | NuGet: `LiteDB` |
| Config | Single file: `openvepa.db` | Connection string, user/password, port | Single file: `openvepa.litedb` |
| ORM | EF Core with LINQ, migrations, change tracking | Same EF Core code, swap provider in DI | Native LINQ, no migrations needed |
| Write throughput | ~8,000 writes/sec (WAL mode, single writer) | ~50,000+ writes/sec (concurrent writers) | Fast for document CRUD, single-writer |
| Read throughput | ~160,000 reads/sec | ~100,000+ reads/sec | Good for document reads, slower for joins |
| Concurrency | Single writer, multiple readers (WAL mode) | Full concurrent read/write | Single writer, multiple readers |
| Schema | Strict, migration-based | Strict, migration-based | Flexible, schema-free documents |
| Dependencies | 1 NuGet package (+ EF Core) | 1 NuGet package (+ EF Core) | 1 NuGet package, 100% managed C# |
| Native interop | Yes (SQLite is C, accessed via P/Invoke) | Yes (libpq via Npgsql) | None. Pure .NET, single DLL |

**EF Core Performance Notes (2024-2025)**:
- EF Core 10 shows 25-50% faster data fetching compared to .NET 8 for repeated queries.
- Compiled queries reduce plan compilation overhead. AOT compilation narrows the gap with micro-ORMs.
- For hot paths: use `AsNoTracking()` for reads, batch `SaveChanges()` calls, and add proper indexes.
- Dapper remains faster for raw throughput. EF Core is adequate for a personal assistant's workload.

**When to choose LiteDB over SQLite**:
- Data is naturally document-shaped (JSON/BSON): agent outputs, flexible configs, plugin data.
- You want zero native dependencies (LiteDB is 100% managed C#, no P/Invoke).
- Schema flexibility matters more than relational integrity.
- Deployment targets include constrained environments where native SQLite binaries are problematic.

**When SQLite wins over LiteDB**:
- Data is relational (foreign keys, joins, normalized tables).
- Full SQL query support is needed.
- Tooling ecosystem matters (SQLite has decades of viewers, analyzers, migration tools).
- EF Core provides a standardized migration path to PostgreSQL.

**Migration path (SQLite to PostgreSQL)**:
1. Use EF Core for all database access from day one. Define models with LINQ queries, not raw SQL.
2. Use EF Core Migrations for schema management. Migrations generate dialect-aware SQL.
3. To migrate: change one line in DI registration:
   ```csharp
   // Default
   services.AddDbContext<AppDbContext>(o => o.UseSqlite("Data Source=openvepa.db"));
   // Upgrade
   services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString));
   ```
4. Run `dotnet ef database update` against PostgreSQL target.
5. Data transfer: read from SQLite via EF Core, write to PostgreSQL via EF Core. Or use `pgloader`.

**Trigger for upgrade**: More than 1 concurrent user, agents on separate hosts, or database exceeding 5 GB.

**Implementation requirement**: All structured data access MUST go through EF Core. No raw `Microsoft.Data.Sqlite` calls. This is the single most important decision for migration flexibility.

---

### 4.2 Document / File Storage

**Use cases**: Agent outputs (research reports, analysis results), flexible configuration, plugin data, conversation histories with variable structure.

| Aspect | Default: File-based JSON | Alternative: LiteDB | Optional: MongoDB |
|--------|------------------------|---------------------|-------------------|
| Setup | Zero. `System.Text.Json` is built-in | NuGet: `LiteDB` | Requires MongoDB server + `MongoDB.Driver` |
| Storage | One file per document in organized directories | Single `.db` file per collection | Server-managed collections |
| Query capability | Directory listing + `System.Text.Json` deserialization | LINQ queries, indexing, full-text search | Full aggregation pipeline |
| Performance | Fast for < 1,000 documents per directory | Good for up to ~50,000 documents | Millions of documents |
| Inspectability | Maximum. Open any file in VS Code | LiteDB Studio (GUI tool) | `mongosh` or Compass |
| Git-friendly | Yes. Each document is a separate file | No (binary file changes entirely) | No |
| Concurrency | OS-level file locking | Single-writer, multi-reader | Full concurrent access |
| Dependencies | None (BCL) | 1 NuGet package | MongoDB server + NuGet package |

**When LiteDB is sufficient vs needing MongoDB**:

LiteDB covers the gap between file-based storage and MongoDB. Use LiteDB when:
- Document count is 1,000-50,000 per collection.
- You need LINQ queries across documents but not a separate server.
- Single-process access is sufficient.
- You want a single file you can copy/backup easily.

Move to MongoDB when:
- Document count exceeds 50,000 per collection with complex queries.
- Multiple processes or hosts need concurrent write access.
- You need the aggregation pipeline, change streams, or sharding.

**Recommended directory structure (file-based default)**:
```
data/
  documents/
    agent_outputs/
      research/
        2025-01-15_topic-analysis.json
      briefings/
        2025-01-15_morning.json
    configs/
      agents/
        research_agent.json
      llm_providers/
        anthropic.json
    conversations/
      session_abc123.json
```

**Migration path**:
1. Define an `IDocumentStore` interface: `SaveAsync<T>`, `GetAsync<T>`, `QueryAsync<T>`, `DeleteAsync`.
2. Default: `FileDocumentStore` using `System.Text.Json` + file system.
3. Mid-tier: `LiteDbDocumentStore` using LiteDB for queryable document storage.
4. Upgrade: `MongoDocumentStore` using `MongoDB.Driver`.
5. Swap via DI: `services.AddSingleton<IDocumentStore, FileDocumentStore>()`.

**Trigger for upgrade to LiteDB**: Need to query across documents with filters. More than 1,000 documents per collection.
**Trigger for upgrade to MongoDB**: More than 50,000 documents, multi-process write access, aggregation needs.

---

### 4.3 Caching

**Use cases**: In-flight agent state, temporary computation results, rate limit counters, session data, LLM response caching.

| Aspect | Default: HybridCache | Default (simple): IMemoryCache | Optional: Redis via IDistributedCache |
|--------|---------------------|-------------------------------|--------------------------------------|
| Setup | NuGet: `Microsoft.Extensions.Caching.Hybrid` | Built-in (`AddMemoryCache()`) | NuGet: `Microsoft.Extensions.Caching.StackExchangeRedis` |
| Latency | Nanoseconds (L1 memory) + configurable L2 | Nanoseconds (in-process) | Sub-millisecond (network round-trip) |
| Persistence | L1 lost on restart; L2 depends on backend | Lost on process restart | Configurable (RDB snapshots, AOF) |
| Cross-process | Only if L2 backend is distributed | No | Yes |
| Cache stampede | Built-in protection (single caller populates) | Manual implementation needed | Manual implementation needed |
| Tag invalidation | Built-in (`RemoveByTagAsync`) | Not available | Manual key management |
| Dependencies | 1 NuGet package | None (BCL) | NuGet + Redis server |

**The .NET caching abstraction stack**:

.NET provides a layered caching architecture. Code against the highest-level abstraction you need:

1. **`IMemoryCache`**: In-process only. Fast. Cannot swap to distributed. Use for simple, non-critical caching.
2. **`IDistributedCache`**: Interface with multiple implementations. Code against this, swap backends via DI:
   - `AddDistributedMemoryCache()` -- in-process (dev/testing, no external service)
   - `AddStackExchangeRedisCache()` -- Redis (production)
   - `AddDistributedSqlServerCache()` -- SQL Server
3. **`HybridCache`** (.NET 10 GA): Combines L1 (memory) + L2 (any `IDistributedCache` backend). Single `GetOrCreateAsync` API. Stampede protection. Tag-based invalidation. This is the recommended default.

**Key architectural point**: `IDistributedCache` allows swapping backends without code changes. Register a different implementation in `Program.cs` and all consuming code works unchanged:

```csharp
// Development (zero dependencies)
builder.Services.AddDistributedMemoryCache();

// Production (Redis)
builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = "localhost:6379");

// Either way, inject IDistributedCache in your services
public class AgentStateService(IDistributedCache cache) { ... }
```

**HybridCache as default** (recommended for OpenVEPA):
```csharp
// Zero-config default: L1 memory + L2 in-memory (no Redis needed)
builder.Services.AddHybridCache();

// Later, add Redis as L2 without changing any service code:
builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = "localhost:6379");
```

**Trigger for upgrade to Redis**: Multiple processes need shared cache, agents on separate hosts, or pub/sub messaging needed.

---

### 4.4 Vector Database (Embeddings, Semantic Search, RAG)

**Use cases**: Semantic search over documents, RAG for agent context, conversation memory similarity search, knowledge base retrieval.

| Aspect | Default: SQLite + sqlite-vec | Alternative: SharpVector (in-memory) | Optional: Qdrant (server) |
|--------|----------------------------|--------------------------------------|--------------------------|
| Setup | NuGet: `sqlite-vec` + Semantic Kernel connector | NuGet: `Build5Nines.SharpVector` | Docker: `qdrant/qdrant` + NuGet client |
| Type | Embedded (SQLite extension) | In-memory library | Client-server |
| Persistence | Built-in (SQLite file) | Manual (serialize to disk) | Built-in, enterprise-grade |
| Index type | Brute-force KNN (no ANN) | Linear scan | HNSW (approximate nearest neighbor) |
| Max vectors (practical) | ~1 million (128-dim) | ~100,000 (memory-limited) | Billions (horizontal scaling) |
| Query latency | Low ms for < 100K vectors | Low ms for < 50K vectors | Sub-ms to low ms (HNSW) |
| Metadata filtering | Via SQL WHERE clauses | Basic | Advanced filtering, payload indexing |
| .NET native | Via P/Invoke (C extension) | 100% managed C# | REST/gRPC client |
| Semantic Kernel | Yes (`Microsoft.SemanticKernel.Connectors.SqliteVec`) | No official connector | Yes (official connector) |

**Microsoft.Extensions.VectorData -- the .NET vector abstraction**:

Microsoft introduced `Microsoft.Extensions.VectorData` as a first-party abstraction for vector stores. This is the .NET equivalent of defining a `VectorStore` Protocol in Python, but it ships from Microsoft with official connectors.

Connectors available: Azure AI Search, Qdrant, Redis, SQLite (via sqlite-vec), Chroma, Pinecone, Weaviate, CosmosDB, in-memory (volatile).

```csharp
// Define a record with vector attributes
public class MemoryEntry
{
    [VectorStoreRecordKey]
    public string Id { get; set; }

    [VectorStoreRecordData]
    public string Content { get; set; }

    [VectorStoreRecordVector(384, DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}

// Swap implementation via DI -- code stays the same
```

**Best embedded vector option for .NET**:

**SQLite + sqlite-vec** is the recommended default. Reasons:
- Reuses the existing SQLite database (one fewer file/service to manage).
- Semantic Kernel provides an official connector (`Microsoft.SemanticKernel.Connectors.SqliteVec`).
- Handles up to ~1 million 128-dimensional vectors with brute-force KNN.
- A personal assistant's vector collection will be thousands to low tens of thousands. Brute-force is fine.
- Cross-platform: runs anywhere SQLite runs.

**SharpVector** as lightweight alternative:
- 100% managed C#, zero native dependencies.
- Good for prototyping or when native interop is problematic.
- No persistence by default (must serialize manually).
- No HNSW or ANN -- linear scan only.

**Qdrant for production**:
- HNSW index for sub-millisecond queries at scale.
- Full filtering, multi-tenant, horizontal scaling.
- Runs as Docker container or cloud service.
- Official Semantic Kernel connector.

**Migration path**:
1. Code against `Microsoft.Extensions.VectorData` abstractions (first-party Microsoft).
2. Default: SQLite + sqlite-vec via Semantic Kernel connector.
3. Prototyping: In-memory volatile store (`Microsoft.SemanticKernel.Connectors.InMemory`).
4. Production: Qdrant via `Microsoft.SemanticKernel.Connectors.Qdrant`.
5. Swap via DI registration -- no business logic changes.

**Trigger for upgrade**: More than 1 million vectors, need for ANN (HNSW) index, multi-tenant access, or sub-millisecond query requirements at scale.

---

### 4.5 Task Scheduling

**Use cases**: Daily briefing generation, periodic data syncing, scheduled research tasks, recurring reminders, health checks.

| Aspect | Default: Hangfire (SQLite) | Alternative: Quartz.NET | Built-in: IHostedService + Timer |
|--------|--------------------------|------------------------|--------------------------------|
| Setup | NuGet: `Hangfire.Core` + `Hangfire.Storage.SQLite` | NuGet: `Quartz` | Built-in (`BackgroundService`) |
| Dashboard | Yes, built-in web UI | None (custom or third-party) | None |
| Scheduling types | Fire-and-forget, delayed, recurring (cron) | Cron, calendar, complex triggers | Timer-based (manual cron parsing) |
| Persistence | Automatic (SQLite, SQL Server, Redis) | Pluggable job store (DB/memory) | None (in-memory only) |
| Retry/error handling | Automatic retries, exponential backoff | Configurable, manual setup | Manual |
| ASP.NET Core integration | Native DI integration, middleware | DI integration, hosted service | Native |
| Clustering | Multiple servers (moderate) | Native clustering, high scalability | None |
| Learning curve | Low (simple API) | Moderate-High (job/trigger model) | Very low |

**Hangfire vs Quartz.NET for OpenVEPA**:

**Hangfire is the better default**. Reasons:
- Built-in dashboard provides visibility into job status, retries, and history. For a personal assistant, seeing "your morning briefing job failed 3 times" in a web UI is valuable.
- SQLite storage means zero external dependencies. Reuse the same SQLite database.
- Simple API: `BackgroundJob.Enqueue(() => GenerateBriefing())` -- that's it.
- Automatic retries with exponential backoff, out of the box.

**Quartz.NET is better when**:
- You need calendar-based exclusions ("run every weekday except holidays").
- Clustering across multiple servers is required.
- Complex job chains or execution hierarchies are needed.

**Built-in BackgroundService for simple cases**:
For simple periodic tasks that don't need persistence or retry logic, .NET's built-in `BackgroundService` + `PeriodicTimer` is sufficient and requires zero additional packages:

```csharp
public class HealthCheckService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(ct))
        {
            await RunHealthCheck();
        }
    }
}
```

**Recommended approach**: Use `BackgroundService` for simple timers. Use Hangfire for anything that needs persistence, retry, or a dashboard. Reserve Quartz.NET for enterprise scheduling needs.

**Trigger for upgrade to Quartz.NET**: Calendar-based scheduling, job clustering, or complex trigger hierarchies.

---

### 4.6 Message Queue / Background Jobs

**Use cases**: Agent-to-agent communication, task dispatching from orchestrator to agents, async job processing, event broadcasting.

| Aspect | Default: System.Threading.Channels | Optional: MassTransit + RabbitMQ | Optional: Redis Streams |
|--------|-----------------------------------|--------------------------------|------------------------|
| Setup | Built-in (BCL, no NuGet needed) | NuGet: `MassTransit.RabbitMQ` + RabbitMQ server | NuGet: `StackExchange.Redis` + Redis server |
| Scope | Single process | Cross-process, cross-host | Cross-process, cross-host |
| Delivery guarantee | None (process crash = lost) | At-least-once or exactly-once | At-least-once (consumer acks) |
| Throughput | Very high (~millions msgs/sec, no serialization) | ~50,000+ msgs/sec (network + serialization) | ~100,000+ msgs/sec |
| Backpressure | Built-in (bounded channels) | Broker-managed | Stream consumer groups |
| Memory overhead | Very low (~200KB for 1M operations) | Higher (network buffers, serialization) | Moderate |
| Async-first | Yes (`ReadAsync`/`WriteAsync`) | Yes | Yes |
| Patterns | Producer/consumer, pipeline | Pub/sub, sagas, retries, dead-letter | Pub/sub, consumer groups |
| Dependencies | None (BCL) | NuGet + RabbitMQ server | NuGet + Redis server |

**System.Threading.Channels is the correct default**. This is the .NET equivalent of Python's `asyncio.Queue`, but with stronger guarantees:
- Thread-safe by design (asyncio queues are single-threaded).
- Bounded channels provide backpressure (producers wait when buffer is full).
- Very low allocation (~200KB vs ~8MB for equivalent `ConcurrentQueue` operations).
- Built into the runtime. No package to install.

```csharp
// Create a bounded channel for agent task dispatch
var channel = Channel.CreateBounded<AgentTask>(new BoundedChannelOptions(100)
{
    FullMode = BoundedChannelFullMode.Wait
});

// Producer (orchestrator)
await channel.Writer.WriteAsync(new AgentTask { ... });

// Consumer (agent)
await foreach (var task in channel.Reader.ReadAllAsync(ct))
{
    await ProcessTask(task);
}
```

**MassTransit as the upgrade path**: MassTransit provides a transport abstraction over RabbitMQ, Azure Service Bus, Amazon SQS, and in-memory. It adds sagas, retries, dead-letter queues, and distributed transactions. For a personal assistant scaling to multi-service architecture, MassTransit is the natural next step.

**Migration path**:
1. Define message contracts as C# records/classes.
2. Default: `Channel<T>` for in-process communication.
3. Wrapper: Build a thin `IMessageBus` abstraction over Channels.
4. Upgrade: Swap `IMessageBus` implementation to MassTransit (which itself abstracts over RabbitMQ, Redis, etc.).
5. MassTransit even has an in-memory transport for testing.

**Trigger for upgrade**: Agents run in separate processes, cross-host communication needed, delivery guarantees required, or saga/workflow orchestration needed.

---

### 4.7 Hosting

**Use cases**: HTTP API, WebSocket connections, real-time agent communication, dashboard serving.

| Aspect | ASP.NET Core + Kestrel |
|--------|----------------------|
| Setup | Built-in. `dotnet new webapi` includes everything |
| Web server | Kestrel (cross-platform, high-performance) |
| HTTP support | HTTP/1.1, HTTP/2, HTTP/3 |
| WebSocket | Built-in support |
| Real-time | SignalR (built-in, WebSocket + fallback transports) |
| Performance | Millions of req/sec for plaintext, hundreds of thousands for JSON |
| Concurrency | Async event-driven I/O, thousands of concurrent connections |
| TLS | Built-in, or terminate at reverse proxy |
| Deployment | Self-contained binary, Docker, systemd, Windows Service |

**Kestrel is the only hosting option needed**. Unlike Python (where you choose between Uvicorn, Gunicorn, Hypercorn, etc.), .NET has one answer: Kestrel. It is:
- The default web server in every ASP.NET Core application.
- Regularly among the top performers in TechEmpower benchmarks.
- Supports WebSockets natively (no additional package).
- Runs SignalR for real-time agent communication.

**SignalR for real-time features**:
SignalR provides a higher-level abstraction over WebSockets with automatic fallback, connection management, groups, and hub-based routing. For an AI assistant, SignalR is ideal for:
- Streaming LLM responses to the UI.
- Real-time agent status updates.
- Push notifications for completed tasks.

**Reverse proxy (optional)**:
For production, place Nginx or Caddy in front of Kestrel for:
- HTTPS termination with automatic certificate renewal (Caddy).
- Static file serving.
- Rate limiting and request buffering.

For development and single-user deployment, Kestrel alone is sufficient.

---

## 5. Results: .NET 10 Default Stack

### Default Stack (Zero External Services)

| Category | Technology | Install | External Service | Approx. Memory |
|----------|-----------|---------|-----------------|----------------|
| Primary Database | SQLite via EF Core (WAL mode) | 1 NuGet package | None | ~5 MB |
| Document Store | File-based JSON (`System.Text.Json`) | None (BCL) | None | ~1 MB |
| Cache | HybridCache (L1 memory + L2 in-memory) | 1 NuGet package | None | ~10 MB |
| Vector Database | SQLite + sqlite-vec (via Semantic Kernel) | 2 NuGet packages | None | ~50-200 MB |
| Message Queue | `System.Threading.Channels` | None (BCL) | None | ~1 MB |
| Scheduler | Hangfire (SQLite storage) | 2 NuGet packages | None | ~15 MB |
| Hosting | ASP.NET Core Kestrel + SignalR | None (BCL) | None | ~30 MB |

**Total NuGet packages to install**: ~6 (EF Core SQLite, HybridCache, sqlite-vec, Semantic Kernel connector, Hangfire Core, Hangfire SQLite).

**Total external services to run**: 0.

**Minimum RAM**: ~150 MB (excluding LLM inference). Lower than Python equivalent due to no interpreter overhead and AOT compilation option.

**Time to first run**: Under 3 minutes (`dotnet new`, add packages, `dotnet run`).

### Production Stack (Full External Services)

| Category | Technology | Trigger |
|----------|-----------|---------|
| Primary Database | PostgreSQL via EF Core (Npgsql) | Multi-user, >5 GB data, cross-host access |
| Document Store | MongoDB via `MongoDB.Driver` | >50K documents/collection, complex queries |
| Cache | Redis via `IDistributedCache` / HybridCache L2 | Multi-process, pub/sub, distributed coordination |
| Vector Database | Qdrant (server mode) | >1M vectors, HNSW index, multi-tenant |
| Message Queue | MassTransit + RabbitMQ | Multi-process agents, delivery guarantees, sagas |
| Scheduler | Quartz.NET (clustered) | Calendar scheduling, distributed workers |
| Hosting | Kestrel behind Nginx/Caddy | HTTPS termination, rate limiting, static files |

---

## 6. Interface / Abstraction Architecture

The .NET ecosystem provides stronger built-in abstractions than the Python equivalent. Several interfaces are first-party Microsoft.

### Built-in Abstractions (No Custom Code Needed)

| Category | Interface | Default Registration | Upgrade Registration |
|----------|-----------|---------------------|---------------------|
| Cache | `IDistributedCache` | `AddDistributedMemoryCache()` | `AddStackExchangeRedisCache()` |
| Cache (L1+L2) | `HybridCache` | `AddHybridCache()` | `AddHybridCache()` + Redis L2 |
| Vector Store | `Microsoft.Extensions.VectorData` | SQLite connector | Qdrant connector |
| Hosting | `IHost` / Kestrel | `WebApplication.CreateBuilder()` | Same (add reverse proxy externally) |
| Background jobs | `IHostedService` | `BackgroundService` subclass | Same pattern |

### Custom Abstractions (Thin Wrappers Needed)

| Category | Custom Interface | Default Implementation | Upgrade Implementation |
|----------|-----------------|----------------------|----------------------|
| Primary DB | EF Core `DbContext` | `.UseSqlite()` | `.UseNpgsql()` |
| Document Store | `IDocumentStore` | `FileDocumentStore` | `MongoDocumentStore` |
| Message Queue | `IMessageBus` | `ChannelMessageBus` | `MassTransitMessageBus` |
| Scheduler | (Hangfire API directly) | Hangfire + SQLite | Hangfire + Redis / Quartz.NET |

### Configuration-Driven Backend Selection

```csharp
// Program.cs -- all infrastructure wired via DI
var builder = WebApplication.CreateBuilder(args);

// --- Database ---
var dbProvider = builder.Configuration["Infrastructure:Database:Provider"];
if (dbProvider == "postgresql")
    builder.Services.AddDbContext<AppDbContext>(o =>
        o.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSQL")));
else
    builder.Services.AddDbContext<AppDbContext>(o =>
        o.UseSqlite("Data Source=data/openvepa.db"));

// --- Cache ---
builder.Services.AddHybridCache();
var cacheProvider = builder.Configuration["Infrastructure:Cache:Provider"];
if (cacheProvider == "redis")
    builder.Services.AddStackExchangeRedisCache(o =>
        o.Configuration = builder.Configuration["Infrastructure:Cache:Redis:Connection"]);
else
    builder.Services.AddDistributedMemoryCache();

// --- Document Store ---
var docProvider = builder.Configuration["Infrastructure:DocumentStore:Provider"];
if (docProvider == "mongodb")
    builder.Services.AddSingleton<IDocumentStore, MongoDocumentStore>();
else if (docProvider == "litedb")
    builder.Services.AddSingleton<IDocumentStore, LiteDbDocumentStore>();
else
    builder.Services.AddSingleton<IDocumentStore, FileDocumentStore>();

// --- Vector Store ---
// Configured via Semantic Kernel / Microsoft.Extensions.VectorData

// --- Scheduler ---
builder.Services.AddHangfire(config =>
    config.UseSQLiteStorage("data/openvepa-jobs.db"));
builder.Services.AddHangfireServer();

// --- Hosting ---
// Kestrel is the default. No configuration needed.
```

### Configuration File

```json
{
  "Infrastructure": {
    "Database": {
      "Provider": "sqlite",
      "ConnectionStrings": {
        "SQLite": "Data Source=data/openvepa.db",
        "PostgreSQL": "Host=localhost;Database=openvepa;Username=user;Password=pass"
      }
    },
    "DocumentStore": {
      "Provider": "file",
      "File": { "BasePath": "data/documents" },
      "LiteDB": { "Path": "data/documents.litedb" },
      "MongoDB": { "ConnectionString": "mongodb://localhost:27017/openvepa" }
    },
    "Cache": {
      "Provider": "memory",
      "Redis": { "Connection": "localhost:6379" }
    },
    "VectorStore": {
      "Provider": "sqlite-vec",
      "Qdrant": { "Endpoint": "http://localhost:6333" }
    },
    "Scheduler": {
      "Provider": "hangfire",
      "Storage": "sqlite"
    },
    "MessageQueue": {
      "Provider": "channels",
      "RabbitMQ": { "Host": "localhost" }
    }
  }
}
```

---

## 7. Summary Comparison Table

| Category | Default (.NET 10) | Optional Upgrade | Interface/Abstraction | Swap Mechanism |
|----------|-------------------|-----------------|----------------------|----------------|
| Primary Database | SQLite via EF Core | PostgreSQL via EF Core (Npgsql) | `DbContext` + EF Core provider | Change `UseSqlite()` to `UseNpgsql()` in DI |
| Document Store | File-based JSON | LiteDB / MongoDB | Custom `IDocumentStore` | Swap DI registration |
| Cache | HybridCache (in-memory L1+L2) | Redis as L2 backend | `HybridCache` / `IDistributedCache` | Add `AddStackExchangeRedisCache()` |
| Vector Database | SQLite + sqlite-vec | Qdrant (server) | `Microsoft.Extensions.VectorData` | Swap Semantic Kernel connector |
| Task Scheduling | Hangfire (SQLite storage) | Quartz.NET (clustered) | Hangfire API / `IHostedService` | Swap Hangfire storage or replace with Quartz |
| Message Queue | `System.Threading.Channels` | MassTransit + RabbitMQ | Custom `IMessageBus` | Swap DI registration |
| Hosting | Kestrel + SignalR | Kestrel + Nginx/Caddy reverse proxy | `IHost` / ASP.NET Core middleware | Add reverse proxy externally |

---

## 8. Migration from Python Analysis

### What stays the same

| Principle | Python Analysis | .NET Analysis |
|-----------|----------------|---------------|
| Lightweight defaults | Zero Docker containers | Zero Docker containers |
| Optional extensions | Swap via config | Swap via DI + config |
| SQLite as default DB | SQLAlchemy ORM | EF Core ORM |
| File-based document store | Python `json` stdlib | `System.Text.Json` BCL |
| In-memory caching default | Python dict | `IMemoryCache` / HybridCache |
| In-process message queue | asyncio queues | `System.Threading.Channels` |
| Interface-based swapping | Python Protocol classes | .NET interfaces + DI |

### What changes

| Category | Python Default | .NET Default | Why It Changed |
|----------|---------------|-------------|---------------|
| ORM | SQLAlchemy + Alembic | EF Core + EF Migrations | .NET first-party ORM with identical swap pattern |
| Document store (mid-tier) | TinyDB | LiteDB | LiteDB is the .NET equivalent: embedded, single-file, LINQ queries |
| Cache abstraction | Custom `CacheBackend` Protocol | `IDistributedCache` (Microsoft) | .NET provides this as a first-party interface |
| Cache (advanced) | N/A | `HybridCache` (L1+L2) | New in .NET 10. No Python equivalent out of the box |
| Vector DB default | ChromaDB (embedded) | SQLite + sqlite-vec | Reuses existing SQLite. Semantic Kernel connector available |
| Vector abstraction | Custom `VectorStore` Protocol | `Microsoft.Extensions.VectorData` (Microsoft) | First-party Microsoft abstraction with official connectors |
| Message queue | asyncio queues | `System.Threading.Channels` | Thread-safe, bounded, backpressure built-in |
| Scheduler | APScheduler | Hangfire | Hangfire has built-in dashboard, SQLite storage, retry logic |
| Web server | Uvicorn / Gunicorn + FastAPI | Kestrel (built-in) | No choice needed. Kestrel is the .NET answer |
| Real-time | WebSockets (manual) | SignalR (built-in) | Higher-level abstraction with fallback transports |
| Hosting | Multiple options, complex config | Single `dotnet run` | .NET consolidates web server, DI, config into one host |

### Key advantage of .NET over Python for this pattern

The .NET ecosystem provides 3 first-party abstractions that Python requires custom code for:

1. **`IDistributedCache`** -- swap cache backend via DI registration. Python needs a hand-rolled `CacheBackend` Protocol.
2. **`Microsoft.Extensions.VectorData`** -- swap vector store via DI registration. Python needs a hand-rolled `VectorStore` Protocol.
3. **`HybridCache`** -- L1+L2 caching with stampede protection. Python has no equivalent.

This means less custom abstraction code to write and maintain.

---

## 9. Recommendations

| Priority | Recommendation | Rationale | Effort |
|----------|---------------|-----------|--------|
| P0 | Use EF Core for all structured data access | Enables SQLite-to-PostgreSQL swap via DI, no code changes | Low (design decision) |
| P0 | Use `Microsoft.Extensions.VectorData` for vector operations | First-party Microsoft abstraction, official connectors for SQLite, Qdrant, etc. | Low (use existing library) |
| P0 | Use `HybridCache` for all caching | GA in .NET 10, stampede protection, L1+L2, tag invalidation | Low (1 NuGet + 1 line DI) |
| P0 | Use `System.Threading.Channels` for in-process messaging | Built-in, high-performance, backpressure support. Zero dependencies | Low (BCL) |
| P1 | Define `IDocumentStore` interface | Only custom abstraction needed. 3 methods: Save, Get, Query | Low (simple interface) |
| P1 | Define `IMessageBus` interface wrapping Channels | Enables future swap to MassTransit without touching consumers | Low (thin wrapper) |
| P1 | Use Hangfire with SQLite storage for scheduling | Dashboard, persistence, retries out of the box. SQLite reuse | Low-Medium |
| P2 | Implement MongoDB document store backend | Only when document count exceeds 50K per collection | Medium |
| P2 | Implement MassTransit message bus backend | Only when multi-process architecture is needed | Medium |
| P3 | Evaluate Quartz.NET for calendar scheduling | Only when Hangfire's scheduling model is insufficient | Low |

---

## 10. Conclusion

**Verdict**: Proceed with .NET 10 lightweight default stack.

**Confidence**: High.

**Rationale**: The .NET ecosystem provides stronger built-in abstractions than Python for infrastructure swapping. Three of six backend categories have first-party Microsoft interfaces (`IDistributedCache`, `HybridCache`, `Microsoft.Extensions.VectorData`). Only two custom interfaces are needed (`IDocumentStore`, `IMessageBus`). EF Core handles the database abstraction. The default stack requires 0 external services, approximately 6 NuGet packages, and under 150 MB of RAM. Every category has a clear upgrade trigger and migration path that involves changing DI registration, not rewriting business logic.

### User Impact

- **What changes for you**: The default deployment is `dotnet publish` + run the binary. PostgreSQL, MongoDB, Redis, Qdrant, and RabbitMQ become optional. Install them only when you hit specific scale triggers.
- **Effort required**: 2 custom interfaces (`IDocumentStore`, `IMessageBus`) plus their default implementations. All other abstractions are first-party Microsoft. Estimated 2-3 days for interface design and default implementations.
- **Risk if ignored**: Users face Docker setup before seeing "Hello World." Early development carries the overhead of debugging external services. Contributors need multiple servers running locally to develop.

---

## 11. Appendices

### Appendix A: Sources Consulted

- EF Core SQLite provider documentation (learn.microsoft.com/ef/core/providers/sqlite)
- EF Core 10 performance improvements (blog.elmah.io, developersvoice.com/blog/database/orm-showdown-2025)
- LiteDB official site (litedb.org) and .NET MAUI comparison benchmarks (dev.to/naderelshehabi)
- Hangfire vs Quartz.NET comparison (10decoders.com, daily-devops.net, boldsign.com)
- System.Threading.Channels documentation (learn.microsoft.com/dotnet/core/extensions/channels)
- System.Threading.Channels vs ConcurrentQueue benchmarks (stackoverflow.com, codegenes.net)
- Microsoft.Extensions.VectorData announcement (devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-vector-data)
- sqlite-vec documentation (github.com/asg017/sqlite-vec) and Semantic Kernel connector (learn.microsoft.com)
- Build5Nines.SharpVector (github.com/Build5Nines/SharpVector)
- HybridCache GA announcement (devblogs.microsoft.com/dotnet/hybrid-cache-is-now-ga)
- IDistributedCache documentation (learn.microsoft.com, github.com/dotnet/AspNetCore.Docs)
- MassTransit RabbitMQ documentation (masstransit.io)
- Kestrel web server documentation (learn.microsoft.com)
- SignalR production hosting and scaling (learn.microsoft.com)
- Qdrant .NET client and Semantic Kernel integration (systenics.ai, devblogs.microsoft.com/dotnet)

### Appendix B: Data Transparency

- **Found**: EF Core 10 query improvements (25-50% over .NET 8). SQLite WAL throughput (~8K writes/sec, ~160K reads/sec). System.Threading.Channels allocation data (~200KB vs ~8MB for ConcurrentQueue). HybridCache GA status in .NET 10. Microsoft.Extensions.VectorData connector list. sqlite-vec brute-force limit (~1M vectors). Hangfire SQLite storage availability. Kestrel benchmark rankings.
- **Not Found**: OpenVEPA-specific load patterns. Exact memory footprint of sqlite-vec with typical embedding dimensions. HybridCache performance benchmarks under load (too new). LiteDB write throughput at scale (community benchmarks only, not standardized).

### Appendix C: NuGet Package List (Default Stack)

```xml
<!-- Primary Database -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0" />

<!-- Caching -->
<PackageReference Include="Microsoft.Extensions.Caching.Hybrid" Version="10.3.0" />

<!-- Vector Database -->
<PackageReference Include="sqlite-vec" Version="0.1.7-alpha.2.1" />
<PackageReference Include="Microsoft.SemanticKernel.Connectors.SqliteVec" Version="*-*" />

<!-- Scheduler -->
<PackageReference Include="Hangfire.Core" Version="1.8.*" />
<PackageReference Include="Hangfire.Storage.SQLite" Version="*" />

<!-- Everything else is built into the BCL / ASP.NET Core -->
<!-- System.Text.Json, System.Threading.Channels, IMemoryCache, -->
<!-- IDistributedCache, Kestrel, SignalR -- all included -->
```

### Appendix D: Comparison with Python Analysis

| Category | Python Default | .NET Default | Custom Interface Needed? |
|----------|---------------|-------------|------------------------|
| Primary DB | SQLite (stdlib) | SQLite via EF Core | No (EF Core is the abstraction) |
| Document Store | File JSON (stdlib) | File JSON (BCL) | Yes (`IDocumentStore`) |
| Cache | Python dict (stdlib) | HybridCache | No (first-party) |
| Vector DB | ChromaDB (pip) | sqlite-vec + SK | No (`Microsoft.Extensions.VectorData`) |
| Message Queue | asyncio queue (stdlib) | Channels (BCL) | Yes (`IMessageBus`) |
| Scheduler | APScheduler (pip) | Hangfire (NuGet) | No (Hangfire API) |
| Web Server | Uvicorn (pip) | Kestrel (BCL) | No (built-in) |
| Real-time | Manual WebSocket | SignalR (BCL) | No (built-in) |
| **Custom interfaces** | **6 needed** | **2 needed** | .NET reduces custom code by 67% |
