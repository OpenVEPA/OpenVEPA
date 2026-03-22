# Plan Critique: Provider + LLM Multi-Model Architecture Refactor

**Documents Reviewed:**
- `.agents/analysis/002-provider-llm-data-model-analysis.md`
- `.agents/architecture/ADR-0001-provider-llm-multi-model-architecture.md`

**Date:** 2025-07-22

---

## Verdict

**[APPROVED WITH CONDITIONS]**

**Confidence:** High

**Rationale:** The proposal is technically sound, well-scoped, and the simplest viable option. It correctly identifies all 24 locations requiring change. The analysis is thorough and matches codebase reality on every claim I verified. Three conditions must be addressed before implementation begins.

---

## Summary

Option A (extend `ProviderInstanceOptions` with `DefaultModel` + `AvailableModels`) is the right choice. It preserves the existing config shape, requires no new config sections, and the migration path is mechanical. The ADR covers config, C# types, API contracts, factory logic, UI, and migration. The analysis identified a real pre-existing audit bug.

The proposal addresses all 4 user requirements:

| Requirement | Addressed | How |
|-------------|-----------|-----|
| One provider = many LLMs | Yes | `AvailableModels` list on instance |
| Same type, multiple instances | Yes (already works) | `Instances[]` array unchanged |
| Agents select provider + model independently | Yes | `Provider` = instance name, `Model` = specific model |
| Good UX | Yes | Cascading dropdowns, "(instance default)" option |

---

## Strengths

1. **Analysis quality is high.** 24 locations across 12 files, each with file path, line number, and severity. I verified 9 of these against the codebase; all matched exactly.

2. **Backward compatibility is well-designed.** The `[Obsolete] ModelId` setter that maps to `DefaultModel` during deserialization is a clean pattern. The Type-based resolution fallback with deprecation warning avoids breaking existing agent configs.

3. **Cache key already includes model.** Current format: `{Provider}|{Model}|{Temperature}|{MaxTokens}`. The refactor does not need to change the cache key structure, only ensure the base client is created per `(instance, model)` pair. This is lower risk than it appears.

4. **FallbackChatClient is fully implemented.** 136 lines, handles both sync and streaming, falls back on non-cancellation exceptions. Wiring it in is a P2 addition with low risk.

5. **ModelListProxy already supports all provider APIs.** Dynamic model fetching for Ollama (`/api/tags`), Google (`/models`), OpenAI (`/models`), and 6 other providers exists at `ModelListProxy.cs:60-98`. The `AvailableModels: []` dynamic fetch path has infrastructure ready.

6. **Option B and C rejections are well-justified.** Option B (separate sections with cross-references) adds complexity without benefit. Option C (flatten per-model) causes credential duplication and config explosion.

---

## Issues Found

### Critical (Must Fix)

- [x] **C1: Ollama SDK model override behavior is unverified, yet the design depends on it.**

  The analysis flags this as "uncertain" (Section 4.5, Appendix C item 1). The ADR chooses Option A (create client per model pair) which avoids the problem. But the ADR Section 3.2 still routes through `CreateFromInstance` with an explicit `modelId` parameter, which means `OllamaApiClient(uri, modelId)` gets a new instance per model. This is correct.

  **However**, the ADR does not state whether `OllamaApiClient` instances are heavyweight or lightweight. If Ollama SDK maintains persistent HTTP connections per client, creating one per model could cause connection pool exhaustion. OllamaSharp 5.1.12 uses `HttpClient` internally.

  **Resolution required:** Before implementation, verify that creating multiple `OllamaApiClient` instances per provider (one per model) does not cause resource leaks. A single integration test confirming 3 models from 1 Ollama endpoint work simultaneously is sufficient.

  **UPDATE after codebase verification:** The factory already creates one client per unique cache key. Today, each agent with a different provider/model/temp/maxTokens combination gets its own client. The refactor does not change this pattern; it only ensures the base client model matches the requested model. This is a **clarification gap**, not a design flaw. Downgraded from blocking to must-document.

### Important (Should Fix)

