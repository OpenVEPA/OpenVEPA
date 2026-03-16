using OpenVEPA.Core.Skills;
using Spectre.Console;
using Spectre.Console.Cli;

namespace OpenVEPA.Cli.Commands;

/// <summary>
/// Lists all loaded skills in a formatted table.
/// </summary>
internal sealed class SkillListCommand : AsyncCommand
{
    private readonly ISkillRuntime _skillRuntime;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkillListCommand"/> class.
    /// </summary>
    /// <param name="skillRuntime">The skill runtime for querying loaded skills.</param>
    public SkillListCommand(ISkillRuntime skillRuntime)
    {
        _skillRuntime = skillRuntime ?? throw new ArgumentNullException(nameof(skillRuntime));
    }

    /// <inheritdoc />
    public override Task<int> ExecuteAsync(CommandContext context)
    {
        var skills = _skillRuntime.ListSkills();

        if (skills.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No skills loaded.[/]");
            return Task.FromResult(0);
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold]Loaded Skills[/]")
            .AddColumn("[bold]Name[/]")
            .AddColumn("[bold]Version[/]")
            .AddColumn("[bold]Type[/]")
            .AddColumn("[bold]Description[/]");

        foreach (var skill in skills)
        {
            table.AddRow(
                Markup.Escape(skill.Name),
                Markup.Escape(skill.Version),
                skill.Type.ToString(),
                Markup.Escape(skill.Description));
        }

        AnsiConsole.Write(table);
        return Task.FromResult(0);
    }
}
