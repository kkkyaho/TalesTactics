param([string]$Player="$PSScriptRoot/../Builds/WindowsVisualUxReview/TalesTactics.exe")
$ErrorActionPreference='Stop'
$playerPath=(Resolve-Path -LiteralPath $Player).Path
$runId=[Guid]::NewGuid().ToString('N')
$root=Join-Path $env:USERPROFILE 'AppData/LocalLow/LocalTactics/TalesTactics'
$review=Join-Path $root "Reviews/$runId"
$evidence=Join-Path $PSScriptRoot '../Docs/VisualUx'
New-Item -ItemType Directory -Force -Path $review | Out-Null
function UserFiles {
 foreach($name in @('campaign.json','campaign.json.bak','campaign.json.v1.bak','campaign.json.v2.bak','settings.json','settings.json.bak')){
  $path=Join-Path $root $name
  [pscustomobject]@{file=$name;sha256=if(Test-Path -LiteralPath $path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash}else{'missing'}}
 }
}
$before=@(UserFiles);$errorText=$null;$exitCode=$null;$report=$null
try {
 $log=Join-Path $review 'chapter1-player.log'
 $quotedLog='"'+$log+'"'
 $process=Start-Process -FilePath $playerPath -ArgumentList @('--campaign-review',$runId,'chapter1','-screen-fullscreen','0','-screen-width','1280','-screen-height','800','-logFile',$quotedLog) -WindowStyle Hidden -PassThru
 $deadline=[DateTime]::UtcNow.AddSeconds(180)
 while(!$process.WaitForExit(1000)){if([DateTime]::UtcNow -gt $deadline){Stop-Process -Id $process.Id;throw 'Isolated review timed out'}}
 $exitCode=$process.ExitCode
 $report=Get-Content -LiteralPath (Join-Path $review 'chapter1.json') -Raw | ConvertFrom-Json
 Copy-Item -LiteralPath (Join-Path $review 'chapter1.json') -Destination (Join-Path $evidence 'windows-review.json')
 if($exitCode -ne 0 -or !$report.passed -or $report.errors.Count -gt 0){throw 'Player review failed'}
} catch {$errorText=$_.ToString()}
$after=@(UserFiles);$unchanged=($before|ConvertTo-Json -Compress) -eq ($after|ConvertTo-Json -Compress)
$result=[ordered]@{runId=$runId;utc=[DateTime]::UtcNow.ToString('o');passed=(!$errorText -and $unchanged);exitCode=$exitCode;error=$errorText;userSaveUnchanged=$unchanged;before=$before;after=$after;playerSHA256=(Get-FileHash -LiteralPath $playerPath).Hash;runtimeSHA256=(Get-FileHash -LiteralPath (Join-Path (Split-Path $playerPath) 'TalesTactics_Data/Managed/TalesTactics.Runtime.dll')).Hash;report=$report}
$result|ConvertTo-Json -Depth 10|Set-Content (Join-Path $evidence 'windows-player-run.json') -Encoding utf8
if(!$result.passed){throw ($errorText+' User files unchanged: '+$unchanged)}
Write-Output "PASS: chapter1, exit $exitCode, $($report.playerTurns) turns / $($report.attacks) attacks / $($report.seconds)s. User files unchanged."
