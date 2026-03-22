# Analysis: Provider and LLM Data Model for Multi-Model Refactor

## 1. Objective and Scope

**Objective**: Map the complete data model and data flow for provider/LLM configuration across all layers. Identify every location that assumes "one provider = one model" and assess the impact of refactoring to "one provider = multiple models."

**Scope**: Config storage (openvepa.conf), C# domain types, DI registration, runtime chat client resolution, API endpoints, dashboard UI (provider settings + agent config), audit logging, and setup wizard. Excludes implementation planning.

## 2. Context

The current architecture binds each provider instance to exactly one model via a scalar `ModelId` field. The user wants:

1. One provider instance gives access to multiple LLMs (not just one).
2. The currently configured `ModelId` becomes the default model, not the sole model.
3. Same provider type can be configured multiple times (different subscriptions, different allowed models).
4. Agents select provider + specific model independently (main LLM, fallback LLM, etc.).

Requirement 3 already works. The `Instances[]` array supports multiple entries of the same `type`. The remaining 3 requirements are blocked by the 1:1 provider-to-model assumption embedded across 9 layers.

## 3. Approach

**Methodology**: Static code analysis of all layers that touch provider/model data. Cross-referenced config file format, C# types, API contracts, and embedded JavaScript.

**Tools Used**: grep, view, explore agents across 7 parallel investigations.

**Limitations**: No runtime testing performed. Dashboard JavaScript is embedded in `DashboardPageHtml.cs` (7900+ lines of inline JS), making exact line references approximate.

## 4. Data and Analysis

### 4.1 Current Data Model: Complete Type Inventory

#### Layer 1: Config File (openvepa.conf)

Format: JSON. Two supported formats.

**Instance-based (current):**
```json
{
  "Providers": {
    "DefaultProvider": "ollama",
    "Instances": [
      {
        "name": "ollama",
        "displayName": "Ollama",
        "type": "ollama",
        "endpoint": "http://localhost:11434",
        "modelId": "llama3"
      }
    ]
  }
}
```

**Legacy section-based (still supported for reads):**
```json
{
  "Providers": {
    "DefaultProvider": "ollama",
    "Ollama": {
      "Endpoint": "http://localhost:11434",
      "ModelId": "llama3"
    }
  }
}
```

Both formats: `modelId`/`ModelId` is a scalar string. One value per instance.

#### Layer 2: C# Configuration Types

| Type | File | ModelId Field | Cardinality |
|------|------|---------------|-------------|
| `ProviderInstanceOptions` | `src/OpenVEPA.Providers/ProviderOptions.cs` | `string ModelId` | **1:1** |
| `OpenAiOptions` | `src/OpenVEPA.Providers/ProviderOptions.cs` | `string Model` (mapped via `[ConfigurationKeyName("ModelId")]`) | **1:1** |
| `OllamaOptions` | `src/OpenVEPA.Providers/ProviderOptions.cs` | `string Model` (mapped via `[ConfigurationKeyName("ModelId")]`) | **1:1** |
| `ProviderOptions` | `src/OpenVEPA.Providers/ProviderOptions.cs` | `string DefaultModel` (global fallback) | **1:1** |

#### Layer 3: Agent LLM Config

| Type | File | Model Field | Notes |
|------|------|-------------|-------|
| `AgentLlmConfig` | `src/OpenVEPA.Core/Agents/AgentLlmConfig.cs` | `string? Model` | Overrides provider default. Already supports arbitrary model names. |
| `AgentDefinition` | `src/OpenVEPA.Core/Agents/AgentDefinition.cs` | Contains `AgentLlmConfig? LlmConfig` | Part of agent definition record. |

**AgentLlmConfig** is already model-flexible. Agents can specify any model string. The constraint is upstream: `AgentChatClientFactory` resolves the provider instance by name, then the instance's `ModelId` determines the base client.

#### Layer 4: API DTOs

| Type | File | ModelId Field | Direction |
|------|------|---------------|-----------|
| `LlmProviderEntry` | `SystemApiExtensions.cs` | `string ModelId` | Response (GET) |
| `LlmMultiProviderResponse` | `SystemApiExtensions.cs` | Contains `IReadOnlyList<LlmProviderEntry>` | Response (GET) |
| `ProviderConfigInput` | `SystemApiExtensions.cs` | `string? ModelId` | Request (PUT) |
| `ValidatedProviderInstance` | `SystemApiExtensions.cs` | `string ModelId` | Internal validation |
| `UpdateLlmConfigRequest` | `SystemApiExtensions.cs` | Contains `List<ProviderConfigInput>` | Request (PUT) |