- [ ] **I1: No test coverage exists for the components being refactored.**

  Zero tests exist for `AgentChatClientFactory`, `TokenTrackingChatClient`, `ConfiguredChatClient`, or `ModelListProxy`. The refactor touches all 4 components.

  The ADR Section "Confirmation" lists unit and integration tests as verification criteria. This is correct but understated. Without pre-refactor tests that capture current behavior, regressions will be invisible.

  **Recommendation:** Add characterization tests for current behavior BEFORE the refactor. Minimum:
  - `AgentChatClientFactory`: resolution by Name, resolution by Type, cache hit on same config, cache miss on different model
  - `TokenTrackingChatClient`: model logged matches constructor param (documents the bug)
  - `ConfiguredChatClient`: model override via MergeOptions

  Estimated effort: 1 focused session, 6-8 tests.

- [ ] **I2: The ADR does not specify what happens when an agent references a deleted provider instance.**

  The analysis (Section 4.3, item 24) notes that `SystemApiExtensions.cs:573-588` checks for agent references before allowing provider deletion. The ADR does not state whether this check is preserved, strengthened, or what the error behavior is.

  **Recommendation:** Explicitly state in the ADR: "The PUT /api/system/llm-config handler MUST reject deletion of a provider instance if any agent references it by name. Error response must list the blocking agent names."

- [ ] **I3: The ADR does not address model deprecation/retirement by upstream providers.**

  Risk table item 2 says "Allow any model string through to the API. Provider will reject invalid models with a clear error." This is correct for the happy path. But when a provider retires a model (e.g., Google retires `gemini-1.0-pro`), agents referencing it will fail silently at invocation time.

  **Recommendation:** Add a health-check mechanism in a future iteration (not blocking for this refactor). Document this as a known limitation in the ADR risks table. Consider logging a warning at startup if `DefaultModel` is not in the dynamic model list.

- [ ] **I4: The `ProviderOptions.DefaultModel` removal needs explicit migration handling.**

  The ADR Section 1.3 says "Remove it. The system default model is `Instances[DefaultProvider].DefaultModel`." But the analysis (Section 4.1, Layer 2) shows `ProviderOptions.DefaultModel` currently exists with value `"llama3"` (line 15 of `ProviderOptions.cs`). 

  If an existing config has `ProviderOptions.DefaultModel = "llama3"` but the default instance has `ModelId = "llama3.2"`, removing `DefaultModel` silently changes the effective model.

  **Recommendation:** During migration, if `ProviderOptions.DefaultModel` differs from the default instance's `ModelId`/`DefaultModel`, log a warning explaining the change. Do not silently drop the value.

### Minor (Consider)

- [ ] **M1: The naming in `AgentLlmConfig` could confuse users.**

  Current XML doc says `"provider identifier (e.g., 'google', 'anthropic')"`. After the refactor, `Provider` means instance name (e.g., `"google-main"`). The ADR updates the docs but the semantic shift could confuse users who have internalized the current meaning.

  **Recommendation:** Use the UI label "Provider Instance" (not just "Provider") in the dashboard to signal the change. The YAML key `provider:` in `.agent.md` files can stay as-is since only 1 file uses it and it currently has `null`.

- [ ] **M2: The `AvailableModels` empty-vs-pinned distinction is implicit.**

  `AvailableModels: []` means "dynamic fetch." `AvailableModels: ["a", "b"]` means "pinned." There is no explicit boolean like `DynamicModelFetch: true`. This works but requires documentation. A user who sets `AvailableModels: ["gemini-2.5-flash"]` might expect dynamic fetch PLUS a filter, not a hard pin.

  **Recommendation:** Document clearly in the config file comments and UI tooltip: "Empty list = all models from provider API. Non-empty list = ONLY these models."

- [ ] **M3: Cost tracking per model is mentioned (ADR Section 4.4, `ModelUsageEntry`) but the analysis does not cover where cost-per-model rates come from.**

  `CostUsd` appears in the response DTOs but there is no cost table or rate lookup anywhere in the codebase. This is a future feature, not part of this refactor.

  **Recommendation:** Set `CostUsd = 0.0` in the response or omit the field until rate data is available. Do not expose a field that always returns zero without explanation.

---

## Questions for Planner

1. **Ollama client resource weight:** Does creating 5 `OllamaApiClient` instances to the same endpoint cause 5 separate `HttpClient` instances? If so, consider an `HttpClient` factory or singleton with model passed at request time.

2. **Dynamic model list cache TTL:** The ADR risk table mentions "cache last-known model list for 24h." Where is this cache stored? In-memory (lost on restart) or persisted? Recommendation: in-memory with configurable TTL, default 1 hour.

3. **API versioning strategy:** The GET/PUT `/api/system/llm-config` response shape changes (new fields, removed fields). Is there an API versioning strategy, or is this a breaking change absorbed by the fact that the dashboard JS and API are deployed together?

