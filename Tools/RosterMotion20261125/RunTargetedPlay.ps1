[CmdletBinding()]param([string]$Batch='Initial',[int]$SkipFirst=0)
Set-StrictMode -Version Latest;$ErrorActionPreference='Stop'
$tools='C:/dev/TalesTactics/Tools/RosterMotion20261125';$logs='C:/dev/TalesTactics/Logs/RosterMotion20261125/Tests';$pwsh='C:/Users/USER-PC/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe'
$filters=@('AttackRecoversGuardPersistsAndMissingPoseFallsBack','ConsecutiveAttackMotionRestartsWithoutRestartingTimingEveryFrame','EnvironmentGroundingTracksRealMovementAndFourCameraDirections','ArtDirectionsMotionAndCameraRotationRemainConnected','CollapseHoldsLastFrameAcrossSyncAndRevivalRestoresIdle','VisualPilotEnvironmentTests')
$rows=[Collections.Generic.List[object]]::new();$summary=Join-Path $logs ('targeted-play-'+$Batch+'.json');if(Test-Path -LiteralPath $summary){throw 'Fresh summary required'}
try{foreach($filter in @($filters | Select-Object -Skip $SkipFirst)){
 $prefix=Join-Path $logs ($Batch+'-'+$filter)
 $idle=$false
 for($wait=0;$wait -lt 12;$wait++){
  $editor=$prefix+'-editor-'+$wait+'.json'
  & $pwsh -NoProfile -File (Join-Path $tools 'InvokeUnity.ps1') -Operation ReadEditor -Output $editor
  if($LASTEXITCODE -ne 0){throw 'Editor state query failed'}
  $e=(Get-Content ($editor+'.normalized.json') -Raw|ConvertFrom-Json).result
  if($e.playMode -eq 'stopped' -and !$e.compiling -and !$e.domainReloadInProgress){$idle=$true;break}
  Start-Sleep -Seconds 3
 }
 if(!$idle){throw 'Editor did not finish previous test lifecycle'}
 Start-Sleep -Seconds 5
 & $pwsh -NoProfile -File (Join-Path $tools 'InvokeUnity.ps1') -Operation StartTests -Mode playmode -Filter $filter -Output ($prefix+'-start.json')
 if($LASTEXITCODE -ne 0){throw "Start failed: $filter"}
 $end=[DateTime]::UtcNow.AddMinutes(3);$complete=$false;$index=0
 while([DateTime]::UtcNow -lt $end){
  Start-Sleep -Seconds 5;$status=$prefix+'-status-'+(++$index)+'.json'
  & $pwsh -NoProfile -File (Join-Path $tools 'InvokeUnity.ps1') -Operation TestStatus -Output $status
  if($LASTEXITCODE -ne 0){throw "Status failed: $filter"}
  $r=(Get-Content ($status+'.normalized.json') -Raw|ConvertFrom-Json).result
  if($r.status -eq 'completed'){$complete=$true;break}
 }
 if(!$complete){throw "Test timeout: $filter"}
 $count=if($filter -eq 'VisualPilotEnvironmentTests'){2}else{1}
 if($r.summary.total -ne $count -or $r.summary.passed -ne $count -or $r.summary.failed -ne 0){throw "Test did not pass exact expected count: $filter"}
 $rows.Add([ordered]@{filter=$filter;summary=$r.summary;duration=$r.duration;status=$status;results=$r.results})
}
[IO.File]::WriteAllText($summary,([ordered]@{passed=$true;rows=$rows}|ConvertTo-Json -Depth 30),[Text.UTF8Encoding]::new($false))
}catch{[IO.File]::WriteAllText($summary,([ordered]@{passed=$false;error=$_.Exception.Message;rows=$rows}|ConvertTo-Json -Depth 30),[Text.UTF8Encoding]::new($false));throw}
