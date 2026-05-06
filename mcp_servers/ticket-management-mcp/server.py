"""Ticket Management MCP Server — entrypoint with graceful shutdown."""

from __future__ import annotations

import signal
import sys

from rich.console import Console

_console = Console()


def _shutdown(signum=None, frame=None) -> None:  # noqa: ARG001
    """Clean up resources and exit without printing a traceback."""
    _console.print("\n[bold yellow]⏹  Shutting down — goodbye.[/bold yellow]")
    # Dispose the SQLAlchemy connection pool so any in-flight sessions are
    # released before the process exits.
    try:
        from ticket_mgmt.db.engine import engine
        engine.dispose()
    except Exception:  # pragma: no cover
        pass
    sys.exit(0)


if __name__ == "__main__":
    # SIGTERM is sent by Docker / systemd on container stop.
    signal.signal(signal.SIGTERM, _shutdown)

    from ticket_mgmt.mcp_app import create_app

    mcp, settings = create_app()
    try:
        mcp.run(
            transport="http",
            host=settings.mcp_host,
            port=settings.mcp_port,
        )
    except KeyboardInterrupt:
        # Ctrl+C — suppress the traceback and exit cleanly.
        _shutdown()
