[CmdletBinding()]
param(
    [string]$OutputDirectory = ".artifacts/migrations",
    [string]$RuntimeIdentifier = "",
    [switch]$SelfContained,
    [switch]$Force,
    [switch]$SkipPendingModelChangesCheck
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Resolve-RepositoryRoot {
    $directory = Get-Item -LiteralPath $PSScriptRoot

    while ($null -ne $directory) {
        if (Test-Path -LiteralPath (Join-Path $directory.FullName "CafeMenu.slnx")) {
            return $directory.FullName
        }

        $directory = $directory.Parent
    }

    throw "Repository root could not be located from script path '$PSScriptRoot'."
}

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & dotnet @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

try {
    $repositoryRoot = Resolve-RepositoryRoot
    $apiProject = "src/CafeMenu.Api"
    $dbContext = "CafeMenuDbContext"
    $isWindowsRuntime = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
        [System.Runtime.InteropServices.OSPlatform]::Windows)

    Push-Location -LiteralPath $repositoryRoot

    try {
        if ([System.IO.Path]::IsPathRooted($OutputDirectory)) {
            $resolvedOutputDirectory = $OutputDirectory
        }
        else {
            $resolvedOutputDirectory = Join-Path $repositoryRoot $OutputDirectory
        }

        New-Item -ItemType Directory -Path $resolvedOutputDirectory -Force | Out-Null

        $bundleFileName = if ([string]::IsNullOrWhiteSpace($RuntimeIdentifier)) {
            "cafemenu-migrations"
        }
        else {
            "cafemenu-migrations-$RuntimeIdentifier"
        }

        if ($RuntimeIdentifier.StartsWith("win-", [StringComparison]::OrdinalIgnoreCase) -or
            ([string]::IsNullOrWhiteSpace($RuntimeIdentifier) -and $isWindowsRuntime)) {
            $bundleFileName = "$bundleFileName.exe"
        }

        $bundlePath = Join-Path $resolvedOutputDirectory $bundleFileName

        if ((Test-Path -LiteralPath $bundlePath) -and -not $Force) {
            throw "Migration bundle already exists at '$bundlePath'. Re-run with -Force to overwrite it deliberately."
        }

        Invoke-DotNet -Arguments @("tool", "restore")

        if (-not $SkipPendingModelChangesCheck) {
            Invoke-DotNet -Arguments @(
                "ef",
                "migrations",
                "has-pending-model-changes",
                "--project",
                $apiProject,
                "--startup-project",
                $apiProject,
                "--context",
                $dbContext)
        }

        $bundleArguments = @(
            "ef",
            "migrations",
            "bundle",
            "--project",
            $apiProject,
            "--startup-project",
            $apiProject,
            "--context",
            $dbContext,
            "--output",
            $bundlePath)

        if ($Force) {
            $bundleArguments += "--force"
        }

        if (-not [string]::IsNullOrWhiteSpace($RuntimeIdentifier)) {
            $bundleArguments += @("--target-runtime", $RuntimeIdentifier)
        }

        if ($SelfContained) {
            $bundleArguments += "--self-contained"
        }

        Invoke-DotNet -Arguments $bundleArguments

        Write-Host "Migration bundle artifact: $bundlePath"
    }
    finally {
        Pop-Location
    }
}
catch {
    Write-Error $_
    exit 1
}