All use scalar `ModelId`. None support a list of models or an "allowed models" concept.

#### Layer 5: Setup Types

| Type | File | ModelId Field |
|------|------|---------------|
| `SetupConfiguration` | `src/OpenVEPA.Cli/Setup/SetupConfiguration.cs` | `string ModelId` |
| `SetupSubmission` | `src/OpenVEPA.Server/SetupEndpointExtensions.cs` | `string? ModelId` |

Both scalar. Setup wizard selects one model at configuration time.

#### Layer 6: Audit Types

| Type | File | Model Field | Notes |
|------|------|-------------|-------|
| `LlmAuditLogEntry` | `src/OpenVEPA.Core/Audit/LlmAuditLogEntry.cs` | `string Model` | Records the actual model used per invocation. |
| `LlmAuditLogEntity` | EF Core entity in `OpenVepaDbContext` | `string Model` | Persisted to `llm_audit_log` table. |

Audit is already model-correct. `TokenTrackingChatClient` records the model string passed at construction time. If the agent overrides the model, the `ConfiguredChatClient` sets `ChatOptions.ModelId`, but `TokenTrackingChatClient` still logs the provider-level model (not the override). This is a pre-existing issue.

### 4.2 Data Flow Diagram

```
CONFIG FILE (openvepa.conf)
    |
    | JSON bind via IConfiguration
    v
ProviderOptions (C# - IOptions<ProviderOptions>)
    |
    | List<ProviderInstanceOptions> Instances
    | Each instance has: Name, Type, Endpoint, ModelId (scalar), ApiKey
    |
    +---> ProviderServiceExtensions.RegisterDefault()
    |         |
    |         | Finds instance by DefaultProvider name
    |         | Calls CreateFromInstance(instance)
    |         v
    |     OllamaProvider.Create() / OpenAiProvider.Create()
    |         |
    |         | Model = instance.ModelId  <-- HARDWIRED HERE
    |         v
    |     TokenTrackingChatClient(innerClient, provider, model)
    |         |
    |         | Registered as IChatClient singleton (default)
    |         v
    |     DI Container: IChatClient (default)
    |
    +---> AgentChatClientFactory.GetChatClient(agent)
              |
              | agent.LlmConfig.Provider -> resolve instance by Name or Type
              | agent.LlmConfig.Model -> passed to ConfiguredChatClient
              |
              +--[no config]--> Return default IChatClient
              |
              +--[has config]-->  CreateClientForProvider(providerKey)
                                      |
                                      | Finds ProviderInstanceOptions by name/type
                                      | CreateFromInstance(instance) 
                                      |   Model = instance.ModelId  <-- HARDWIRED
                                      v
                                  TokenTrackingChatClient(inner, provider, instance.ModelId)
                                      |
                                      | Wrapped by ConfiguredChatClient if overrides exist
                                      v
                                  ConfiguredChatClient.MergeOptions()
                                      |
                                      | options.ModelId ??= agent.LlmConfig.Model
                                      | (overrides at ChatOptions level, not client level)
                                      v
                                  IChatClient returned to agent runtime
```

### 4.3 All Locations Assuming "One Provider = One Model"

