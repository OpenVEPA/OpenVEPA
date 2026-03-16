# Analysis: WebSocket Communication Architecture for OpenVEPA

## 1. Objective and Scope

**Objective**: Evaluate WebSocket communication options for real-time bidirectional messaging between OpenVEPA clients (CLI, TUI, Web GUI, messaging bots) and the OpenVEPA core/assistant on C#/.NET 10.

**Scope**: Covers library selection, hub architecture, message protocol design, client integration patterns, security, and scalability. Excludes HTTP REST API design (covered separately) and UI framework selection.

**Platform note**: The current `architecture.md` describes a Python-based system. This analysis targets the C#/.NET 10 implementation. The architecture document requires updating to reflect the platform change.

---

## 2. Context

OpenVEPA is a personal AI assistant with multiple client interfaces. The project requirements specify:

- TUI as the primary interface, CLI for control, Web GUI as secondary (Section 6.1 of `project_requirements.md`)
- Concurrent task execution with real-time progress tracking (Section 3.3)
- Agent communication via structured messages (Section 3.4)
- Dashboard with active tasks, agent status, and notifications (Section 6.2)

These requirements demand persistent bidirectional communication. HTTP polling wastes resources and adds latency. Server-Sent Events provide one-way streaming only. WebSockets provide full-duplex communication over a single TCP connection, matching all requirements.

The infrastructure analysis (`infrastructure_analysis.md`) established a "lightweight default, optional scale" pattern. The WebSocket solution must follow the same principle: zero external services by default, Redis backplane as an optional upgrade.

---

## 3. Approach

**Methodology**: Web research on .NET WebSocket libraries, official Microsoft documentation for SignalR on .NET 9/10, community benchmarks, and analysis of OpenVEPA-specific communication patterns.

**Tools Used**: Web search (Microsoft Learn, NuGet, StackOverflow, community benchmarks), project requirements review.

**Limitations**: No .NET 10-specific SignalR benchmarks exist yet (preview release). Performance data sourced from .NET 8/9 benchmarks. OpenVEPA has no production traffic patterns to measure against.

---

## 4. Data and Analysis

### 4.1 WebSocket Options in .NET

Four approaches exist for WebSocket communication in .NET 10.

#### Option A: ASP.NET Core SignalR

SignalR is Microsoft's official real-time communication framework, shipped with ASP.NET Core. First released in 2013, rewritten for ASP.NET Core in 2018. 12+ years of production use.

| Aspect | Detail |
|--------|--------|
| **Package** | `Microsoft.AspNetCore.SignalR` (server, included in ASP.NET Core SDK) |
| **Client packages** | `Microsoft.AspNetCore.SignalR.Client` (.NET), `@microsoft/signalr` (JavaScript/TypeScript) |
| **Protocol** | JSON (default) or MessagePack (binary, via `AddMessagePackProtocol()`) |
| **Transport** | WebSocket (preferred), Server-Sent Events (fallback), Long Polling (last resort) |
| **Features** | Hubs (RPC-style), strongly-typed hubs, groups, user tracking, auto-reconnect, streaming (`IAsyncEnumerable`), connection lifetime events |
| **Authentication** | JWT bearer tokens, cookies, Windows Auth, custom token providers |
| **Scale-out** | Redis backplane (`Microsoft.AspNetCore.SignalR.StackExchangeRedis`), Azure SignalR Service |
| **Diagnostics** | OpenTelemetry integration, .NET Aspire dashboard support (.NET 9+) |
| **AOT support** | Native AOT with System.Text.Json source generators (.NET 9+, strongly-typed hubs not supported under AOT) |
| **Overhead** | 10-15% slower than raw WebSocket in message throughput benchmarks |
| **Learning curve** | Low for .NET developers. Hub pattern is familiar RPC. |

#### Option B: Raw WebSocket (System.Net.WebSockets)

Built into .NET and ASP.NET Core. The `WebSocket` class provides low-level frame-based communication.

| Aspect | Detail |
|--------|--------|
| **Package** | None required (part of .NET runtime and ASP.NET Core middleware) |
| **Protocol** | Custom (you define framing, serialization, routing) |
| **Transport** | WebSocket only (no fallback) |
| **Features** | Send/receive binary or text frames. Nothing else built-in. |
| **Authentication** | Manual (validate before upgrade handshake) |
| **Scale-out** | Manual (implement pub/sub yourself) |
| **Overhead** | Minimal. Direct frame access. Lowest latency possible. |
| **Learning curve** | High. Must implement: message routing, reconnection, heartbeats, group management, serialization, error handling. |

