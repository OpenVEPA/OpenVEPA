# ADR-0001: Provider-LLM Multi-Model Architecture

**Status**: Proposed
**Date**: 2025-07-22
**File**: `.agents/architecture/ADR-0001-provider-llm-multi-model-architecture.md`

## Decision

Refactor `ProviderInstanceOptions` to replace single `ModelId` with `DefaultModel` + `AvailableModels` list. One provider instance exposes many models. Agents reference instances by Name (not Type).

## Key Changes
- `ProviderInstanceOptions.ModelId` -> `DefaultModel` + `AvailableModels: List<string>`
- `AvailableModels = []` means fetch dynamically from provider API
- `AgentLlmConfig.Provider` semantic change: instance name, not provider type
- New `FallbackProvider` + `FallbackModel` on AgentLlmConfig
- Factory resolves by instance Name only (Type fallback deprecated)
- `CreateFromInstance` takes explicit `modelId` parameter
- API: `LlmProviderEntry` gains `AvailableModels` and `SupportsModelListing`
- WebUI: cascading dropdowns (provider instance -> model list)

## Files Affected
- `src/OpenVEPA.Providers/ProviderOptions.cs`
- `src/OpenVEPA.Core/Agents/AgentLlmConfig.cs`
- `src/OpenVEPA.Providers/AgentChatClientFactory.cs`
- `src/OpenVEPA.Providers/ProviderServiceExtensions.cs`
- `src/OpenVEPA.Server/Api/SystemApiExtensions.cs`
- `src/OpenVEPA.Server/DashboardPageHtml.cs`
- `src/OpenVEPA.Agents.Runtime/AgentMdParser.cs`

## Migration
- `ModelId` setter maps to `DefaultModel` (backward compat)
- Type-based resolution kept as deprecated fallback with warning log
- Legacy config sections auto-synthesize to Instances
