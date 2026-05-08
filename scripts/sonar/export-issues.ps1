[CmdletBinding()]
param(
    [string]$ProjectKey = "",
    [string]$Branch = "",
    [string]$PullRequest = "",
    [string]$Severities = "BLOCKER,CRITICAL,MAJOR,MINOR,INFO",
    [string]$Statuses = "OPEN,CONFIRMED,REOPENED",
    [string]$HotspotStatuses = "TO_REVIEW,REVIEWED",
    [switch]$IncludeHotspots = $true,
    [string]$OutputFile = "sonar-issues.json",
    [string]$ApiBaseUrl = "",
    [int]$PageSize = 500
)

$ErrorActionPreference = "Stop"

function Require-EnvVar {
    param([string]$Name)
    $value = [Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Falta variable de entorno requerida: $Name"
    }
    return $value
}

$token = Require-EnvVar -Name "SONAR_TOKEN"

if ([string]::IsNullOrWhiteSpace($ProjectKey)) {
    $ProjectKey = Require-EnvVar -Name "SONAR_PROJECT_KEY"
}

if ([string]::IsNullOrWhiteSpace($ApiBaseUrl)) {
    if ([string]::IsNullOrWhiteSpace($env:SONAR_HOST_URL)) {
        $ApiBaseUrl = "https://sonarcloud.io/api"
    }
    else {
        $hostUrl = $env:SONAR_HOST_URL.TrimEnd("/")
        if ($hostUrl.EndsWith("/api")) {
            $ApiBaseUrl = $hostUrl
        }
        else {
            $ApiBaseUrl = "$hostUrl/api"
        }
    }
}

$encodedToken = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${token}:"))
$headers = @{
    Authorization = "Basic $encodedToken"
}

$page = 1
$allIssues = @()
$total = $null

do {
    $query = @{
        componentKeys = $ProjectKey
        statuses = $Statuses
        severities = $Severities
        p = $page
        ps = $PageSize
    }

    if (-not [string]::IsNullOrWhiteSpace($Branch)) {
        $query["branch"] = $Branch
    }

    if (-not [string]::IsNullOrWhiteSpace($PullRequest)) {
        $query["pullRequest"] = $PullRequest
    }

    $queryString = ($query.GetEnumerator() | ForEach-Object {
            "{0}={1}" -f [uri]::EscapeDataString($_.Key), [uri]::EscapeDataString($_.Value)
        }) -join "&"

    $url = "$ApiBaseUrl/issues/search?$queryString"
    try {
        $response = Invoke-RestMethod -Method Get -Uri $url -Headers $headers
    }
    catch {
        Write-Host "Error consultando Sonar API URL: $url" -ForegroundColor Red
        if ($_.Exception.Response -and $_.Exception.Response.GetResponseStream()) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $body = $reader.ReadToEnd()
            if (-not [string]::IsNullOrWhiteSpace($body)) {
                Write-Host "Response body: $body" -ForegroundColor Yellow
            }
        }
        throw
    }

    if ($null -eq $total) {
        $total = [int]$response.total
    }

    if ($response.issues) {
        $allIssues += $response.issues
    }

    $page++
} while ($allIssues.Count -lt $total)

$allHotspots = @()
if ($IncludeHotspots) {
    $hotspotStatusValues = $HotspotStatuses.Split(",") | ForEach-Object { $_.Trim() } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    $seenHotspotKeys = @{}

    foreach ($hotspotStatus in $hotspotStatusValues) {
        $hotspotPage = 1
        $hotspotTotal = $null
        $hotspotsForStatus = @()

        do {
            $hotspotQuery = @{
                projectKey = $ProjectKey
                p = $hotspotPage
                ps = $PageSize
                status = $hotspotStatus
            }

            if (-not [string]::IsNullOrWhiteSpace($Branch)) {
                $hotspotQuery["branch"] = $Branch
            }

            if (-not [string]::IsNullOrWhiteSpace($PullRequest)) {
                $hotspotQuery["pullRequest"] = $PullRequest
            }

            $hotspotQueryString = ($hotspotQuery.GetEnumerator() | ForEach-Object {
                    "{0}={1}" -f [uri]::EscapeDataString($_.Key), [uri]::EscapeDataString($_.Value)
                }) -join "&"

            $hotspotUrl = "$ApiBaseUrl/hotspots/search?$hotspotQueryString"
            try {
                $hotspotResponse = Invoke-RestMethod -Method Get -Uri $hotspotUrl -Headers $headers
            }
            catch {
                Write-Host "Error consultando Sonar Hotspots API URL: $hotspotUrl" -ForegroundColor Red
                if ($_.Exception.Response -and $_.Exception.Response.GetResponseStream()) {
                    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                    $body = $reader.ReadToEnd()
                    if (-not [string]::IsNullOrWhiteSpace($body)) {
                        Write-Host "Response body: $body" -ForegroundColor Yellow
                    }
                }
                throw
            }

            if ($null -eq $hotspotTotal) {
                $hotspotTotal = [int]$hotspotResponse.paging.total
            }

            if ($hotspotResponse.hotspots) {
                $hotspotsForStatus += $hotspotResponse.hotspots
            }

            $hotspotPage++
        } while ($hotspotsForStatus.Count -lt $hotspotTotal)

        foreach ($hotspot in $hotspotsForStatus) {
            if ($hotspot -and $hotspot.key -and -not $seenHotspotKeys.ContainsKey($hotspot.key)) {
                $seenHotspotKeys[$hotspot.key] = $true
                $allHotspots += $hotspot
            }
        }
    }
}

$payload = [ordered]@{
    exportedAt = (Get-Date).ToString("o")
    projectKey = $ProjectKey
    branch = $Branch
    pullRequest = $PullRequest
    total = $allIssues.Count
    issues = $allIssues
    hotspotsTotal = $allHotspots.Count
    hotspots = $allHotspots
}

$payload | ConvertTo-Json -Depth 100 | Out-File -FilePath $OutputFile -Encoding UTF8
Write-Host "Export completado: $OutputFile ($($allIssues.Count) issues, $($allHotspots.Count) hotspots)"
