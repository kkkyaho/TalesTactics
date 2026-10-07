param(
    [ValidatePattern('^[a-fA-F0-9]{32}$')][string]$RunId = [Guid]::NewGuid().ToString('N'),
    [switch]$PrepareOnly,
    [switch]$Hidden
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$player = Join-Path $projectRoot 'Builds/CampaignReview/TalesTactics.exe'
$runtime = Join-Path $projectRoot 'Builds/CampaignReview/TalesTactics_Data/Managed/TalesTactics.Runtime.dll'
if (!(Test-Path -LiteralPath $player) -or !(Test-Path -LiteralPath $runtime)) { throw 'Build the CampaignReview development player first. See Docs/PLAYER_REVIEW.md.' }
if (!$PrepareOnly) {
    $running=Get-CimInstance Win32_Process -Filter "Name = 'TalesTactics.exe'" | Where-Object { $_.CommandLine -like '*--manual-review*' -and $_.CommandLine -match [regex]::Escape($RunId) }
    if ($running) { throw 'This playtest is already running. Close its player before resuming the same save.' }
}
$saveRoot = Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData/LocalLow/LocalTactics/TalesTactics'
$reviewRoot = Join-Path $saveRoot "ManualReviews/$RunId"
New-Item -ItemType Directory -Force -Path $reviewRoot | Out-Null
$feedback = Join-Path $reviewRoot 'feedback.csv'
if (!(Test-Path -LiteralPath $feedback)) {
    'chapter,party,entry_levels,attempts,minutes,difficulty_1_easy_3_fair_5_hard,pacing_1_slow_3_fair_5_fast,reward_1_low_3_fair_5_high,issue,desired_change' | Set-Content -LiteralPath $feedback -Encoding utf8
    1..6 | ForEach-Object { "$_,,,,,,,,," } | Add-Content -LiteralPath $feedback -Encoding utf8
}
$launchId = [Guid]::NewGuid().ToString('N')
$log = Join-Path $reviewRoot "player-$launchId.log"
$userFiles = @('campaign.json','campaign.json.bak','settings.json','campaign-slot2.json','campaign-slot3.json') | ForEach-Object {
    $path = Join-Path $saveRoot $_
    [ordered]@{name=$_;sha256=if(Test-Path -LiteralPath $path -PathType Leaf){(Get-FileHash -LiteralPath $path).Hash}else{$null}}
}
# Existing manual instrumentation appends events but overwrites performance/hardware on launch.
# Preserve those observations before resuming this isolated save.
if (!$PrepareOnly) {
    foreach($name in @('performance.csv','hardware.txt')) {
        $path=Join-Path $reviewRoot $name
        if(Test-Path -LiteralPath $path){Copy-Item -LiteralPath $path -Destination (Join-Path $reviewRoot "$launchId-previous-$name")}
    }
}
$manifest = [ordered]@{
    runId=$RunId;launchId=$launchId;preparedOnly=[bool]$PrepareOnly;utc=[DateTime]::UtcNow.ToString('o')
    player=$player;runtimeSHA256=(Get-FileHash -LiteralPath $runtime).Hash;userFilesBefore=$userFiles
    feedback=$feedback;log=$log;inputSource='Unclassified: human feedback must be recorded separately.'
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $reviewRoot "$launchId-manifest.json") -Encoding utf8
$processId = $null
if (!$PrepareOnly) {
    $style=if($Hidden){'Hidden'}else{'Normal'}
    $process=Start-Process -FilePath $player -ArgumentList @('--manual-review',$RunId,'-screen-fullscreen','0','-screen-width','1366','-screen-height','768','-logFile',"`"$log`"") -WindowStyle $style -PassThru
    $processId=$process.Id
}
[pscustomobject]@{RunId=$RunId;ProcessId=$processId;Folder=$reviewRoot;Feedback=$feedback;Log=$log;ResumeCommand="./Tools/StartBalancePlaytest.ps1 -RunId $RunId"}
