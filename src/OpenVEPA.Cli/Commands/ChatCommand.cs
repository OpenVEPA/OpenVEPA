using System.ComponentModel;
using OpenVEPA.Core.Agents;
using OpenVEPA.Storage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenVEPA.Cli.Commands;

/// <summary>
/// Interactive chat command that provides a REPL for conversing with the agent.
/// </summary>
internal sealed class ChatCommand : AsyncCommand<ChatCommand.Settings>
{
    private readonly IAgentRuntime _agentRuntime;
    private readonly SqliteSessionStore _sessionStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatCommand"/> class.
    /// </summary>
    /// <param name="agentRuntime">The agent runtime for processing messages.</param>
    /// <param name="sessionStore">The session store for managing chat sessions.</param>
    public ChatCommand(IAgentRuntime agentRuntime, SqliteSessionStore sessionStore)
    {
        _agentRuntime = agentRuntime ?? throw new ArgumentNullException(nameof(agentRuntime));
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
    }

    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var sessionId = settings.SessionId;

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            var session = await _sessionStore.CreateSessionAsync(
                "Interactive Chat",
                CancellationToken.None).ConfigureAwait(false);
            sessionId = session.Id;
            AnsiConsole.MarkupLine($"[dim]Created session:[/] [yellow]{Markup.Escape(sessionId)}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[dim]Resuming session:[/] [yellow]{Markup.Escape(sessionId)}[/]");
        }

        AnsiConsole.MarkupLine("[dim]Type [yellow]exit[/] or [yellow]quit[/] to leave.[/]");
        AnsiConsole.WriteLine();

        while (true)
        {
            var input = AnsiConsole.Prompt(
                new TextPrompt<string>("[green]You>[/] ")
                    .AllowEmpty());

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)
                || input.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine("[dim]Goodbye![/]");
                break;
            }

            var response = await AnsiConsole.Status()
                .StartAsync("Thinking...", async _ =>
                    await _agentRuntime.ProcessMessageAsync(
                        sessionId,
                        input,
                        CancellationToken.None).ConfigureAwait(false))
                .ConfigureAwait(false);

            AnsiConsole.MarkupLine($"[blue]Assistant>[/] {Markup.Escape(response.Content)}");
            AnsiConsole.WriteLine();
        }

        return 0;
    }

    /// <summary>
    /// Settings for the chat command.
    /// </summary>
    public sealed class Settings : CommandSettings
    {
        /// <summary>
        /// Gets the optional session identifier to resume an existing conversation.
        /// </summary>
        [CommandOption("--session")]
        [Description("Session ID to resume")]
        public string? SessionId { get; init; }
    }
}
