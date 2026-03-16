using FluentAssertions;

using OpenVEPA.Cli.Setup;

namespace OpenVEPA.Cli.Tests.Setup;

public sealed class SetupConfigurationTests
{
    [Fact]
    public void Create_With_Required_Properties_Sets_Values()
    {
        var config = new SetupConfiguration
        {
            Provider = "ollama",
            ModelId = "llama3.2",
            HomePath = "/home/user/.openvepa",
        };

        config.Provider.Should().Be("ollama");
        config.ModelId.Should().Be("llama3.2");
        config.HomePath.Should().Be("/home/user/.openvepa");
    }

    [Fact]
    public void Default_Port_Is_8371()
    {
        var config = new SetupConfiguration
        {
            Provider = "ollama",
            ModelId = "llama3.2",
            HomePath = "/tmp",
        };

        config.Port.Should().Be(8371);
    }

    [Fact]
    public void Default_OllamaEndpoint_Is_Localhost_11434()
    {
        var config = new SetupConfiguration
        {
            Provider = "ollama",
            ModelId = "llama3.2",
            HomePath = "/tmp",
        };

        config.OllamaEndpoint.Should().Be("http://localhost:11434");
    }

    [Fact]
    public void Default_OpenAiEndpoint_Is_Api_OpenAI()
    {
        var config = new SetupConfiguration
        {
            Provider = "openai",
            ModelId = "gpt-4o",
            HomePath = "/tmp",
        };

        config.OpenAiEndpoint.Should().Be("https://api.openai.com/v1");
    }

    [Fact]
    public void Default_SchedulerEnabled_Is_True()
    {
        var config = new SetupConfiguration
        {
            Provider = "ollama",
            ModelId = "llama3.2",
            HomePath = "/tmp",
        };

        config.SchedulerEnabled.Should().BeTrue();
    }

    [Fact]
    public void Default_ApiKey_Is_Null()
    {
        var config = new SetupConfiguration
        {
            Provider = "ollama",
            ModelId = "llama3.2",
            HomePath = "/tmp",
        };

        config.ApiKey.Should().BeNull();
    }

    [Fact]
    public void Ollama_Config_With_All_Defaults()
    {
        var config = new SetupConfiguration
        {
            Provider = "ollama",
            ModelId = "llama3.2",
            HomePath = "/home/user/.openvepa",
        };

        config.Provider.Should().Be("ollama");
        config.ModelId.Should().Be("llama3.2");
        config.ApiKey.Should().BeNull();
        config.OllamaEndpoint.Should().Be("http://localhost:11434");
        config.Port.Should().Be(8371);
        config.SchedulerEnabled.Should().BeTrue();
    }

    [Fact]
    public void OpenAI_Config_With_ApiKey()
    {
        var config = new SetupConfiguration
        {
            Provider = "openai",
            ModelId = "gpt-4o",
            HomePath = "/home/user/.openvepa",
            ApiKey = "sk-test-key-123",
        };

        config.Provider.Should().Be("openai");
        config.ModelId.Should().Be("gpt-4o");
        config.ApiKey.Should().Be("sk-test-key-123");
        config.OpenAiEndpoint.Should().Be("https://api.openai.com/v1");
    }

    [Fact]
    public void Record_Equality_Same_Values_Are_Equal()
    {
        var a = new SetupConfiguration
        {
            Provider = "ollama",
            ModelId = "llama3.2",
            HomePath = "/tmp/test",
        };

        var b = new SetupConfiguration
        {
            Provider = "ollama",
            ModelId = "llama3.2",
            HomePath = "/tmp/test",
        };

        a.Should().Be(b);
    }

    [Fact]
    public void Record_Equality_Different_Provider_Are_Not_Equal()
    {
        var a = new SetupConfiguration
        {
            Provider = "ollama",
            ModelId = "llama3.2",
            HomePath = "/tmp",
        };

        var b = new SetupConfiguration
        {
            Provider = "openai",
            ModelId = "llama3.2",
            HomePath = "/tmp",
        };

        a.Should().NotBe(b);
    }

    [Fact]
    public void With_Expression_Creates_New_Instance_With_Changed_Property()
    {
        var original = new SetupConfiguration
        {
            Provider = "ollama",
            ModelId = "llama3.2",
            HomePath = "/tmp",
            Port = 8371,
        };

        var modified = original with { Port = 8080 };

        modified.Port.Should().Be(8080);
        modified.Provider.Should().Be("ollama");
        modified.ModelId.Should().Be("llama3.2");
        original.Port.Should().Be(8371, "original should remain unchanged");
    }

    [Fact]
    public void With_Expression_Preserves_Other_Properties()
    {
        var original = new SetupConfiguration
        {
            Provider = "openai",
            ModelId = "gpt-4o",
            HomePath = "/home/user",
            ApiKey = "sk-key",
            Port = 3000,
            SchedulerEnabled = false,
        };

        var modified = original with { ModelId = "gpt-4o-mini" };

        modified.Provider.Should().Be("openai");
        modified.ApiKey.Should().Be("sk-key");
        modified.HomePath.Should().Be("/home/user");
        modified.Port.Should().Be(3000);
        modified.SchedulerEnabled.Should().BeFalse();
        modified.ModelId.Should().Be("gpt-4o-mini");
    }