| # | Location | File | What Assumes 1:1 | Severity |
|---|----------|------|------------------|----------|
| 1 | `ProviderInstanceOptions.ModelId` | `ProviderOptions.cs` | Scalar string, not list | **HIGH** - Core type |
| 2 | `OpenAiOptions.Model` | `ProviderOptions.cs` | Scalar string | MEDIUM - Legacy path |
| 3 | `OllamaOptions.Model` | `ProviderOptions.cs` | Scalar string | MEDIUM - Legacy path |
| 4 | `CreateFromInstance()` | `ProviderServiceExtensions.cs:120-146` | `Model = instance.ModelId` passed to provider factory | **HIGH** - Runtime binding |
| 5 | `OllamaProvider.Create()` | `OllamaProvider.cs:24-37` | `new OllamaApiClient(uri, options.Model)` - model baked into client | **HIGH** - Ollama SDK constraint |
| 6 | `OpenAiProvider.Create()` | `OpenAiProvider.cs:26-46` | `openAiClient.GetChatClient(options.Model)` - model baked into client | **HIGH** - OpenAI SDK constraint |
| 7 | `TokenTrackingChatClient` ctor | `TokenTrackingChatClient.cs` | `_model` field set at construction, logged for all calls | **HIGH** - Audit accuracy |
| 8 | `LlmProviderEntry` record | `SystemApiExtensions.cs:720` | `string ModelId` - scalar in API response | **HIGH** - API contract |
| 9 | `ProviderConfigInput` record | `SystemApiExtensions.cs` | `string? ModelId` - scalar in API request | **HIGH** - API contract |
| 10 | `ValidatedProviderInstance` | `SystemApiExtensions.cs` | `string ModelId` - scalar | MEDIUM - Internal |
| 11 | `ReadInstanceFormat()` | `SystemApiExtensions.cs:429-465` | Reads `instance["modelId"]` as scalar, rejects empty | **HIGH** - Config parse |
| 12 | `ReadLegacySectionFormat()` | `SystemApiExtensions.cs:467-496` | Reads `section["ModelId"]` as scalar | LOW - Legacy path |
| 13 | `WriteMultiProviderConfigurationAsync()` | `SystemApiExtensions.cs:596-696` | Writes `["modelId"] = p.ModelId` as scalar | **HIGH** - Config write |
| 14 | `BuildProvidersNode()` (server setup) | `SetupEndpointExtensions.cs:106-138` | `["modelId"] = modelId` scalar | MEDIUM - Setup only |
| 15 | `BuildProvidersNode()` (CLI setup) | `SetupConfigurationWriter.cs` | `["ModelId"] = config.ModelId` scalar | MEDIUM - Setup only |
| 16 | `SetupConfiguration.ModelId` | `SetupConfiguration.cs` | `string ModelId` scalar | LOW - Initial setup |
| 17 | `SetupSubmission.ModelId` | `SetupEndpointExtensions.cs` | `string? ModelId` scalar | LOW - Initial setup |
| 18 | `saveNewProvider()` JS | `DashboardPageHtml.cs:4395-4454` | `modelId: modelId` single value in provider object | **HIGH** - UI contract |
| 19 | `saveEditedProvider()` JS | `DashboardPageHtml.cs:4456-4507` | `modelId: modelId` single value update | **HIGH** - UI contract |
| 20 | `renderLlmConfigCard()` JS | `DashboardPageHtml.cs:3778-3955` | Shows single `p.modelId` per provider | **HIGH** - UI display |
| 21 | Provider dropdown in agent config | `DashboardPageHtml.cs:5352-5360` | Label: `cp.displayName + ' (' + cp.modelId + ')'` | MEDIUM - Cosmetic |
| 22 | PUT validation | `SystemApiExtensions.cs:530-534` | Rejects if `ModelId` is null/empty per provider | MEDIUM - Validation |
| 23 | AgentChatClientFactory cache key | `AgentChatClientFactory.cs:107-111` | Key includes model, but resolution uses instance ModelId for base client | LOW - Cache correctness |
| 24 | Agent removal check | `SystemApiExtensions.cs:573-588` | Checks `a.LlmConfig.Provider` against instance name to prevent deletion | LOW - Already instance-based |

**Total: 24 locations across 12 files.**

### 4.4 The ConfiguredChatClient Override Gap

There is a critical subtlety in how model overrides work today.

When an agent specifies `LlmConfig.Model = "gpt-4-turbo"` but the provider instance has `ModelId = "gpt-4o"`:

1. `CreateFromInstance()` creates an OpenAI client bound to `"gpt-4o"`.
2. `TokenTrackingChatClient` wraps it with `_model = "gpt-4o"` (audit logs this).
3. `ConfiguredChatClient` wraps that with `_modelId = "gpt-4-turbo"`.
4. At request time, `MergeOptions()` sets `ChatOptions.ModelId = "gpt-4-turbo"`.

For the OpenAI SDK, `ChatOptions.ModelId` overrides the client-level model. The actual API call uses `"gpt-4-turbo"`. But audit logs record `"gpt-4o"` because `TokenTrackingChatClient` was constructed with the instance model, not the runtime model.

For Ollama, `OllamaApiClient` constructor takes the model. It is unclear whether `ChatOptions.ModelId` overrides this at the HTTP level.