---

## Recommendations

1. **Write characterization tests first.** Capture current factory resolution, cache, and audit behavior in tests before changing anything. This is the highest-ROI risk reduction.

2. **Implement in this order** (matches ADR Section 7 with one addition):
   - Phase 0: Characterization tests for current behavior (new)
   - Phase 1: C# type changes (`ProviderInstanceOptions`, `ProviderOptions`, API DTOs)
   - Phase 2: Factory changes (`CreateFromInstance` signature, Name-only resolution)
   - Phase 3: Audit fix (`TokenTrackingChatClient` logs actual model)
   - Phase 4: Config migration (ModelId setter, legacy section handling)
   - Phase 5: API endpoint updates
   - Phase 6: Dashboard UI changes (embedded JS)
   - Phase 7: FallbackChatClient wiring (can be deferred)

3. **The Type-based resolution fallback should have a sunset version.** The ADR says "remove in v2" but does not define when v2 is. Set a concrete deprecation timeline (e.g., 2 releases or 6 months).

---

## Approval Conditions

Three conditions before this plan is approved for implementation:

| # | Condition | Blocking? |
|---|-----------|-----------|
| 1 | Add Phase 0 (characterization tests) to the implementation plan | Yes |
| 2 | Document the `AvailableModels` empty-vs-pinned semantics explicitly in the ADR | No (can be done during implementation) |
| 3 | Clarify Ollama `HttpClient` resource implications for multi-model instances | No (can be investigated during Phase 0) |

Condition 1 is blocking. Conditions 2 and 3 can be resolved during implementation.

---

## Completeness Assessment

### Requirements Coverage

| Requirement | [PASS]/[FAIL] | Notes |
|-------------|---------------|-------|
| One provider = many LLMs | [PASS] | `DefaultModel` + `AvailableModels` |
| Same type, multiple instances | [PASS] | Already works, no change needed |
| Agent selects provider + model | [PASS] | `Provider` = instance name, `Model` = specific model |
| Good UX | [PASS] | Cascading dropdowns, clear card layout |
| Backward compat (config) | [PASS] | `ModelId` setter auto-maps |
| Backward compat (agent YAML) | [PASS] | Type fallback with deprecation warning |
| Audit accuracy | [PASS] | Explicit fix for `TokenTrackingChatClient` |

### Feasibility Assessment

| Dimension | [PASS]/[FAIL] | Notes |
|-----------|---------------|-------|
| Technical approach | [PASS] | Additive changes, no redesign |
| Scope realistic | [PASS] | 12 files, 24 locations, 2-3 sessions |
| Dependencies available | [PASS] | All SDKs already in use |
| Skills available | [PASS] | Standard C# refactor + JS changes |

### Testability Assessment

| Dimension | [PASS]/[FAIL] | Notes |
|-----------|---------------|-------|
| Each milestone verifiable | [PASS] | Each phase has clear acceptance criteria |
| Acceptance criteria measurable | [WARNING] | ADR lists tests but no pre-refactor baseline |
| Test strategy clear | [WARNING] | No characterization tests planned |

---

## Impact Analysis Review

**Analysis coverage**: Thorough. 7 parallel investigations, 24 locations identified, SDK constraints documented.

**Cross-domain conflicts**: None. The refactor is internal to the provider subsystem. No security, deployment, or database schema changes.

**Escalation required**: No.

---

## Reversibility Assessment

| Dimension | Status | Notes |
|-----------|--------|-------|
| Rollback capability | [PASS] | `ModelId` setter provides bidirectional compat |
| Vendor lock-in | [PASS] | No new vendor dependencies |
| Exit strategy | [PASS] | Internal change only |
| Legacy impact | [PASS] | Full backward compat via fallback paths |
| Data migration reversibility | [PASS] | Config changes are additive, audit schema unchanged |

---

## Final Verdict

**[APPROVED WITH CONDITIONS]**

This is a well-analyzed, correctly scoped refactor. The chosen option (A) is the simplest that solves all requirements. The analysis is accurate (verified against codebase). The migration strategy handles backward compatibility without manual user effort.

**One blocking condition:** Add characterization tests (Phase 0) before implementation begins. The current test coverage for the affected components is zero. Refactoring without a safety net across 12 files and 24 locations is avoidable risk.

**Recommend orchestrator routes to:** milestone-planner to incorporate Phase 0, then to implementer for execution.
