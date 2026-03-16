using OpenVEPA.Scheduler;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenVEPA.Cli.Commands;

/// <summary>
/// Lists all scheduled tasks in a formatted table.
/// </summary>
internal sealed class ScheduleListCommand : AsyncCommand
{
    private readonly SchedulerService _schedulerService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScheduleListCommand"/> class.
    /// </summary>
    /// <param name="schedulerService">The scheduler service for querying scheduled tasks.</param>
    public ScheduleListCommand(SchedulerService schedulerService)
    {
        _schedulerService = schedulerService ?? throw new ArgumentNullException(nameof(schedulerService));
    }

    /// <inheritdoc />
    public override Task<int> ExecuteAsync(CommandContext context)
    {
        var schedules = _schedulerService.ListSchedules();

        if (schedules.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No scheduled tasks.[/]");
            return Task.FromResult(0);
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Scheduled Tasks[/]")
            .AddColumn("[bold]ID[/]")
            .AddColumn("[bold]Name[/]")
            .AddColumn("[bold]Cron[/]")
            .AddColumn("[bold]Agent[/]")
            .AddColumn("[bold]Enabled[/]");

        foreach (var schedule in schedules)
        {
            var enabled = schedule.Enabled ? "[green]Yes[/]" : "[red]No[/]";
            var agent = schedule.AgentName ?? "[dim]Default[/]";

            table.AddRow(
                Markup.Escape(schedule.Id),
                Markup.Escape(schedule.Name),
                Markup.Escape(schedule.CronExpression),
                agent,
                enabled);
        }

        AnsiConsole.Write(table);
        return Task.FromResult(0);
    }
}