**This means the model override mechanism already partially works for OpenAI-compatible providers, but audit logging is incorrect.**

### 4.5 Provider SDK Constraints

| Provider SDK | Model Binding | Override via ChatOptions.ModelId? |
|-------------|---------------|-----------------------------------|
| OpenAI SDK (`OpenAIClient.GetChatClient(model)`) | Client-level | Yes - OpenAI SDK respects ChatOptions.ModelId |
| Ollama SDK (`OllamaApiClient(uri, model)`) | Client-level | Uncertain - needs verification |
| Google (via OpenAI compat endpoint) | Client-level | Yes - same as OpenAI |

This determines whether a single base client can serve multiple models or if a new client must be created per model.

## 5. Results

### 5.1 Quantified Impact Summary

| Dimension | Count |
|-----------|-------|
| C# types requiring change | 8 (ProviderInstanceOptions, OpenAiOptions, OllamaOptions, LlmProviderEntry, ProviderConfigInput, ValidatedProviderInstance, SetupConfiguration, SetupSubmission) |
| C# methods requiring change | 7 (CreateFromInstance, RegisterDefault, ReadInstanceFormat, ReadLegacySectionFormat, WriteMultiProviderConfigurationAsync, BuildProvidersNode x2) |
| API endpoints requiring change | 2 (GET /api/system/llm-config response, PUT /api/system/llm-config request) |
| JS functions requiring change | 6 (renderLlmConfigCard, renderLlmSettingsPanels, saveNewProvider, saveEditedProvider, renderAgentLlmTab label, fetchAgentModels) |
| Pre-existing bugs exposed | 1 (audit logs record provider-level model, not actual model used) |
| Files touched | 12 minimum |

### 5.2 What Already Works

1. **Multiple instances of same provider type**: The `Instances[]` array supports `[{name:"openai-work", type:"openai"}, {name:"openai-personal", type:"openai"}]`. No changes needed.

2. **Agent model override**: `AgentLlmConfig.Model` already lets agents specify any model. `ConfiguredChatClient.MergeOptions()` injects it into `ChatOptions.ModelId`. For OpenAI-compatible providers, this works at the API level.

3. **Model discovery**: `fetchProviderModels()` and `fetchAgentModels()` call `GET /api/system/models?provider=X` which returns the full list of models the provider supports. The dropdown already shows all available models.

4. **FallbackChatClient**: Fully implemented but not wired. Supports primary+fallback provider pairs.

### 5.3 What Does Not Work

1. **Provider instance configuration requires a model**: The PUT handler rejects providers without a `ModelId`. You cannot configure a provider without picking a specific model.

2. **Base client is model-bound**: `CreateFromInstance()` creates a client locked to `instance.ModelId`. Even if the agent overrides via `ChatOptions.ModelId`, the base client and audit trail are model-specific.

3. **Audit logs the wrong model**: `TokenTrackingChatClient` captures the model from construction time, not from the actual `ChatOptions.ModelId` used in the request.

4. **No "allowed models" concept**: Nothing validates whether an agent's model override is valid for the provider. An agent could request `model: "nonexistent-model"` and the error surfaces only at API call time.

5. **Dashboard UI shows one model per provider**: The provider card, the agent dropdown label, and the save payloads all treat `modelId` as a scalar.

## 6. Discussion

### 6.1 Semantic Shift: ModelId Becomes DefaultModelId

The minimal viable change reinterprets the existing `ModelId` field as "default model" rather than "the model." This preserves backward compatibility while enabling the multi-model behavior.

**Config impact**: Add optional `AllowedModels: string[]` to `ProviderInstanceOptions`. If null or empty, any model is allowed (open-ended providers like Ollama). If populated, acts as a whitelist.

**Rename**: `ModelId` to `DefaultModelId` throughout all layers. This is a breaking change to the config file format and API contract. A migration path is required (accept both `modelId` and `defaultModelId` during reads, write `defaultModelId`).

### 6.2 Runtime Resolution Change

Current: `CreateFromInstance()` creates one client per provider instance, model baked in.

Proposed: Two options.

**Option A: Create client per model (cache).**
- `AgentChatClientFactory` creates a new client per unique `(provider, model)` pair.
- Each client wraps the provider with the specific model.
- Cache key already includes model: `"{provider}|{model}|{temp}|{maxTokens}"`.
- Audit logging is correct because each client has the right model.
- Cost: More client instances. For OpenAI, each `GetChatClient(model)` creates a new REST client.

