#!/usr/bin/env pwsh
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    Get-ChildItem -Recurse -Directory -Include bin, obj | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    dotnet clean EventService.Mcp.sln
} finally {
    Pop-Location
}