#### Option C: WatsonWebsocket

Third-party library. Async-first, event-driven API. Supports .NET 8+.

| Aspect | Detail |
|--------|--------|
| **Package** | `WatsonWebsocket` (NuGet, ~4.1.x) |
| **Protocol** | Custom (event-driven message handling) |
| **Features** | Client + server, SSL/TLS, cross-platform, simple event API |
| **Maintenance** | Actively maintained as of 2024. Single maintainer. |
| **Scale-out** | None built-in |
| **Overhead** | Low. Thin wrapper over System.Net.WebSockets. |
| **Learning curve** | Low-medium. Simpler than raw WebSocket, but no RPC or hub abstraction. |

#### Option D: Fleck / WebSocketSharp

Legacy libraries. Fleck is server-only, no async/await. WebSocketSharp is unmaintained since ~2020. Neither targets modern .NET.

| Aspect | Detail |
|--------|--------|
| **Target** | .NET Framework 4.x primarily |
| **Maintenance** | Stagnant (Fleck) / Abandoned (WebSocketSharp) |
| **Recommendation** | Do not use for new .NET 10 projects |

#### Comparison Matrix

| Criteria | SignalR | Raw WebSocket | WatsonWebsocket | Fleck/WSSharp |
|----------|---------|---------------|-----------------|---------------|
| RPC-style hubs | Yes | No | No | No |
| Streaming (IAsyncEnumerable) | Yes | Manual | No | No |
| Auto-reconnect | Yes | Manual | Manual | No |
| Transport fallback | Yes | No | No | No |
| Groups/user tracking | Yes | Manual | Manual | No |
| MessagePack binary | Yes | Manual | Manual | No |
| Client libraries | .NET, JS, Java | Manual | .NET only | .NET only |
| Redis scale-out | Yes (1 line config) | Manual | No | No |
| Authentication integration | Yes (JWT, cookies) | Manual | Manual | No |
| OpenTelemetry | Yes (.NET 9+) | No | No | No |
| .NET 10 support | Yes (first-party) | Yes (runtime) | Likely | No |
| Implementation effort | Low | Very High | Medium | N/A |
| Lindy Effect (age) | 12+ years | 14+ years (RFC 6455) | ~5 years | Declining |

---

### 4.2 SignalR Deep Dive

SignalR is the recommended option. This section details the features relevant to OpenVEPA.

#### Hub Pattern

Hubs are the central abstraction. Each hub method is a remote procedure call. The server defines methods the client can invoke. The client registers handlers the server can call.

```csharp
// Server-side hub
public class AssistantHub : Hub<IAssistantClient>
{
    public async IAsyncEnumerable<StreamToken> SendMessage(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var token in _assistant.StreamResponseAsync(request, ct))
        {
            yield return token;
        }
    }
}

// Strongly-typed client interface
public interface IAssistantClient
{
    Task ReceiveMessage(ChatResponse response);
    Task ReceiveStreamToken(StreamToken token);
    Task TaskStatusChanged(TaskStatusEvent status);
    Task SystemEvent(SystemEvent evt);
}
```

#### Strongly-Typed Hubs

Using `Hub<T>` where `T` is an interface provides compile-time safety for server-to-client calls. If a method name changes, the compiler catches it. This eliminates the string-based `Clients.All.SendAsync("MethodName")` pattern.

**Limitation**: Strongly-typed hubs are not supported under Native AOT (.NET 9+). OpenVEPA does not require AOT for the server component, so this is not a constraint.

#### IAsyncEnumerable Streaming

Critical for LLM token-by-token output. SignalR natively supports `IAsyncEnumerable<T>` as a hub method return type. Each `yield return` sends a message to the client immediately. The client receives tokens as an observable stream.

```csharp
// Server streams tokens
public async IAsyncEnumerable<string> StreamTokens(
    string prompt,
    [EnumeratorCancellation] CancellationToken ct)
{
    await foreach (var token in _llm.GenerateAsync(prompt, ct))
    {
        yield return token;
    }
}
```

