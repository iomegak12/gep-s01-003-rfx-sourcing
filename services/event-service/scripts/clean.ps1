#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Resets the local Event Service workspace.

.DESCRIPTION
    Deletes the SQLite database file(s) and the bin/obj directories so the
    next run rebuilds from scratch and re-applies migrations + seed data.
#>

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

$paths = @('./data', './bin', './obj')

foreach ($p in $paths) {
    if (Test-Path $p) {
        Write-Host "Removing $p" -ForegroundColor Yellow
        Remove-Item $p -Recurse -Force
    }
}

Write-Host "Workspace cleaned." -ForegroundColor Green
