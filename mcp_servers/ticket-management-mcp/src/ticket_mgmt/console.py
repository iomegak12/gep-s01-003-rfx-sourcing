"""Rich console startup banner and component registration tables."""

from __future__ import annotations

from rich.console import Console
from rich.panel import Panel
from rich.table import Table
from rich.text import Text

console = Console()

_BANNER_STYLE = "bold cyan"
_HEADER_STYLE = "bold magenta"


def print_startup_banner(
    host: str,
    port: int,
    db_url: str,
    version: str = "0.1.0",
) -> None:
    """Print the server startup banner with connection details."""
    transport_url = f"http://{host}:{port}/mcp"

    title = Text("🎫  Ticket Management MCP Server", style=_BANNER_STYLE)
    body = (
        f"[bold white]Version:[/bold white]   [yellow]{version}[/yellow]\n"
        f"[bold white]Transport:[/bold white] [green]{transport_url}[/green]\n"
        f"[bold white]Database:[/bold white]  [blue]{db_url}[/blue]"
    )
    console.print()
    console.print(Panel(body, title=title, border_style="cyan", expand=False))
    console.print()


def print_components_table(
    tools: list[str],
    resources: list[str],
    prompts: list[str],
    host: str = "0.0.0.0",
    port: int = 8989,
) -> None:
    """Print a table listing all registered MCP components."""
    table = Table(
        title="Registered MCP Components",
        title_style=_HEADER_STYLE,
        border_style="bright_black",
        show_lines=True,
    )
    table.add_column("Type", style="bold cyan", no_wrap=True)
    table.add_column("Name", style="white")

    for name in tools:
        table.add_row("🔧 Tool", name)
    for name in resources:
        table.add_row("📦 Resource", name)
    for name in prompts:
        table.add_row("💬 Prompt", name)

    console.print(table)
    console.print()
    console.print(
        f"[bold green]✓ Server ready.[/bold green]  "
        f"Connect MCP clients to [green]http://{host}:{port}/mcp[/green]",
        highlight=False,
    )
    console.print()