```javascript
// JavaScript client consumes stream
const stream = connection.stream("StreamTokens", prompt);
stream.subscribe({
    next: (token) => appendToUI(token),
    complete: () => markComplete(),
    error: (err) => handleError(err)
});
```

```csharp
// .NET client (CLI/TUI) consumes stream
var stream = hubConnection.StreamAsync<string>("StreamTokens", prompt, ct);
await foreach (var token in stream)
{
    Console.Write(token);
}
```

#### MessagePack Protocol

MessagePack is a binary serialization format. Smaller payloads and faster serialization than JSON. Configured with one line:

```csharp
builder.Services.AddSignalR()
    .AddMessagePackProtocol();
```

**Trade-off**: MessagePack is not human-readable. JSON is easier to debug. Recommendation: use JSON in development, MessagePack in production. Make it configurable.

| Protocol | Message size (typical chat) | Serialization speed | Debuggability |
|----------|---------------------------|---------------------|---------------|
| JSON | ~200-500 bytes | Baseline | High (human-readable) |
| MessagePack | ~120-300 bytes (40-50% smaller) | 2-3x faster | Low (binary) |

#### Client Libraries

| Client Type | Library | Package |
|-------------|---------|---------|
| Web GUI (JavaScript/TypeScript) | `@microsoft/signalr` | npm |
| CLI / TUI (.NET) | `Microsoft.AspNetCore.SignalR.Client` | NuGet |
| Python bridge (if needed) | `signalrcore` | pip (community) |
| Java/Android (future) | `com.microsoft.signalr` | Maven |

The .NET client supports:
- `WithAutomaticReconnect()` with configurable retry intervals
- `StreamAsync<T>()` for consuming server streams
- `InvokeAsync()` for request-response calls
- `On<T>()` for registering push notification handlers
- JWT token factory via `WithUrl(url, options => options.AccessTokenProvider = ...)`

#### Connection Management

SignalR provides:
- **Connection IDs**: Unique per connection. Use for direct messaging.
- **Groups**: Named sets of connections. Use for topic-based routing (e.g., "task-123-watchers").
- **Users**: Map authenticated user identity to connections. One user can have multiple connections (CLI + Web GUI simultaneously).
- **OnConnectedAsync / OnDisconnectedAsync**: Lifecycle hooks for tracking active clients.

#### Scale-Out: Single Server vs Redis Backplane

**Single server (default)**: All connections are in-memory. Groups and user mappings are process-local. Zero configuration. Handles hundreds of concurrent connections on commodity hardware.

**Redis backplane (optional)**: One line of configuration adds cross-server message synchronization.

```csharp
builder.Services.AddSignalR()
    .AddStackExchangeRedis("localhost:6379", options =>
    {
        options.Configuration.ChannelPrefix = RedisChannel.Literal("OpenVEPA");
    });
```

**When to add Redis backplane**:
- Multiple OpenVEPA server instances behind a load balancer
- Horizontal scaling for multi-user deployments
- Sticky sessions required on the load balancer when using Redis backplane

**For a single-user personal assistant, the default in-memory mode is sufficient.** This aligns with the infrastructure analysis principle: zero external services by default.

---

### 4.3 Communication Architecture for OpenVEPA

#### Hub Design

Three hubs, separated by concern. Each hub has a distinct responsibility and message frequency.

| Hub | Purpose | Message Frequency | Auth Required |
|-----|---------|-------------------|---------------|
| `AssistantHub` | Chat messages, LLM streaming, conversation management | High (streaming tokens) | Yes |
| `TaskHub` | Task lifecycle events, progress updates | Medium (status changes) | Yes |
| `SystemHub` | Health checks, config changes, system events | Low (periodic) | Yes |

**Why three hubs instead of one**: Separation allows clients to connect only to what they need. A monitoring dashboard connects to `SystemHub` and `TaskHub` but not `AssistantHub`. A chat interface connects to `AssistantHub`. This reduces unnecessary message traffic per connection.

#### AssistantHub Methods

