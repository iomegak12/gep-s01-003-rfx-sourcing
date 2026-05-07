using EventService.Mcp.Configuration;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;
using Spectre.Console;
using AppMcpServerOptions = EventService.Mcp.Configuration.McpServerOptions;

namespace EventService.Mcp.Startup;

public static class StartupBanner
{
    public static void Render(IServiceProvider services, IConfiguration config)
    {
        var mcp = services.GetRequiredService<IOptions<AppMcpServerOptions>>().Value;
        var upstream = services.GetRequiredService<IOptions<EventServiceOptions>>().Value;
        var listenUrl = config["Kestrel:Endpoints:Http:Url"]
                        ?? config["ASPNETCORE_URLS"]
                        ?? "(default Kestrel binding)";

        AnsiConsole.Write(new FigletText("Event Service MCP")
            .LeftJustified()
            .Color(Color.Cyan1));

        var info = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Setting[/]"))
            .AddColumn(new TableColumn("[bold]Value[/]"));

        info.AddRow("[yellow]Server name[/]", $"[green]{mcp.ServerName}[/]");
        info.AddRow("[yellow]Server version[/]", $"[green]{mcp.ServerVersion}[/]");
        info.AddRow("[yellow]MCP endpoint[/]", $"[green]{mcp.EndpointPath}[/]");
        info.AddRow("[yellow]Stateless mode[/]", mcp.Stateless ? "[green]true[/]" : "[red]false[/]");
        info.AddRow("[yellow]Listening on[/]", $"[green]{listenUrl}[/]");
        info.AddRow("[yellow]Upstream Event Service[/]", $"[green]{upstream.BaseUrl}[/]");
        info.AddRow("[yellow]Upstream timeout[/]", $"[green]{upstream.TimeoutSeconds}s[/]");
        info.AddRow("[yellow]Auth model[/]", "[green]Pass-through Bearer (JWT not validated locally)[/]");

        AnsiConsole.Write(new Panel(info)
            .Header("[bold cyan]Configuration[/]", Justify.Left)
            .BorderColor(Color.Cyan1)
            .RoundedBorder());

        var tools = services.GetServices<McpServerTool>().ToArray();
        var resources = services.GetServices<McpServerResource>().ToArray();
        var prompts = services.GetServices<McpServerPrompt>().ToArray();

        var primitives = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(Color.Grey)
            .AddColumn(new TableColumn("[bold]Kind[/]"))
            .AddColumn(new TableColumn("[bold]Name[/]"))
            .AddColumn(new TableColumn("[bold]Description[/]"));

        foreach (var t in tools.OrderBy(t => t.ProtocolTool.Name))
        {
            primitives.AddRow(
                "[blue]tool[/]",
                $"[green]{t.ProtocolTool.Name}[/]",
                Markup.Escape(Truncate(t.ProtocolTool.Description ?? string.Empty, 80)));
        }
        foreach (var r in resources.OrderBy(r => r.ProtocolResourceTemplate.UriTemplate))
        {
            primitives.AddRow(
                "[magenta]resource[/]",
                $"[green]{r.ProtocolResourceTemplate.UriTemplate}[/]",
                Markup.Escape(Truncate(r.ProtocolResourceTemplate.Description ?? string.Empty, 80)));
        }
        foreach (var p in prompts.OrderBy(p => p.ProtocolPrompt.Name))
        {
            primitives.AddRow(
                "[yellow]prompt[/]",
                $"[green]{p.ProtocolPrompt.Name}[/]",
                Markup.Escape(Truncate(p.ProtocolPrompt.Description ?? string.Empty, 80)));
        }

        AnsiConsole.Write(new Panel(primitives)
            .Header($"[bold cyan]MCP primitives[/]  [grey]tools={tools.Length}, resources={resources.Length}, prompts={prompts.Length}[/]", Justify.Left)
            .BorderColor(Color.Cyan1)
            .RoundedBorder());

        AnsiConsole.MarkupLine("[grey]Press Ctrl+C to gracefully stop the server.[/]");
        AnsiConsole.WriteLine();
    }

    public static void RenderShutdown(string colorMarkup, string phase, string message)
    {
        AnsiConsole.MarkupLine($"[{colorMarkup}]●[/] [bold]{Markup.Escape(phase)}[/] [grey]{Markup.Escape(message)}[/]");
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..(max - 1)] + "…";
}
