using OpenVEPA.Core.Sessions;
using OpenVEPA.Core.Skills;
using OpenVEPA.Scheduler;
using OpenVEPA.Storage;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenVEPA.Cli.Commands;

/// <summary>
/// Displays the current status of the OpenVEPA system in a formatted table.
/// </summary>
internal sealed class StatusCommand : AsyncCommand
{
    private readonly ISkillRuntime _skillRuntime;
    private readonly SqliteSessionStore _sessionStore;
    private readonly SchedulerService _schedulerService;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusCommand"/> class.
    /// </summary>
    /// <param name="skillRuntime">The skill runtime for querying loaded skills.</param>
    /// <param name="sessionStore">The session store for querying active sessions.</param>
    /// <param name="schedulerService">The scheduler service for querying scheduled tasks.</param>
    public StatusCommand(
        ISkillRuntime skillRuntime,
        SqliteSessionStore sessionStore,
        SchedulerService schedulerService)
    {
        _skillRuntime = skillRuntime ?? throw new ArgumentNullException(nameof(skillRuntime));
        _sessionStore = sessionStore ?? throw new ArgumentNullException(nameof(sessionStore));
        _schedulerService = schedulerService ?? throw new ArgumentNullException(nameof(schedulerService));
    }

    /// <inheritdoc />
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var skills = _skillRuntime.ListSkills();
        var sessions = await _sessionStore.ListSessionsAsync(CancellationToken.None)
            .ConfigureAwait(false);
        var schedules = _schedulerService.ListSchedules();
        var activeSessions = sessions.Count(s => s.Status == SessionStatus.Active);

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]OpenVEPA Status[/]")
            .AddColumn("[bold]Component[/]")
            .AddColumn("[bold]Value[/]");

        table.AddRow("Server", "[green]Configured[/]");
        table.AddRow("Loaded Skills", skills.Count.ToString());
        table.AddRow("Active Sessions", activeSessions.ToString());
        table.AddRow("Total Sessions", sessions.Count.ToString());
        table.AddRow("Scheduled Tasks", schedules.Count.ToString());
        table.AddRow("Scheduler", schedules.Count > 0 ? "[green]Active[/]" : "[dim]No tasks[/]");

        AnsiConsole.Write(table);
        return 0;
    }
}