```csharp
public class AssistantHub : Hub<IAssistantClient>
{
    // Client-to-server: Send a message, receive streamed response
    public async IAsyncEnumerable<StreamToken> SendMessage(
        ChatRequest request,
        [EnumeratorCancellation] CancellationToken ct) { ... }

    // Client-to-server: Cancel an in-progress response
    public async Task CancelResponse(string conversationId) { ... }

    // Client-to-server: Load conversation history
    public async Task<ConversationHistory> GetHistory(
        string conversationId, int limit) { ... }

    // Client-to-server: Provide feedback on a response
    public async Task SubmitFeedback(
        string messageId, FeedbackType type) { ... }
}

public interface IAssistantClient
{
    // Server-to-client: Complete message (non-streaming)
    Task ReceiveMessage(ChatResponse response);

    // Server-to-client: Typing indicator
    Task AssistantTyping(string conversationId, bool isTyping);

    // Server-to-client: Error during processing
    Task MessageError(string conversationId, ErrorInfo error);
}
```

#### TaskHub Methods

```csharp
public class TaskHub : Hub<ITaskClient>
{
    // Client-to-server: Create a new task
    public async Task<TaskInfo> CreateTask(CreateTaskRequest request) { ... }

    // Client-to-server: Cancel a running task
    public async Task CancelTask(string taskId) { ... }

    // Client-to-server: Subscribe to updates for a specific task
    public async Task WatchTask(string taskId) { ... }

    // Client-to-server: Unsubscribe from task updates
    public async Task UnwatchTask(string taskId) { ... }

    // Client-to-server: List active tasks
    public async Task<List<TaskInfo>> ListTasks(TaskFilter filter) { ... }
}

public interface ITaskClient
{
    // Server-to-client: Task status changed
    Task TaskStatusChanged(TaskStatusEvent evt);

    // Server-to-client: Task progress update (percentage, substep)
    Task TaskProgress(TaskProgressEvent evt);

    // Server-to-client: Task completed with result
    Task TaskCompleted(TaskCompletedEvent evt);

    // Server-to-client: Task failed with error
    Task TaskFailed(TaskFailedEvent evt);
}
```

#### SystemHub Methods

```csharp
public class SystemHub : Hub<ISystemClient>
{
    // Client-to-server: Get current system status
    public async Task<SystemStatus> GetStatus() { ... }

    // Client-to-server: Get connected clients info
    public async Task<List<ClientInfo>> GetConnectedClients() { ... }
}

public interface ISystemClient
{
    // Server-to-client: System health update
    Task HealthUpdate(HealthStatus status);

    // Server-to-client: Configuration changed
    Task ConfigChanged(ConfigChangeEvent evt);

    // Server-to-client: Skill installed/removed/updated
    Task SkillEvent(SkillEvent evt);

    // Server-to-client: Agent status change
    Task AgentStatusChanged(AgentStatusEvent evt);

    // Server-to-client: Notification (approval request, alert)
    Task Notification(NotificationEvent evt);
}
```

#### Communication Patterns

| Pattern | Hub | Implementation | Use Case |
|---------|-----|---------------|----------|
| Request-Response | AssistantHub | `InvokeAsync` returns result | Load conversation history |
| Streaming | AssistantHub | `IAsyncEnumerable<T>` return type | Token-by-token LLM output |
| Push Notification | TaskHub | `Clients.User(userId).TaskCompleted(evt)` | Task finished while user was away |
| Broadcast | SystemHub | `Clients.All.HealthUpdate(status)` | System health to all connected clients |
| Group Targeted | TaskHub | `Clients.Group("task-123").TaskProgress(evt)` | Progress to task watchers only |

---

### 4.4 Client Integration Patterns

#### Web GUI (JavaScript/TypeScript)

```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/assistant", {
        accessTokenFactory: () => getJwtToken()
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();

// Register handlers
connection.on("ReceiveMessage", (response) => renderMessage(response));
connection.on("AssistantTyping", (convId, typing) => showTyping(typing));

// Stream tokens
async function sendMessage(prompt) {
    const stream = connection.stream("SendMessage", { text: prompt });
    stream.subscribe({
        next: (token) => appendToken(token),
        complete: () => finishMessage(),
        error: (err) => showError(err)
    });
}

await connection.start();
```

#### CLI / TUI (.NET Client)

```csharp
var connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/hubs/assistant", options =>
    {
        options.AccessTokenProvider = () => Task.FromResult(GetToken());
    })
    .WithAutomaticReconnect()
    .AddMessagePackProtocol() // Binary protocol for efficiency
    .Build();

connection.On<ChatResponse>("ReceiveMessage", response =>
{
    Console.WriteLine($"Assistant: {response.Text}");
});

await connection.StartAsync();

// Stream response
await foreach (var token in
    connection.StreamAsync<StreamToken>("SendMessage", new ChatRequest { Text = input }))
{
    Console.Write(token.Text);
}
```

