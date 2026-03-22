using System.Text.Json;
using System.Text.Json.Nodes;

using FluentAssertions;

using OpenVEPA.Cli.Setup;

namespace OpenVEPA.Cli.Tests.Setup;

public sealed class SetupConfigurationWriterTests : IDisposable
{
    private readonly string _tempDir;

    public SetupConfigurationWriterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"openvepa-test-{Guid.NewGuid():N}");
    }

    [Fact]
    public async Task WriteAsync_Creates_Home_Directory()
    {
        var config = CreateOllamaConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        Directory.Exists(_tempDir).Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_Creates_Data_Directory()
    {
        var config = CreateOllamaConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        Directory.Exists(Path.Combine(_tempDir, "data")).Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_Creates_Documents_Directory()
    {
        var config = CreateOllamaConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        Directory.Exists(Path.Combine(_tempDir, "data", "documents")).Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_Creates_Skills_Directory()
    {
        var config = CreateOllamaConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        Directory.Exists(Path.Combine(_tempDir, "skills")).Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_Creates_Agents_Directory()
    {
        var config = CreateOllamaConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        Directory.Exists(Path.Combine(_tempDir, "agents")).Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_Creates_Logs_Directory()
    {
        var config = CreateOllamaConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        Directory.Exists(Path.Combine(_tempDir, "logs")).Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_Creates_AppSettings_File()
    {
        var config = CreateOllamaConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        File.Exists(Path.Combine(_tempDir, "openvepa.conf")).Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_Writes_Valid_Json()
    {
        var config = CreateOllamaConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var json = await File.ReadAllTextAsync(Path.Combine(_tempDir, "openvepa.conf"));
        var act = () => JsonDocument.Parse(json);

        act.Should().NotThrow("the written file must be valid JSON");
    }

    [Fact]
    public async Task WriteAsync_Json_Contains_Correct_Provider()
    {
        var config = CreateOllamaConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        root["Providers"]?["DefaultProvider"]?.GetValue<string>()
            .Should().Be("ollama");
    }

    [Fact]
    public async Task WriteAsync_Json_Contains_Correct_Model()
    {
        var config = CreateOllamaConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        root["Providers"]?["Ollama"]?["ModelId"]?.GetValue<string>()
            .Should().Be("llama3.2");
    }

    [Fact]
    public async Task WriteAsync_Json_Contains_Kestrel_Endpoint_With_Correct_Port()
    {
        var config = CreateOllamaConfig(_tempDir) with { Port = 8080 };

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        root["Kestrel"]?["Endpoints"]?["Http"]?["Url"]?.GetValue<string>()
            .Should().Be("http://localhost:8080");
    }

    [Fact]
    public async Task WriteAsync_Ollama_Config_Has_Ollama_Section()
    {
        var config = CreateOllamaConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        var ollama = root["Providers"]?["Ollama"];
        ollama.Should().NotBeNull();
        ollama?["Endpoint"]?.GetValue<string>().Should().Be("http://localhost:11434");
        ollama?["ModelId"]?.GetValue<string>().Should().Be("llama3.2");
    }

    [Fact]
    public async Task WriteAsync_OpenAI_Config_Has_OpenAI_Section_With_ApiKey()
    {
        var config = CreateOpenAiConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        var openAi = root["Providers"]?["OpenAi"];
        openAi.Should().NotBeNull();
        openAi?["ApiKey"]?.GetValue<string>().Should().Be("sk-test-key");
        openAi?["ModelId"]?.GetValue<string>().Should().Be("gpt-4o");
        openAi?["Endpoint"]?.GetValue<string>().Should().Be("https://api.openai.com/v1");
    }

    [Fact]
    public async Task WriteAsync_Json_Contains_Scheduler_Section()
    {
        var config = CreateOllamaConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        var scheduler = root["Scheduler"];
        scheduler.Should().NotBeNull();
        scheduler?["Enabled"]?.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_Json_Contains_Logging_Section()
    {
        var config = CreateOllamaConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        root["Logging"]?["LogLevel"]?["Default"]?.GetValue<string>()
            .Should().Be("Information");
    }

    [Fact]
    public async Task WriteAsync_Json_Contains_Skills_Directory()
    {
        var config = CreateOllamaConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();
        var skillsDir = root["Skills"]?["SkillsDirectory"]?.GetValue<string>();

        skillsDir.Should().NotBeNullOrEmpty();
        skillsDir.Should().Contain("skills");
    }

    [Fact]
    public async Task WriteAsync_Json_Contains_Agents_Directory()
    {
        var config = CreateOllamaConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();
        var agentsDir = root["OpenVEPA"]?["Agents"]?["AgentsDirectory"]?.GetValue<string>();

        agentsDir.Should().NotBeNullOrEmpty();
        agentsDir.Should().Contain("agents");
    }

    [Fact]
    public async Task WriteAsync_Default_Port_Produces_Localhost_8371()
    {
        var config = CreateOllamaConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        root["Kestrel"]?["Endpoints"]?["Http"]?["Url"]?.GetValue<string>()
            .Should().Be("http://localhost:8371");
    }

    [Fact]
    public async Task WriteAsync_OpenAI_Provider_Sets_DefaultProvider_To_OpenAI()
    {
        var config = CreateOpenAiConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        root["Providers"]?["DefaultProvider"]?.GetValue<string>()
            .Should().Be("openai");
    }

    [Fact]
    public async Task WriteAsync_With_Null_Config_Throws_ArgumentNullException()
    {
        var act = () => SetupConfigurationWriter.WriteAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WriteAsync_Google_Config_Has_Google_Section()
    {
        var config = CreateGoogleConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        var google = root["Providers"]?["Google"];
        google.Should().NotBeNull();
        google?["ApiKey"]?.GetValue<string>().Should().Be("AIza-test-key");
        google?["Endpoint"]?.GetValue<string>().Should().Be("https://generativelanguage.googleapis.com/v1");
        google?["ModelId"]?.GetValue<string>().Should().Be("gemini-2.5-flash");
    }

    [Fact]
    public async Task WriteAsync_Google_Provider_Sets_DefaultProvider_To_Google()
    {
        var config = CreateGoogleConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        root["Providers"]?["DefaultProvider"]?.GetValue<string>()
            .Should().Be("google");
    }

    [Fact]
    public async Task WriteAsync_Anthropic_Config_Has_Anthropic_Section()
    {
        var config = CreateAnthropicConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        var anthropic = root["Providers"]?["Anthropic"];
        anthropic.Should().NotBeNull();
        anthropic?["ApiKey"]?.GetValue<string>().Should().Be("sk-ant-test-key");
        anthropic?["Endpoint"]?.GetValue<string>().Should().Be("https://api.anthropic.com/v1");
        anthropic?["ModelId"]?.GetValue<string>().Should().Be("claude-sonnet-4-20250514");
    }

    [Fact]
    public async Task WriteAsync_Anthropic_Provider_Sets_DefaultProvider_To_Anthropic()
    {
        var config = CreateAnthropicConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        root["Providers"]?["DefaultProvider"]?.GetValue<string>()
            .Should().Be("anthropic");
    }

    [Fact]
    public async Task WriteAsync_Groq_Config_Routes_To_OpenAi_Section()
    {
        var config = CreateGroqConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        var openAi = root["Providers"]?["OpenAi"];
        openAi.Should().NotBeNull();
        openAi?["ApiKey"]?.GetValue<string>().Should().Be("gsk-test-key");
        openAi?["ModelId"]?.GetValue<string>().Should().Be("llama-3.3-70b");
        openAi?["Endpoint"]?.GetValue<string>().Should().Be("https://api.groq.com/openai/v1");
    }

    [Fact]
    public async Task WriteAsync_Groq_Provider_Sets_DefaultProvider_To_Groq()
    {
        var config = CreateGroqConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        root["Providers"]?["DefaultProvider"]?.GetValue<string>()
            .Should().Be("groq");
    }

    [Fact]
    public async Task WriteAsync_Together_Config_Routes_To_OpenAi_Section()
    {
        var config = CreateTogetherConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        var openAi = root["Providers"]?["OpenAi"];
        openAi.Should().NotBeNull();
        openAi?["ApiKey"]?.GetValue<string>().Should().Be("tog-test-key");
        openAi?["ModelId"]?.GetValue<string>().Should().Be("meta-llama/Llama-3-70b");
        openAi?["Endpoint"]?.GetValue<string>().Should().Be("https://api.together.xyz/v1");
    }

    [Fact]
    public async Task WriteAsync_Together_Provider_Sets_DefaultProvider_To_Together()
    {
        var config = CreateTogetherConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        root["Providers"]?["DefaultProvider"]?.GetValue<string>()
            .Should().Be("together");
    }

    [Fact]
    public async Task WriteAsync_Perplexity_Config_Routes_To_OpenAi_Section()
    {
        var config = CreatePerplexityConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        var openAi = root["Providers"]?["OpenAi"];
        openAi.Should().NotBeNull();
        openAi?["ApiKey"]?.GetValue<string>().Should().Be("pplx-test-key");
        openAi?["ModelId"]?.GetValue<string>().Should().Be("sonar-pro");
        openAi?["Endpoint"]?.GetValue<string>().Should().Be("https://api.perplexity.ai");
    }

    [Fact]
    public async Task WriteAsync_Azure_Config_Routes_To_OpenAi_Section()
    {
        var config = CreateAzureConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        var openAi = root["Providers"]?["OpenAi"];
        openAi.Should().NotBeNull();
        openAi?["ApiKey"]?.GetValue<string>().Should().Be("az-test-key");
        openAi?["ModelId"]?.GetValue<string>().Should().Be("gpt-4o");
        openAi?["Endpoint"]?.GetValue<string>().Should().Be("https://myinstance.openai.azure.com");
    }

    [Fact]
    public async Task WriteAsync_Azure_Provider_Sets_DefaultProvider_To_Azure()
    {
        var config = CreateAzureConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        root["Providers"]?["DefaultProvider"]?.GetValue<string>()
            .Should().Be("azure");
    }

    [Fact]
    public async Task WriteAsync_Mistral_Config_Has_Mistral_Section()
    {
        var config = CreateMistralConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        var mistral = root["Providers"]?["Mistral"];
        mistral.Should().NotBeNull();
        mistral?["ApiKey"]?.GetValue<string>().Should().Be("mist-test-key");
        mistral?["Endpoint"]?.GetValue<string>().Should().Be("https://api.mistral.ai/v1");
        mistral?["ModelId"]?.GetValue<string>().Should().Be("mistral-large-latest");
    }

    [Fact]
    public async Task WriteAsync_Cohere_Config_Has_Cohere_Section()
    {
        var config = CreateCohereConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        var cohere = root["Providers"]?["Cohere"];
        cohere.Should().NotBeNull();
        cohere?["ApiKey"]?.GetValue<string>().Should().Be("co-test-key");
        cohere?["Endpoint"]?.GetValue<string>().Should().Be("https://api.cohere.com/v2");
        cohere?["ModelId"]?.GetValue<string>().Should().Be("command-r-plus");
    }

    [Fact]
    public async Task WriteAsync_Google_Config_Does_Not_Have_OpenAi_Section()
    {
        var config = CreateGoogleConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        root["Providers"]?["OpenAi"].Should().BeNull();
    }

    [Fact]
    public async Task WriteAsync_Groq_Config_Does_Not_Have_Groq_Section()
    {
        var config = CreateGroqConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        root["Providers"]?["Groq"].Should().BeNull();
    }

    [Fact]
    public async Task WriteAsync_OpenAi_Compatible_Providers_Write_Endpoint_Correctly()
    {
        var config = CreateGroqConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        root["Providers"]?["OpenAi"]?["Endpoint"]?.GetValue<string>()
            .Should().Be("https://api.groq.com/openai/v1",
                "OpenAI-compatible providers should use their own endpoint, not the default OpenAI one");
    }

    [Fact]
    public async Task WriteAsync_Without_Channels_Has_Empty_Channels_Section()
    {
        var config = CreateOllamaConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        var channels = root["Channels"];
        channels.Should().NotBeNull();
        channels!.AsObject().Count.Should().Be(0, "default config should produce an empty Channels object");
    }

    [Fact]
    public async Task WriteAsync_With_TelegramToken_Has_Telegram_Section()
    {
        var config = CreateOllamaConfigWithTelegram(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        var telegram = root["Channels"]?["Telegram"];
        telegram.Should().NotBeNull();
        telegram?["Enabled"]?.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task WriteAsync_With_TelegramToken_Has_Correct_BotToken()
    {
        var config = CreateOllamaConfigWithTelegram(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        root["Channels"]?["Telegram"]?["BotToken"]?.GetValue<string>()
            .Should().Be("123456:ABC-DEF-test-token");
    }

    [Fact]
    public async Task WriteAsync_Without_TelegramToken_Has_No_Telegram_Section()
    {
        var config = CreateOllamaConfig(_tempDir);

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        root["Channels"]?["Telegram"].Should().BeNull();
    }

    [Fact]
    public async Task WriteAsync_With_WhatsAppEnabled_Has_WhatsApp_Section()
    {
        var config = CreateOllamaConfig(_tempDir) with { WhatsAppEnabled = true };

        await SetupConfigurationWriter.WriteAsync(config, CancellationToken.None);

        var root = await ReadSettingsAsJsonNode();

        var whatsApp = root["Channels"]?["WhatsApp"];
        whatsApp.Should().NotBeNull();
        whatsApp?["Enabled"]?.GetValue<bool>().Should().BeTrue();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private async Task<JsonNode> ReadSettingsAsJsonNode()
    {
        var json = await File.ReadAllTextAsync(Path.Combine(_tempDir, "openvepa.conf"));
        return JsonNode.Parse(json)!;
    }

    private static SetupConfiguration CreateOllamaConfig(string homePath) => new()
    {
        Provider = "ollama",
        ModelId = "llama3.2",
        HomePath = homePath,
    };

    private static SetupConfiguration CreateOpenAiConfig(string homePath) => new()
    {
        Provider = "openai",
        ModelId = "gpt-4o",
        HomePath = homePath,
        ApiKey = "sk-test-key",
    };

    private static SetupConfiguration CreateGoogleConfig(string homePath) => new()
    {
        Provider = "google",
        ModelId = "gemini-2.5-flash",
        HomePath = homePath,
        ApiKey = "AIza-test-key",
        Endpoint = "https://generativelanguage.googleapis.com/v1",
    };

    private static SetupConfiguration CreateAnthropicConfig(string homePath) => new()
    {
        Provider = "anthropic",
        ModelId = "claude-sonnet-4-20250514",
        HomePath = homePath,
        ApiKey = "sk-ant-test-key",
        Endpoint = "https://api.anthropic.com/v1",
    };

    private static SetupConfiguration CreateGroqConfig(string homePath) => new()
    {
        Provider = "groq",
        ModelId = "llama-3.3-70b",
        HomePath = homePath,
        ApiKey = "gsk-test-key",
        Endpoint = "https://api.groq.com/openai/v1",
    };

    private static SetupConfiguration CreateTogetherConfig(string homePath) => new()
    {
        Provider = "together",
        ModelId = "meta-llama/Llama-3-70b",
        HomePath = homePath,
        ApiKey = "tog-test-key",
        Endpoint = "https://api.together.xyz/v1",
    };

    private static SetupConfiguration CreatePerplexityConfig(string homePath) => new()
    {
        Provider = "perplexity",
        ModelId = "sonar-pro",
        HomePath = homePath,
        ApiKey = "pplx-test-key",
        Endpoint = "https://api.perplexity.ai",
    };

    private static SetupConfiguration CreateAzureConfig(string homePath) => new()
    {
        Provider = "azure",
        ModelId = "gpt-4o",
        HomePath = homePath,
        ApiKey = "az-test-key",
        Endpoint = "https://myinstance.openai.azure.com",
    };

    private static SetupConfiguration CreateMistralConfig(string homePath) => new()
    {
        Provider = "mistral",
        ModelId = "mistral-large-latest",
        HomePath = homePath,
        ApiKey = "mist-test-key",
        Endpoint = "https://api.mistral.ai/v1",
    };

    private static SetupConfiguration CreateCohereConfig(string homePath) => new()
    {
        Provider = "cohere",
        ModelId = "command-r-plus",
        HomePath = homePath,
        ApiKey = "co-test-key",
        Endpoint = "https://api.cohere.com/v2",
    };

    private static SetupConfiguration CreateOllamaConfigWithTelegram(string homePath) => new()
    {
        Provider = "ollama",
        ModelId = "llama3.2",
        HomePath = homePath,
        TelegramBotToken = "123456:ABC-DEF-test-token",
    };
}