**Option B: Use ChatOptions.ModelId override (current behavior, fix audit).**
- Keep one base client per provider instance.
- Rely on `ChatOptions.ModelId` to override at request time.
- Fix `TokenTrackingChatClient` to read model from `ChatOptions` instead of constructor param.
- Cost: Depends on SDK support for runtime model override. Ollama SDK needs verification.

Option A is safer and SDK-agnostic. Option B is more efficient but SDK-dependent.

### 6.3 Audit Fix Required Either Way

`TokenTrackingChatClient` must log the actual model used, not the default model. Change:

```csharp
// Current: logs _model (from constructor)
var entry = new LlmAuditLogEntry(..., Model: _model, ...);

// Proposed: logs actual model from response or options
var actualModel = response.ModelId ?? options?.ModelId ?? _model;
var entry = new LlmAuditLogEntry(..., Model: actualModel, ...);
```

### 6.4 Backward Compatibility Matrix

| Component | Breaking Change? | Migration Path |
|-----------|-----------------|----------------|
| Config file (openvepa.conf) | Yes - field rename | Accept `modelId` OR `defaultModelId` during reads. Write `defaultModelId` + optional `allowedModels`. |
| API contract (GET/PUT /llm-config) | Yes - response shape | Version the API or add `defaultModelId` alongside `modelId` temporarily. |
| Agent YAML (openvepa-llm) | No change needed | `provider` + `model` fields already flexible. |
| Agent config preferences (SQLite) | No change needed | JSON serialization of AgentLlmConfig is unchanged. |
| Audit log table | No change needed | `Model` column already stores the actual model string. |
| Dashboard JS | Yes - UI changes | Provider card shows default + allowed models. Agent config shows model selector from allowed list. |
| Setup wizard (CLI + Web) | Minor | Initial setup still picks one default model. Can add allowed models later via dashboard. |

## 7. Recommendations

| Priority | Recommendation | Rationale | Effort |
|----------|----------------|-----------|--------|
| P0 | Rename `ModelId` to `DefaultModelId` in `ProviderInstanceOptions` and all downstream types | Establishes correct semantics. Every other change depends on this. | Medium (24 locations, 12 files) |
| P0 | Add `List<string>? AllowedModels` to `ProviderInstanceOptions` | Enables whitelist of models per provider instance. Null = all models allowed. | Low (1 type + config read/write) |
| P0 | Fix `TokenTrackingChatClient` to log actual model from `ChatOptions` or response | Pre-existing bug. Audit data is incorrect when agents override models. | Low (1 file) |
| P1 | Refactor `AgentChatClientFactory` to cache per `(provider, model)` pair (Option A) | Ensures correct client creation per model without SDK dependency assumptions. | Medium (1 file, affects cache key logic) |
| P1 | Update `LlmProviderEntry` API response to include `defaultModelId` + `allowedModels` | Frontend needs both to render provider settings and agent model selectors. | Medium (API + JS) |
| P1 | Update dashboard provider settings to show/edit default model + allowed models list | Users need to configure which models are available per provider. | High (embedded JS, 4 functions) |
| P1 | Update dashboard agent LLM tab to show model dropdown filtered by provider's allowed models | Agents should select from allowed models, not just free-text. | Medium (embedded JS, 2 functions) |
| P2 | Add config migration logic for `modelId` to `defaultModelId` | Backward compatibility with existing installations. | Low (config reader) |
| P2 | Verify Ollama SDK behavior with `ChatOptions.ModelId` override | Determines if Option B is viable for Ollama. | Low (test) |
| P2 | Wire `FallbackChatClient` into `AgentChatClientFactory` | Already implemented, just needs DI wiring. Agent fallback config already in dashboard. | Medium |

## 8. Conclusion

**Verdict**: Proceed with refactor. The architecture supports the change with moderate effort.

**Confidence**: High

**Rationale**: The codebase already partially supports multi-model behavior. `AgentLlmConfig.Model` overrides work for OpenAI-compatible providers via `ChatOptions.ModelId`. The primary gaps are (1) the 1:1 semantic in config types, (2) incorrect audit logging, and (3) dashboard UI showing one model per provider. No architectural redesign is required. The change is additive: rename a field, add an optional list, fix the audit decorator, update the UI.