#### Telegram / WhatsApp Bots

Bot adapters do not connect via WebSocket directly. They act as bridges:

```
User -> Telegram API -> Bot Service -> SignalR .NET Client -> OpenVEPA Hub
                                    <- SignalR .NET Client <- OpenVEPA Hub
User <- Telegram API <- Bot Service
```

The bot service is a .NET background worker that:
1. Listens for incoming messages from the messaging platform API
2. Forwards them to `AssistantHub` via the SignalR .NET client
3. Streams the response back
4. Sends the response through the messaging platform API

This keeps the bot adapter thin. All intelligence stays in the OpenVEPA core.

#### Mobile App (Future)

SignalR provides official client libraries for:
- .NET MAUI (via `Microsoft.AspNetCore.SignalR.Client`)
- JavaScript/React Native
- Java/Kotlin (Android)
- Swift (iOS, community library)

No architectural changes needed. The hub API is client-agnostic.

---

### 4.5 Message Protocol Design

#### Message Envelope

All messages use a consistent envelope structure. SignalR handles serialization and transport. The envelope is the application-level schema.

```csharp
public record MessageEnvelope<T>
{
    public string Id { get; init; } = Ulid.NewUlid().ToString();
    public string Type { get; init; }           // "chat", "command", "event", "error", "stream_chunk"
    public string CorrelationId { get; init; }  // Links request to response
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public int Version { get; init; } = 1;      // Schema version
    public T Payload { get; init; }
}
```

#### Message Types

| Type | Direction | Description | Example |
|------|-----------|-------------|---------|
| `chat` | Client-to-Server | User message to assistant | "What's the weather?" |
| `chat_response` | Server-to-Client | Complete assistant response | Full response text |
| `stream_chunk` | Server-to-Client | Single token from LLM | "The" |
| `command` | Client-to-Server | System command | Create task, cancel response |
| `event` | Server-to-Client | System/task event | Task completed, skill installed |
| `error` | Server-to-Client | Error notification | Rate limited, LLM failure |

#### Correlation IDs

Every client request includes a `CorrelationId`. All server responses related to that request carry the same ID. This enables:

- Matching streamed tokens to the originating request
- Matching async task completions to the request that created them
- Client-side request timeout tracking
- Debugging and log correlation

```csharp
// Client sends
var correlationId = Ulid.NewUlid().ToString();
var request = new ChatRequest
{
    Text = "Summarize this article",
    CorrelationId = correlationId
};

// Server response carries same ID
public record StreamToken
{
    public string CorrelationId { get; init; }
    public string Text { get; init; }
    public bool IsComplete { get; init; }
    public int SequenceNumber { get; init; }
}
```

#### Schema Versioning

The `Version` field in the envelope enables backward compatibility. When the schema changes:

1. Increment `Version`
2. Server checks client version on connection (negotiation)
3. Server can translate messages between versions if needed
4. Old clients continue working until explicitly deprecated

---

### 4.6 Security Considerations

#### Authentication Before WebSocket Upgrade

SignalR integrates with ASP.NET Core authentication middleware. The JWT token is validated before the WebSocket connection is established.

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // WebSocket cannot send headers, so token comes via query string
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
```

#### Per-Connection Authorization

```csharp
[Authorize]
public class AssistantHub : Hub<IAssistantClient>
{
    // All methods require authentication

    [Authorize(Policy = "AdminOnly")]
    public async Task<SystemStatus> GetDetailedStatus() { ... }
}
```

#### Rate Limiting

ASP.NET Core 7+ includes built-in rate limiting middleware. Apply to SignalR endpoints:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("SignalRPolicy", limiter =>
    {
        limiter.PermitLimit = 100;          // 100 messages
        limiter.Window = TimeSpan.FromMinutes(1); // per minute
        limiter.QueueLimit = 10;
    });
});
```

Additionally, implement application-level rate limiting per user in the hub:

- Maximum messages per minute per user
- Maximum concurrent streams per user
- Cooldown after rapid-fire requests

#### Message Size Limits

