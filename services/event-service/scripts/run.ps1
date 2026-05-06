#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs the Event Service from the project root.

.DESCRIPTION
    Builds and runs the .NET 9 Event Service on http://0.0.0.0:5001.
    Requires appsettings.json to exist (copy from appsettings.Example.json).
#>

$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
Set-Location $projectRoot

if (-not (Test-Path './appsettings.json')) {
    Write-Host "appsettings.json not found. Copying from appsettings.Example.json..." -ForegroundColor Yellow
    Copy-Item './appsettings.Example.json' './appsettings.json'
    Write-Host "Edit appsettings.json and set Jwt:SigningKey before running again." -ForegroundColor Yellow
    exit 1
}

$env:ASPNETCORE_ENVIRONMENT = 'Development'

Write-Host "Starting Event Service on http://0.0.0.0:5001 ..." -ForegroundColor Cyan
dotnet run --project EventService.csproj
