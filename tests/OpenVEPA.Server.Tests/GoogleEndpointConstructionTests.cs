using FluentAssertions;

using Microsoft.Extensions.AI;

using OpenVEPA.Providers;

namespace OpenVEPA.Server.Tests;

/// <summary>
/// Proves that Google Gemini endpoint construction handles null, empty,
/// whitespace, valid, and legacy /openai-suffixed endpoint values correctly.
/// Exercises <see cref="GeminiProvider.Create"/> and
/// <see cref="ProviderServiceExtensions.CreateFromInstance"/> code paths.
/// </summary>
public sealed class GoogleEndpointConstructionTests
{
    // ── Google endpoint via CreateFromInstance ──────────────────────────

    [Fact]
    public void Google_NullEndpoint_UsesDefault_DoesNotThrow()
    {
        var instance = MakeGoogle(endpoint: null);

        var act = () => ProviderServiceExtensions.CreateFromInstance(instance, model: null, auditLogger: null);

        act.Should().NotThrow("a null endpoint should fall back to the Google default");
    }

    [Fact]
    public void Google_EmptyStringEndpoint_UsesDefault_DoesNotThrow()
    {
        var instance = MakeGoogle(endpoint: "");

        var act = () => ProviderServiceExtensions.CreateFromInstance(instance, model: null, auditLogger: null);

        act.Should().NotThrow("an empty-string endpoint was the exact bug (404) and must fall back to the default");
    }

    [Fact]
    public void Google_WhitespaceEndpoint_UsesDefault_DoesNotThrow()
    {
        var instance = MakeGoogle(endpoint: "  ");

        var act = () => ProviderServiceExtensions.CreateFromInstance(instance, model: null, auditLogger: null);

        act.Should().NotThrow("whitespace-only endpoint should be treated as absent and use the default");
    }

    [Fact]
    public void Google_ValidCustomEndpoint_UsesProvided_DoesNotThrow()
    {
        var instance = MakeGoogle(endpoint: "https://custom-google.example.com/v1beta");

        var act = () => ProviderServiceExtensions.CreateFromInstance(instance, model: null, auditLogger: null);

        act.Should().NotThrow("a valid custom endpoint must be accepted");
    }

    [Fact]
    public void Google_EndpointWithOpenAiSuffix_StripsItForNativeApi()
    {
        var instance = MakeGoogle(endpoint: "https://generativelanguage.googleapis.com/v1beta/openai");

        var client = ProviderServiceExtensions.CreateFromInstance(instance, model: null, auditLogger: null);

        // The legacy /openai suffix must be stripped for the native Gemini REST API.
        var inner = GetInnerGeminiMetadata(client);
        inner.Should().NotBeNull();
        inner!.ProviderUri!.ToString().Should().NotContain("/openai",
            "the native Gemini provider must strip the legacy /openai suffix");
    }

    // ── OpenAiProvider.Create edge cases ───────────────────────────────

    [Fact]
    public void OpenAiProvider_RelativeUri_ThrowsInvalidOperation()
    {
        var options = new OpenAiOptions
        {
            ApiKey = "test-key",
            Model = "gpt-4o",
            Endpoint = "/openai",
        };

        var act = () => OpenAiProvider.Create(options, auditLogger: null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid endpoint URL*");
    }

    [Fact]
    public void OpenAiProvider_EmptyStringEndpoint_UsesDefault_DoesNotThrow()
    {
        var options = new OpenAiOptions
        {
            ApiKey = "test-key",
            Model = "gpt-4o",
            Endpoint = "",
        };

        var act = () => OpenAiProvider.Create(options, auditLogger: null);

        act.Should().NotThrow("an empty endpoint string should be treated as absent (SDK default)");
    }

    // ── Generic (OpenAI-type) provider via CreateFromInstance ──────────

    [Fact]
    public void GenericOpenAi_EmptyEndpoint_UsesDefault_DoesNotThrow()
    {
        var instance = new ProviderInstanceOptions
        {
            Name = "openai-test",
            Type = "openai",
            Endpoint = "",
            ApiKey = "test-key",
            DefaultModel = "gpt-4o",
        };

        var act = () => ProviderServiceExtensions.CreateFromInstance(instance, model: null, auditLogger: null);

        act.Should().NotThrow("an empty endpoint for a generic OpenAI provider must map to null (SDK default)");
    }

    // ── GeminiProvider.Create metadata verification ────────────────────

    [Fact]
    public void GeminiProviderStripsOpenAiSuffix()
    {
        var client = GeminiProvider.Create(
            "test-key", "gemini-2.5-flash",
            "https://generativelanguage.googleapis.com/v1beta/openai");

        var metadata = client.GetService<ChatClientMetadata>();

        metadata.Should().NotBeNull();
        metadata!.ProviderName.Should().Be("google-gemini");
        metadata!.ProviderUri!.ToString().Should().NotContain("/openai",
            "the legacy /openai suffix must be stripped for native REST API");
        metadata!.ProviderUri!.ToString().Should().Contain("generativelanguage.googleapis.com/v1beta");
    }

    [Fact]
    public void GeminiProviderUsesDefaultEndpoint()
    {
        var client = GeminiProvider.Create("test-key", "gemini-2.5-flash");

        var metadata = client.GetService<ChatClientMetadata>();

        metadata.Should().NotBeNull();
        metadata!.ProviderUri.Should().NotBeNull();
        metadata!.ProviderUri!.ToString().Should().Contain("generativelanguage.googleapis.com");
    }

    [Fact]
    public void GeminiProviderHandlesTrailingSlash()
    {
        var client = GeminiProvider.Create(
            "test-key", "gemini-2.5-flash",
            "https://generativelanguage.googleapis.com/v1beta/");

        var metadata = client.GetService<ChatClientMetadata>();

        metadata!.ProviderUri!.ToString().Should().NotEndWith("//");
    }

    [Fact]
    public void GeminiProviderThrowsOnMissingApiKey()
    {
        var act = () => GeminiProvider.Create("", "gemini-2.5-flash");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*API key*");
    }

    [Fact]
    public void GeminiProviderStripsOpenAiSuffixWithTrailingSlash()
    {
        var client = GeminiProvider.Create(
            "test-key", "gemini-2.5-flash",
            "https://generativelanguage.googleapis.com/v1beta/openai/");

        var metadata = client.GetService<ChatClientMetadata>();

        metadata!.ProviderUri!.ToString().Should().NotContain("/openai",
            "the legacy /openai/ suffix (with trailing slash) must also be stripped");
    }

    [Fact]
    public void GeminiProviderPreservesCustomEndpoint()
    {
        var client = GeminiProvider.Create(
            "test-key", "gemini-2.5-flash",
            "https://custom-google.example.com/v1beta");

        var metadata = client.GetService<ChatClientMetadata>();

        metadata!.ProviderUri!.ToString().Should().Contain("custom-google.example.com");
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static ProviderInstanceOptions MakeGoogle(string? endpoint) => new()
    {
        Name = "google-test",
        Type = "google",
        Endpoint = endpoint,
        ApiKey = "test-key",
        DefaultModel = "gemini-2.5-flash",
    };

    /// <summary>
    /// Extracts <see cref="ChatClientMetadata"/> from the inner GeminiProvider,
    /// unwrapping the TokenTrackingChatClient wrapper that CreateFromInstance adds.
    /// </summary>
    private static ChatClientMetadata? GetInnerGeminiMetadata(IChatClient client)
    {
        // CreateFromInstance wraps GeminiProvider in TokenTrackingChatClient.
        // Try getting metadata from the inner client via GetService.
        return client.GetService<ChatClientMetadata>();
    }
}