```csharp
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 64 * 1024;  // 64 KB max client message
    options.StreamBufferCapacity = 20;               // Max items buffered per stream
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});
```

Default maximum client message size is 32 KB. For OpenVEPA, 64 KB accommodates longer prompts with context. Server-to-client messages (LLM responses) are streamed as small tokens, so size limits do not constrain output.

#### Security Checklist

| Requirement | Implementation | Status |
|-------------|---------------|--------|
| TLS/HTTPS required | Kestrel HTTPS configuration | Required |
| Authentication before connect | JWT bearer with query string extraction | Required |
| Per-hub authorization | `[Authorize]` attribute on hub classes | Required |
| Rate limiting | ASP.NET Core rate limiter + per-user tracking | Required |
| Message size limits | `MaximumReceiveMessageSize = 64KB` | Required |
| CORS policy | Restrict allowed origins to known clients | Required for Web GUI |
| Input validation | Validate all hub method parameters | Required |
| Token expiry handling | `CloseOnAuthenticationExpiration = true` (.NET 10) | Recommended |
| Connection limits | Max concurrent connections per user | Recommended |
| Logging | Audit hub invocations, never log tokens | Required |

---

## 5. Results

### Evidence Gathered

| Finding | Source | Confidence |
|---------|--------|------------|
| SignalR is 10-15% slower than raw WebSocket per message | Multiple benchmark comparisons (ably.com, wearenotch.com, videosdk.live) | Medium (no .NET 10 specific benchmarks) |
| SignalR `IAsyncEnumerable` streaming works for LLM token output | Microsoft Learn docs, Microsoft Tech Community blog on Azure OpenAI + SignalR | High |
| SignalR .NET client works in console/CLI applications | Microsoft Learn .NET client docs, StackOverflow examples | High |
| MessagePack reduces message size by 40-50% over JSON | Microsoft Learn SignalR configuration docs | High |
| Redis backplane requires 1 line of configuration | Microsoft Learn scale-out docs | High |
| Default max client message size is 32 KB | Microsoft Learn SignalR configuration | High |
| JWT auth via query string is required for WebSocket (no custom headers) | Microsoft Learn SignalR auth docs | High |
| Fleck and WebSocketSharp are unmaintained for modern .NET | NuGet/GitHub analysis, LibHunt comparisons | High |
| WatsonWebsocket is actively maintained but single-maintainer | NuGet gallery, GitHub repo | High |
| SignalR has 12+ years of production use (Lindy: High) | Initial release 2013, ASP.NET Core rewrite 2018 | High |

---

## 6. Discussion

### Why SignalR Over Raw WebSocket

OpenVEPA needs: streaming, groups, reconnection, authentication, and multi-client support. Implementing these from scratch on raw WebSocket would take weeks of development and ongoing maintenance. SignalR provides all of them out of the box.

The 10-15% overhead is irrelevant for a personal AI assistant. The bottleneck is LLM inference (seconds per response), not WebSocket framing (microseconds per message). Optimizing transport at the cost of development velocity is premature.

### Why Not Third-Party Libraries

WatsonWebsocket is well-maintained but provides no hub abstraction, no streaming, no groups, and no scale-out. It solves a different problem: raw WebSocket with a nicer API. OpenVEPA needs the higher-level features.

Fleck and WebSocketSharp are dead for modern .NET. Do not consider them.

### Hub Separation Trade-Off

Three hubs add routing complexity. A single hub would be simpler. The trade-off favors three hubs because:

1. OpenVEPA's dashboard connects to `TaskHub` and `SystemHub` without receiving chat traffic
2. Monitoring tools connect to `SystemHub` only
3. Different hubs can have different authorization policies in the future
4. Message handling code stays cohesive per hub

If this proves over-engineered in practice, collapsing to a single hub is a 30-minute refactor.

### MessagePack Trade-Off

JSON is the default because debuggability matters during development. MessagePack becomes valuable when:
- Mobile clients send/receive over cellular networks (bandwidth matters)
- High-frequency task progress updates generate significant traffic
- Production deployment optimizes for efficiency

Making the protocol configurable (one line change) means this decision can be deferred.

### Alignment with Infrastructure Analysis

The infrastructure analysis established the pattern: "define an interface, provide a default, swap via configuration." The WebSocket architecture follows this:

