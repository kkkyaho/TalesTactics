param([string]$Player="$PSScriptRoot/../Builds/CampaignReview/TalesTactics.exe")
$ErrorActionPreference='Stop'
$playerPath=(Resolve-Path -LiteralPath $Player).Path
$runId=[Guid]::NewGuid().ToString('N')
$saveRoot=Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData/LocalLow/LocalTactics/TalesTactics'
$root=Join-Path $saveRoot "PersistenceReviews/$runId"
$evidence=Join-Path $PSScriptRoot "../Docs/Persistence/$runId"
function OriginalHashes {
    @('campaign.json','campaign.json.bak','settings.json','campaign-slot2.json','campaign-slot3.json') | ForEach-Object {
        $path=Join-Path $saveRoot $_
        if(Test-Path -LiteralPath $path -PathType Leaf){"$($_):$((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash)"}else{"$($_):missing"}
    }
}
$before=@(OriginalHashes)
New-Item -ItemType Directory -Force -Path $root,$evidence | Out-Null
foreach($mode in @('spd','ct')) {
    New-Item -ItemType Directory -Force -Path (Join-Path $root $mode) | Out-Null
    foreach($phase in @('save','resume')) {
        $log=Join-Path $root "$mode/$phase-player.log"
        $process=Start-Process -FilePath $playerPath -ArgumentList @('--persistence-review',$runId,$mode,$phase,'-screen-fullscreen','0','-screen-width','1280','-screen-height','800','-logFile',"`"$log`"") -WindowStyle Hidden -PassThru
        if(!$process.WaitForExit(60000)){Stop-Process -Id $process.Id;throw "$mode/$phase timed out"}
        if($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath (Join-Path $root "$mode/$phase-passed.txt"))){throw "$mode/$phase failed: $root"}
        Write-Output "$mode/$phase PASS"
    }
}
$after=@(OriginalHashes)
if(($before -join '|') -ne ($after -join '|')){throw 'User save/settings changed'}
$errors=@(Get-ChildItem -LiteralPath $root -Recurse -Filter '*-player.log' | Select-String -Pattern 'Exception|Assertion|\bError\b' | ForEach-Object {$_.Line})
if($errors.Count){throw ($errors -join "`n")}
foreach($mode in @('spd','ct')) {
    $target=Join-Path $evidence $mode; New-Item -ItemType Directory -Force -Path $target | Out-Null
    Get-ChildItem -LiteralPath (Join-Path $root $mode) -File | Where-Object {$_.Extension -ne '.log'} | ForEach-Object {Copy-Item -LiteralPath $_.FullName -Destination $target}
}
[ordered]@{passed=$true;runId=$runId;userFiles=$after;logErrors=$errors;playerSHA256=(Get-FileHash -LiteralPath $playerPath).Hash}|ConvertTo-Json -Depth 4|Set-Content (Join-Path $evidence 'summary.json') -Encoding utf8
Write-Output "PASS: four independent launches; user saves/settings unchanged. $evidence"
