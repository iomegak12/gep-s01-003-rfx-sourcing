#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Push-Location (Join-Path $root "src/EventService.Mcp")
try {
    dotnet run --no-launch-profile
} finally {
    Pop-Location
}