- **Default**: SignalR with in-memory connection management (zero external services)
- **Optional upgrade**: SignalR with Redis backplane (one configuration line)
- **Trigger for upgrade**: Multiple server instances or multi-user deployment

---

## 7. Recommendations

| Priority | Recommendation | Rationale | Effort |
|----------|----------------|-----------|--------|
| P0 | Use ASP.NET Core SignalR for all real-time communication | 12+ year Lindy survivor. Built-in streaming, groups, auth, reconnect. Zero external dependencies. | Low (framework choice) |
| P0 | Define strongly-typed hub interfaces before implementation | Compile-time safety for server-to-client calls. Prevents string-based errors. | Low (interface design) |
| P0 | Use `IAsyncEnumerable<T>` for LLM token streaming | Native SignalR support. Client receives tokens as they are generated. Cancellation support built-in. | Low (pattern choice) |
| P0 | Implement JWT authentication with query string extraction | Required for WebSocket security. ASP.NET Core has built-in support. | Medium |
| P1 | Create 3 hubs: AssistantHub, TaskHub, SystemHub | Separation of concerns. Clients connect only to what they need. Collapsible to 1 hub if over-engineered. | Medium |
| P1 | Use JSON protocol by default, make MessagePack configurable | JSON for debuggability in development. MessagePack for production efficiency. One-line switch. | Low |
| P1 | Implement correlation IDs on all messages | Required for request-response matching, stream tracking, and debugging. | Low |
| P1 | Configure rate limiting and message size limits | Defense against abuse. 64 KB max client message, 100 messages/minute default. | Low |
| P2 | Add Redis backplane configuration (optional) | Required only for multi-server deployment. One NuGet package + one config line. | Low |
| P2 | Implement schema versioning in message envelope | Required before first public release. Enables backward-compatible changes. | Medium |
| P2 | Build bot adapter pattern for Telegram/WhatsApp | Bridge architecture: bot service connects as SignalR .NET client. | Medium |

### Recommended Stack

| Component | Technology | Why |
|-----------|-----------|-----|
| WebSocket framework | ASP.NET Core SignalR (.NET 10) | First-party, full-featured, zero dependencies |
| Server | Kestrel (standalone or behind reverse proxy) | Default ASP.NET Core server. Handles WebSocket natively. |
| Protocol (dev) | JSON | Human-readable, debuggable |
| Protocol (prod) | MessagePack (configurable) | 40-50% smaller, 2-3x faster serialization |
| Authentication | JWT bearer tokens | Stateless, works across all client types |
| Scale-out (optional) | Redis backplane | One-line configuration when needed |
| Port | 5000 (HTTP) / 5001 (HTTPS) development, 443 (HTTPS) production | Standard ASP.NET Core defaults |

### Hosting Configuration