### User Impact

- **What changes for you**: Provider instances become "provider connections" that serve multiple models. Default model still works as before. Agent config gains a validated model dropdown instead of free-text.
- **Effort required**: 12 files, 24 code locations. Estimated 2-3 focused implementation sessions.
- **Risk if ignored**: Agents silently use the wrong model when overrides fail. Audit data misattributes token usage to the wrong model. Users create duplicate provider instances just to access different models on the same subscription.

## 9. Appendices

### Appendix A: Complete File Inventory

| File | Role | Change Required |
|------|------|-----------------|
| `src/OpenVEPA.Providers/ProviderOptions.cs` | Core config types | Rename ModelId, add AllowedModels |
| `src/OpenVEPA.Providers/ProviderServiceExtensions.cs` | DI registration + CreateFromInstance | Update model resolution |
| `src/OpenVEPA.Providers/AgentChatClientFactory.cs` | Runtime factory | Update cache key + client creation |
| `src/OpenVEPA.Providers/TokenTrackingChatClient.cs` | Audit decorator | Log actual model from ChatOptions |
| `src/OpenVEPA.Providers/ConfiguredChatClient.cs` | Agent override decorator | No change (already works) |
| `src/OpenVEPA.Providers/OpenAiProvider.cs` | OpenAI factory | Minor - accept default model param |
| `src/OpenVEPA.Providers/OllamaProvider.cs` | Ollama factory | Minor - accept default model param |
| `src/OpenVEPA.Providers/FallbackChatClient.cs` | Fallback decorator | No change (wire later) |
| `src/OpenVEPA.Core/Agents/AgentLlmConfig.cs` | Agent LLM config | No change |
| `src/OpenVEPA.Core/Agents/AgentDefinition.cs` | Agent definition | No change |
| `src/OpenVEPA.Core/Audit/LlmAuditLogEntry.cs` | Audit record | No change |
| `src/OpenVEPA.Server/Api/SystemApiExtensions.cs` | LLM config API | Update DTOs + read/write + validation |
| `src/OpenVEPA.Server/SetupEndpointExtensions.cs` | Web setup | Update BuildProvidersNode |
| `src/OpenVEPA.Server/DashboardPageHtml.cs` | Dashboard UI | Update 6 JS functions |
| `src/OpenVEPA.Server/ModelListProxy.cs` | Model list fetcher | No change |
| `src/OpenVEPA.Cli/Setup/SetupConfiguration.cs` | CLI setup record | Rename ModelId |
| `src/OpenVEPA.Cli/Setup/SetupConfigurationWriter.cs` | CLI config writer | Update BuildProvidersNode |
| `src/OpenVEPA.Cli/Setup/SetupWizardTui.cs` | CLI setup wizard | Minor - model is still "default" |
| `src/OpenVEPA.Storage/SqliteLlmAuditLogger.cs` | Audit persistence | No change |

### Appendix B: Agent LLM Config Persistence Flow

```
YAML frontmatter (*.agent.md)     Dashboard (PUT /api/agents/{name}/config)
        |                                    |
        | AgentMdParser.ParseLlmConfig()     | JSON -> UserPreferences DB
        v                                    v
AgentLlmConfig record              Stored in SQLite key: "agent-config:{name}"
        |                                    |
        +---> AgentDefinition.LlmConfig <----+  (merge at runtime - TODO: verify)
                    |
                    v
        AgentChatClientFactory.GetChatClient(agent)
                    |
                    v
        ConfiguredChatClient(base, model, temp, maxTokens)
```

Note: The GET /api/agents/{name} endpoint returns the agent definition directly from `AgentDirectory.DiscoverAgents()`, which reads from `.agent.md` files. The dashboard PUT saves to user preferences. It is unclear whether runtime merges file-based and preference-based configs or if preferences fully override file-based config.

### Appendix C: Data Transparency

- **Found**: Complete type definitions, all 24 1:1 assumption locations, full data flow from config to runtime to audit, dashboard JS function bodies, API endpoint handlers, setup wizard code.
- **Not Found**: (1) Whether Ollama SDK respects ChatOptions.ModelId at runtime. (2) How user preferences merge with file-based agent definitions at runtime (no merge code found in AgentDirectory). (3) Whether the OpenAI SDK's `GetChatClient(model)` supports model switching via ChatOptions (documented but not tested).
