[CmdletBinding()]
param([string[]]$Characters=@('cless','mint','farah','tear','jade','natalia','velvet','alphen','shionne','kisara'),[string]$Batch='Initial', [switch]$ReusePassingActual, [switch]$ActualOnly, [string]$MotionSuffix='')
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$repo='C:/dev/TalesTactics';$tools=Join-Path $repo 'Tools/RosterMotion20261125';$logs=Join-Path $repo 'Logs/RosterMotion20261125';$root=Join-Path $logs 'Captures';$runner=Join-Path $tools 'InvokeUnity.ps1';$pwsh='C:/Users/USER-PC/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/powershell/pwsh.exe'
if($Batch -notmatch '^[A-Za-z0-9_-]+$'){throw 'Simple batch name required'}
$allowed=@('cless','mint','farah','tear','jade','natalia','velvet','alphen','shionne','kisara');if(@($Characters|Where-Object{$_ -notin $allowed}).Count){throw 'Unknown character'}
$summary=Join-Path $logs ('capture-batch-'+$Batch+'.json');if(Test-Path -LiteralPath $summary){throw 'Fresh batch required'}
$rows=[Collections.Generic.List[object]]::new()
function Call([string]$file,[string]$entry,[object[]]$arg,[string]$output){
 $json=ConvertTo-Json -InputObject @($arg) -Compress
 & $pwsh -NoProfile -File $runner -Operation RunScript -File (Join-Path $tools $file) -Entry $entry -ArgumentsJson $json -Output $output
 if($LASTEXITCODE -ne 0){throw "Unity invocation failed: $output"}
 $response=Get-Content -LiteralPath ($output+'.normalized.json') -Raw|ConvertFrom-Json
 if($response.result.PSObject.Properties['success'] -and !$response.result.success){throw "Unity script failed: $output"}
}
function AwaitReport([string]$path){
 $end=[DateTime]::UtcNow.AddSeconds(200)
 while([DateTime]::UtcNow -lt $end){
  if(Test-Path -LiteralPath $path){
   try{$r=Get-Content -LiteralPath $path -Raw|ConvertFrom-Json}catch{$r=$null}
   if($null -ne $r -and ($r.complete -or $r.status -in @('failed','Stopped or failed'))){if(!$r.passed){throw "Capture did not pass: $path"};return $r}
  }
  Start-Sleep -Seconds 3
 }
 throw "Capture report did not finish within 200 seconds: $path"
}
try{
 foreach($id in $Characters){
  $dir=Join-Path $root $id;[IO.Directory]::CreateDirectory($dir)|Out-Null
  $actual=Join-Path $dir 'Actual';$report=Join-Path $actual 'result.json'
  if($ReusePassingActual -and (Test-Path -LiteralPath $report)){$a=Get-Content -LiteralPath $report -Raw|ConvertFrom-Json;if(!$a.passed){throw 'Existing actual failed'}}else{
   Call 'RosterActualEpisode6Review.cs' 'TalesTactics.RosterActualEpisode6Review.BeginEditor' @($actual,$id) (Join-Path $dir ('actual-start-'+$Batch+'.json'))
   $a=AwaitReport $report
  }
  $rows.Add([ordered]@{characterId=$id;kind='Actual';passed=$a.passed;captures=$a.completedCaptures;report=$report})
  if($ActualOnly){continue}
  foreach($kind in @('Before','After')){
   $output=Join-Path $dir ($kind+$MotionSuffix);$pilot=$kind -eq 'After'
   Call 'RosterMotionCapture.cs' 'TalesTactics.Editor.RosterMotionCapture.Begin' @($output,$id,$pilot) (Join-Path $dir ($kind.ToLower()+'-start-'+$Batch+'.json'))
   $r=AwaitReport (Join-Path $output 'result.json')
   $rows.Add([ordered]@{characterId=$id;kind=$kind;passed=$r.passed;captures=$r.pngCount;report=(Join-Path $output 'result.json')})
   [IO.File]::WriteAllText($summary+'.progress.json',([ordered]@{utc=[DateTime]::UtcNow.ToString('o');rows=$rows}|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
  }
  & $pwsh -NoProfile -File (Join-Path $tools 'BuildComparison.ps1') -BeforeResult (Join-Path $dir ('Before'+$MotionSuffix+'/result.json')) -AfterResult (Join-Path $dir ('After'+$MotionSuffix+'/result.json')) -OutputHtml (Join-Path $dir 'comparison.html')
  if($LASTEXITCODE -ne 0){throw 'Comparison build failed'}
 }
 [IO.File]::WriteAllText($summary,([ordered]@{passed=$true;utc=[DateTime]::UtcNow.ToString('o');rows=$rows}|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false))
}catch{
 [IO.File]::WriteAllText($summary,([ordered]@{passed=$false;utc=[DateTime]::UtcNow.ToString('o');error=$_.Exception.Message;rows=$rows}|ConvertTo-Json -Depth 12),[Text.UTF8Encoding]::new($false));throw
}