**Development**:
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();
app.MapHub<AssistantHub>("/hubs/assistant");
app.MapHub<TaskHub>("/hubs/tasks");
app.MapHub<SystemHub>("/hubs/system");
app.Run();
// Runs on http://localhost:5000, https://localhost:5001
```

**Production behind reverse proxy (nginx/Caddy)**:
```
# nginx example
location /hubs/ {
    proxy_pass http://localhost:5000;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";
    proxy_set_header Host $host;
    proxy_cache_bypass $http_upgrade;
}
```

---

## 8. Conclusion

**Verdict**: Proceed with ASP.NET Core SignalR.

**Confidence**: High.

**Rationale**: SignalR provides every capability OpenVEPA requires (streaming, groups, auth, reconnect, multi-client) with zero external dependencies. The 10-15% overhead vs raw WebSocket is irrelevant when LLM inference dominates latency. The framework has 12+ years of production use, first-party Microsoft support, and a clear scale-out path via Redis backplane. No third-party library matches this combination of features, maturity, and .NET integration.

### User Impact

- **What changes for you**: All real-time communication (chat, task updates, system events) flows through SignalR hubs. CLI, TUI, Web GUI, and bots all connect using the same protocol. Adding a new client type requires only writing a SignalR client adapter.
- **Effort required**: Hub interface design (1-2 days). Hub implementation (3-5 days). Client integration per client type (1-2 days each).
- **Risk if ignored**: Without WebSocket architecture, clients must poll HTTP endpoints. Polling adds 100-1000ms latency to every interaction and prevents token streaming. The "typing effect" of LLM output becomes impossible without streaming.

---

## 9. Appendices

### Appendix A: Sources Consulted

- Microsoft Learn: ASP.NET Core SignalR configuration (aspnetcore-10.0)
- Microsoft Learn: SignalR streaming with IAsyncEnumerable
- Microsoft Learn: SignalR authentication and authorization
- Microsoft Learn: SignalR security considerations
- Microsoft Learn: Redis backplane for SignalR scale-out
- Microsoft Learn: SignalR .NET client
- Microsoft Learn: WebSockets support in ASP.NET Core
- Microsoft Tech Community: Real-Time AI Streaming with Azure OpenAI and SignalR
- ABP.IO Community: ASP.NET Core SignalR New Features Summary (.NET 9)
- Ably.com: SignalR vs WebSocket comparison
- wearenotch.com: ASP.NET Core SignalR vs WebSockets benchmark
- videosdk.live: SignalR vs WebSocket performance comparison 2025
- NuGet Gallery: WatsonWebsocket 4.1.x
- LibHunt: WebSocket-Sharp alternatives comparison
- GitHub: Fleck, WebSocketSharp maintenance status

### Appendix B: Data Transparency

- **Found**: SignalR overhead benchmarks (10-15% vs raw WebSocket). IAsyncEnumerable streaming pattern for LLM output. JWT query string authentication requirement for WebSocket. MessagePack size reduction estimates (40-50%). Redis backplane configuration. Default message size limits (32 KB). Client library availability for .NET, JavaScript, Java.
- **Not Found**: .NET 10-specific SignalR benchmarks (preview). OpenVEPA-specific connection count projections. Exact memory footprint of SignalR per connection on .NET 10. WatsonWebsocket benchmark data vs SignalR. Python SignalR client library stability assessment.

### Appendix C: Architecture Diagram

```
+-------------------+    +-------------------+    +-------------------+
|    Web GUI        |    |    CLI / TUI      |    |   Telegram Bot    |
|  (JS SignalR      |    |  (.NET SignalR    |    |  (.NET SignalR    |
|   Client)         |    |   Client)         |    |   Client)         |
+--------+----------+    +--------+----------+    +--------+----------+
         |                         |                        |
         | WebSocket (wss://)      | WebSocket (ws://)     | WebSocket (ws://)
         |                         |                        |
+--------v-------------------------v------------------------v----------+
|                        Kestrel / Reverse Proxy                       |
+----------------------------------------------------------------------+
|                        ASP.NET Core Pipeline                         |
|  +------------------+  +------------------+  +--------------------+  |
|  |  Authentication  |  |  Rate Limiting   |  |  CORS              |  |
|  +------------------+  +------------------+  +--------------------+  |
+----------------------------------------------------------------------+
         |                         |                        |
+--------v----------+    +--------v----------+    +--------v----------+
|  AssistantHub     |    |  TaskHub          |    |  SystemHub        |
|                   |    |                   |    |                   |
|  SendMessage()    |    |  CreateTask()     |    |  GetStatus()      |
|  CancelResponse() |    |  WatchTask()      |    |  HealthUpdate()   |
|  StreamTokens()   |    |  TaskProgress()   |    |  ConfigChanged()  |
+--------+----------+    +--------+----------+    +--------+----------+
         |                         |                        |
+--------v-------------------------v------------------------v----------+
|                        OpenVEPA Core / Assistant                     |
|  +------------------+  +------------------+  +--------------------+  |
|  |  LLM Providers   |  |  Skill Runtime   |  |  Task Manager      |  |
|  +------------------+  +------------------+  +--------------------+  |
+----------------------------------------------------------------------+
```

### Appendix D: Connection Lifecycle

```
1. Client requests WebSocket upgrade: GET /hubs/assistant
2. ASP.NET Core middleware validates JWT token (from query string)
3. If valid: WebSocket upgrade proceeds, SignalR assigns ConnectionId
4. Hub.OnConnectedAsync() fires: register user, join default groups
5. Client calls hub methods (InvokeAsync, StreamAsync)
6. Server pushes events (Clients.User(), Clients.Group(), Clients.All)
7. On disconnect: Hub.OnDisconnectedAsync() fires, cleanup resources
8. Auto-reconnect: client retries with backoff [0, 2s, 5s, 10s, 30s]
9. On reconnect: re-register groups, resume subscriptions
```