    [Fact]
    public void Custom_Port_Overrides_Default()
    {
        var config = new SetupConfiguration
        {
            Provider = "ollama",
            ModelId = "llama3.2",
            HomePath = "/tmp",
            Port = 9090,
        };

        config.Port.Should().Be(9090);
    }

    [Fact]
    public void Custom_OllamaEndpoint_Overrides_Default()
    {
        var config = new SetupConfiguration
        {
            Provider = "ollama",
            ModelId = "llama3.2",
            HomePath = "/tmp",
            OllamaEndpoint = "http://remote-server:11434",
        };

        config.OllamaEndpoint.Should().Be("http://remote-server:11434");
    }

    [Fact]
    public void Default_Endpoint_Is_Null()
    {
        var config = new SetupConfiguration
        {
            Provider = "ollama",
            ModelId = "llama3.2",
            HomePath = "/tmp",
        };

        config.Endpoint.Should().BeNull();
    }

    [Fact]
    public void Custom_Endpoint_Overrides_Default()
    {
        var config = new SetupConfiguration
        {
            Provider = "groq",
            ModelId = "llama-3.3-70b",
            HomePath = "/tmp",
            Endpoint = "https://api.groq.com/openai/v1",
        };

        config.Endpoint.Should().Be("https://api.groq.com/openai/v1");
    }

    [Fact]
    public void Google_Config_With_ApiKey()
    {
        var config = new SetupConfiguration
        {
            Provider = "google",
            ModelId = "gemini-2.0-flash",
            HomePath = "/home/user/.openvepa",
            ApiKey = "AIza-test-key",
            Endpoint = "https://generativelanguage.googleapis.com/v1",
        };

        config.Provider.Should().Be("google");
        config.ModelId.Should().Be("gemini-2.0-flash");
        config.ApiKey.Should().Be("AIza-test-key");
        config.Endpoint.Should().Be("https://generativelanguage.googleapis.com/v1");
    }

    [Fact]
    public void Anthropic_Config_With_ApiKey()
    {
        var config = new SetupConfiguration
        {
            Provider = "anthropic",
            ModelId = "claude-sonnet-4-20250514",
            HomePath = "/home/user/.openvepa",
            ApiKey = "sk-ant-test-key",
            Endpoint = "https://api.anthropic.com/v1",
        };

        config.Provider.Should().Be("anthropic");
        config.ModelId.Should().Be("claude-sonnet-4-20250514");
        config.ApiKey.Should().Be("sk-ant-test-key");
        config.Endpoint.Should().Be("https://api.anthropic.com/v1");
    }

    [Fact]
    public void Groq_Config_With_ApiKey_And_Endpoint()
    {
        var config = new SetupConfiguration
        {
            Provider = "groq",
            ModelId = "llama-3.3-70b",
            HomePath = "/tmp",
            ApiKey = "gsk-test-key",
            Endpoint = "https://api.groq.com/openai/v1",
        };

        config.Provider.Should().Be("groq");
        config.ModelId.Should().Be("llama-3.3-70b");
        config.ApiKey.Should().Be("gsk-test-key");
        config.Endpoint.Should().Be("https://api.groq.com/openai/v1");
    }

    [Fact]
    public void Together_Config_With_ApiKey_And_Endpoint()
    {
        var config = new SetupConfiguration
        {
            Provider = "together",
            ModelId = "meta-llama/Llama-3-70b",
            HomePath = "/tmp",
            ApiKey = "tog-test-key",
            Endpoint = "https://api.together.xyz/v1",
        };

        config.Provider.Should().Be("together");
        config.Endpoint.Should().Be("https://api.together.xyz/v1");
    }

    [Fact]
    public void Mistral_Config_With_ApiKey()
    {
        var config = new SetupConfiguration
        {
            Provider = "mistral",
            ModelId = "mistral-large-latest",
            HomePath = "/tmp",
            ApiKey = "mist-test-key",
            Endpoint = "https://api.mistral.ai/v1",
        };

        config.Provider.Should().Be("mistral");
        config.ModelId.Should().Be("mistral-large-latest");
        config.ApiKey.Should().Be("mist-test-key");
    }

    [Fact]
    public void Default_TelegramBotToken_Is_Null()
    {
        var config = new SetupConfiguration { Provider = "ollama", ModelId = "llama3.2", HomePath = "/tmp" };

        config.TelegramBotToken.Should().BeNull();
    }

    [Fact]
    public void Default_WhatsAppEnabled_Is_False()
    {
        var config = new SetupConfiguration { Provider = "ollama", ModelId = "llama3.2", HomePath = "/tmp" };

        config.WhatsAppEnabled.Should().BeFalse();
    }

    [Fact]
    public void Telegram_Config_With_BotToken()
    {
        var config = new SetupConfiguration
        {
            Provider = "ollama",
            ModelId = "llama3.2",
            HomePath = "/tmp",
            TelegramBotToken = "123456:ABC-DEF",
        };

        config.TelegramBotToken.Should().Be("123456:ABC-DEF");
    }
}
