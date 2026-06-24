param(
    [Parameter(Position = 0)]
    [string] $Command,

    [Parameter(Position = 1, ValueFromRemainingArguments = $true)]
    [string[]] $Arguments
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$validEnvironments = @("dev", "prd")
$validActions = @("up", "down", "restart", "recreate")

function Show-Usage {
    Write-Host "Usage: .\da.cmd {dev|prd} {up|down|restart|recreate}"
    Write-Host "       .\da.cmd {dev|prd} {docker compose args}"
}

function Read-DotEnv {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    $values = @{}

    if (-not (Test-Path -LiteralPath $Path)) {
        return $values
    }

    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()

        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith("#")) {
            continue
        }

        $separatorIndex = $trimmed.IndexOf("=")

        if ($separatorIndex -le 0) {
            continue
        }

        $key = $trimmed.Substring(0, $separatorIndex).Trim()
        $value = $trimmed.Substring($separatorIndex + 1).Trim()

        if (
            ($value.StartsWith('"') -and $value.EndsWith('"')) -or
            ($value.StartsWith("'") -and $value.EndsWith("'"))
        ) {
            $value = $value.Substring(1, $value.Length - 2)
        }

        $values[$key] = $value
    }

    return $values
}

function Invoke-Docker {
    param(
        [Parameter(Mandatory = $true)]
        [string[]] $Arguments
    )

    Write-Host "docker $($Arguments -join ' ')"
    & docker @Arguments

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

if ($null -eq $Arguments) {
    $Arguments = @()
}

if ($validEnvironments -notcontains $Command) {
    [Console]::Error.WriteLine("Invalid environment: $Command")
    Show-Usage
    exit 1
}

if ($Arguments.Count -eq 0) {
    [Console]::Error.WriteLine("Missing docker compose arguments.")
    Show-Usage
    exit 1
}

$composeFile = Join-Path $PSScriptRoot "docker-compose.$Command.yml"

if (-not (Test-Path -LiteralPath $composeFile)) {
    [Console]::Error.WriteLine("Compose file not found: $composeFile")
    exit 1
}

$dockerArgs = @("compose", "-f", $composeFile)

$isShortcut = $Arguments.Count -eq 1 -and ($validActions -contains $Arguments[0])

if ($isShortcut) {
    switch ($Arguments[0]) {
        "up" {
            $dockerArgs += @("up", "-d")
        }
        "down" {
            $dockerArgs += @("down")
        }
        "restart" {
            $dockerArgs += @("restart")
        }
        "recreate" {
            $dockerArgs += @("up", "-d", "--force-recreate")
        }
    }
} else {
    $dockerArgs += $Arguments
}

Invoke-Docker -Arguments $dockerArgs
exit 0
