[CmdletBinding()]
param(
    [string]$SolutionPath = "MedicalCenter.sln",
    [string]$Configuration = "Release",
    [string]$ResultsDirectory = "",
    [string]$SonarHostUrl = ""
)

$ErrorActionPreference = "Stop"

if ($env:SKIP_PREPUSH_QUALITY -eq "1") {
    Write-Host "SKIP_PREPUSH_QUALITY=1 -> se omite validación pre-push."
    exit 0
}

if ([string]::IsNullOrWhiteSpace($ResultsDirectory)) {
    $ResultsDirectory = Join-Path $env:TEMP "MedicalCenter\TestResults\sonar"
}

$repoRoot = Resolve-Path "."
$sonarWorkingDirectory = Join-Path $repoRoot ".sonarqube"

function Clear-ReadOnlyAttributes {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return }
    Get-ChildItem -LiteralPath $Path -Recurse -Force -ErrorAction SilentlyContinue | ForEach-Object {
        if ($_.Attributes -band [IO.FileAttributes]::ReadOnly) {
            $_.Attributes = ($_.Attributes -bxor [IO.FileAttributes]::ReadOnly)
        }
    }
    $rootItem = Get-Item -LiteralPath $Path -Force -ErrorAction SilentlyContinue
    if ($null -ne $rootItem -and ($rootItem.Attributes -band [IO.FileAttributes]::ReadOnly)) {
        $rootItem.Attributes = ($rootItem.Attributes -bxor [IO.FileAttributes]::ReadOnly)
    }
}

function Invoke-WithRetry {
    param(
        [scriptblock]$Action,
        [string]$Description,
        [int]$Retries = 3,
        [int]$DelaySeconds = 2
    )

    for ($attempt = 1; $attempt -le $Retries; $attempt++) {
        try {
            & $Action
            return
        }
        catch {
            if ($attempt -ge $Retries) { throw }
            Write-Host "$Description falló (intento $attempt/$Retries). Reintentando en $DelaySeconds s..." -ForegroundColor Yellow
            Start-Sleep -Seconds $DelaySeconds
        }
    }
}

function Require-EnvVar {
    param([string]$Name)
    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Falta variable de entorno requerida: $Name"
    }
}

Require-EnvVar -Name "SONAR_TOKEN"
Require-EnvVar -Name "SONAR_PROJECT_KEY"
Require-EnvVar -Name "SONAR_ORGANIZATION"

if ([string]::IsNullOrWhiteSpace($SonarHostUrl)) {
    if ([string]::IsNullOrWhiteSpace($env:SONAR_HOST_URL)) {
        $SonarHostUrl = "https://sonarcloud.io"
    }
    else {
        $SonarHostUrl = $env:SONAR_HOST_URL
    }
}

dotnet tool update --global dotnet-sonarscanner | Out-Host
$globalTools = Join-Path $HOME ".dotnet\tools"
if ($env:PATH -notlike "*$globalTools*") {
    $env:PATH = "$globalTools;$env:PATH"
}

$sonarBegun = $false

try {
    if (Test-Path -LiteralPath $sonarWorkingDirectory) {
        Clear-ReadOnlyAttributes -Path $sonarWorkingDirectory
        Remove-Item -LiteralPath $sonarWorkingDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }

    if (Test-Path -LiteralPath $ResultsDirectory) {
        Remove-Item -LiteralPath $ResultsDirectory -Recurse -Force
    }

    Invoke-WithRetry -Description "Sonar begin" -Action {
        dotnet sonarscanner begin `
            /k:"$($env:SONAR_PROJECT_KEY)" `
            /o:"$($env:SONAR_ORGANIZATION)" `
            /d:sonar.host.url="$SonarHostUrl" `
            /d:sonar.token="$($env:SONAR_TOKEN)" `
            /d:sonar.qualitygate.wait=true `
            /d:sonar.qualitygate.timeout=600 `
            /d:sonar.cs.vstest.reportsPaths="$ResultsDirectory/**/*.trx" `
            /d:sonar.cs.opencover.reportsPaths="$ResultsDirectory/**/coverage.opencover.xml"
    }

    $sonarBegun = $true

    Invoke-WithRetry -Description "dotnet restore" -Action {
        dotnet restore $SolutionPath
    }

    Invoke-WithRetry -Description "dotnet build" -Action {
        dotnet build $SolutionPath --configuration $Configuration --no-restore -m:1 /nr:false /p:UseSharedCompilation=false
    }

    Invoke-WithRetry -Description "dotnet test" -Action {
        dotnet test $SolutionPath `
            --configuration $Configuration `
            --no-build `
            --results-directory $ResultsDirectory `
            --logger "trx;LogFileName=test-results.trx" `
            --collect:"XPlat Code Coverage;Format=opencover"
    }
}
finally {
    if ($sonarBegun) {
        dotnet sonarscanner end /d:sonar.token="$($env:SONAR_TOKEN)"
    }
}
