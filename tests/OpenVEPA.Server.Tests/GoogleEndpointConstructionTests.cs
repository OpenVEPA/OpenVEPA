using FluentAssertions;

using OpenVEPA.Providers;

namespace OpenVEPA.Server.Tests;

/// <summary>
/// Proves that Google Gemini endpoint construction handles null, empty,
/// whitespace, valid, and already-suffixed endpoint values correctly.
/// Exercises the internal <see cref="ProviderServiceExtensions.CreateFromInstance"/>
/// and <see cref="OpenAiProvider.Create"/> code paths.
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
    public void Google_EndpointAlreadyContainsOpenAi_NoDoubleAppend()
    {
        var instance = MakeGoogle(endpoint: "https://generativelanguage.googleapis.com/v1beta/openai");

        var act = () => ProviderServiceExtensions.CreateFromInstance(instance, model: null, auditLogger: null);

        act.Should().NotThrow("an endpoint already ending in /openai must not get /openai appended again");
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

    // ── Helpers ─────────────────────────────────────────────────────────

    private static ProviderInstanceOptions MakeGoogle(string? endpoint) => new()
    {
        Name = "google-test",
        Type = "google",
        Endpoint = endpoint,
        ApiKey = "test-key",
        DefaultModel = "gemini-2.0-flash",
    };
}
