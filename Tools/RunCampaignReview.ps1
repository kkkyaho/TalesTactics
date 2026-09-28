param([string]$Player = "$PSScriptRoot/../Builds/CampaignReview/TalesTactics.exe", [switch]$Tactical)
$ErrorActionPreference = 'Stop'
$playerPath = (Resolve-Path -LiteralPath $Player).Path
$runId = [Guid]::NewGuid().ToString('N')
$saveRoot = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData/LocalLow/LocalTactics/TalesTactics'
$reviewRoot = Join-Path $saveRoot "Reviews/$runId"
$evidence = Join-Path $PSScriptRoot '../Docs/PlayerReviews'
New-Item -ItemType Directory -Force -Path $reviewRoot, $evidence | Out-Null
function UserSaveState {
    @('campaign.json', 'campaign.json.bak') | ForEach-Object {
        $path = Join-Path $saveRoot $_
        if (Test-Path -LiteralPath $path -PathType Leaf) { "$($_):$((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash)" }
        else { "$($_):missing" }
    }
}
$before = @(UserSaveState)
$results = @()
$failure = $null
try {
    foreach ($phase in @('chapter1', 'chapter2', 'chapter3', 'resume')) {
        $log = Join-Path $reviewRoot "$phase-player.log"
        $reviewArgs = @('--campaign-review', $runId, $phase, '-screen-fullscreen', '0', '-screen-width', '1280', '-screen-height', '800', '-logFile', "`"$log`"")
        if ($Tactical) { $reviewArgs += '--tactical-review' }
        $process = Start-Process -FilePath $playerPath -ArgumentList $reviewArgs -WindowStyle Hidden -PassThru
        Write-Output "Started $phase (PID $($process.Id)), evidence: $reviewRoot"
        $deadline = [DateTime]::UtcNow.AddSeconds(330)
        while (!$process.WaitForExit(1000)) {
            if ([DateTime]::UtcNow -gt $deadline) {
                Stop-Process -Id $process.Id
                throw "Player timed out in $phase"
            }
        }
        $reportPath = Join-Path $reviewRoot "$phase.json"
        if (!(Test-Path -LiteralPath $reportPath)) { throw "Missing report for $phase; inspect $log" }
        $result = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
        $results += $result
        Copy-Item -LiteralPath $reportPath -Destination (Join-Path $evidence "$runId-$phase.json")
        Get-ChildItem -LiteralPath $reviewRoot -Filter "$phase-*.png" | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $evidence "$runId-$($_.Name)") }
        Write-Output "$phase passed=$($result.passed), turns=$($result.playerTurns), attacks=$($result.attacks), seconds=$($result.seconds)"
        if ($process.ExitCode -ne 0 -or !$result.passed) { throw "Review failed: $phase. $($result.error)" }
    }
}
catch { $failure = $_.ToString() }
finally {
    $after = @(UserSaveState)
    $unchanged = ($before -join '|') -eq ($after -join '|')
    $summary = [ordered]@{
        runId = $runId
        utc = [DateTime]::UtcNow.ToString('o')
        playerSHA256 = (Get-FileHash -LiteralPath $playerPath -Algorithm SHA256).Hash
        runtimeSHA256 = (Get-FileHash -LiteralPath (Join-Path (Split-Path $playerPath) 'TalesTactics_Data/Managed/TalesTactics.Runtime.dll') -Algorithm SHA256).Hash
        passed = !$failure -and $unchanged -and $results.Count -eq 4
        userSaveUnchanged = $unchanged
        error = $failure
        reports = $results
    }
    $summary | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $evidence "$runId-summary.json") -Encoding utf8
}
if (!$unchanged) { throw 'User save or backup changed during review.' }
if ($failure) { throw $failure }
Write-Output "PASS: four separate player launches; user save unchanged. Summary: $evidence/$runId-summary.json"
