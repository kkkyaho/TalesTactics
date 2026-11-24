[CmdletBinding()]
param([Parameter(Mandatory)][string]$Output)
# Read-only build-proof verification. This script never executes either player.
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$repo='C:/dev/TalesTactics'
$public=Join-Path $repo 'Builds/SourceOnlyPublish20261118/Project'
$evidence=Join-Path $repo 'Logs/RosterMotion20261125'
$Output=[IO.Path]::GetFullPath($Output)
if(!$Output.StartsWith([IO.Path]::GetFullPath($evidence).TrimEnd('\','/')+'\',[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $Output)){throw 'Fresh task-local evidence output required'}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
$freezePath=Join-Path $evidence 'validation-product-freeze.json'
$freezeHash=Hash $freezePath
$freezeJson=[IO.File]::ReadAllText($freezePath)
$freeze=$freezeJson|ConvertFrom-Json
if($freeze.schemaVersion -ne 1 -or @($freeze.files).Count -ne 83 -or @($freeze.files.relative|Sort-Object -Unique).Count -ne 83){throw 'Expected the exact 83-file product freeze'}
$sourceNow=@(foreach($entry in $freeze.files){
 if($entry.relative -match '(^/|\\|(^|/)\.\.?(/|$)|(^|/)LocalMusic(/|$))' -or [IO.Path]::IsPathRooted($entry.relative)){throw 'Invalid source-freeze path'}
 $rootPath=Join-Path $repo $entry.relative;$publicPath=Join-Path $public $entry.relative
 $rootHash=Hash $rootPath;$publicHash=Hash $publicPath
 [pscustomobject]@{relative=$entry.relative;passed=($rootHash -ceq $entry.sha256 -and $publicHash -ceq $entry.sha256 -and (Get-Item -LiteralPath $rootPath).Length -eq $entry.bytes -and (Get-Item -LiteralPath $publicPath).Length -eq $entry.bytes)}
})
$currentSourceMatched=@($sourceNow|Where-Object {!$_.passed}).Count -eq 0
if((Hash $freezePath) -cne $freezeHash){throw 'Source-freeze document changed while reading'}
$expectedPayload=@(foreach($id in @('cless','farah','jade','kisara','mint','natalia','shionne','tear','velvet')){
 'Assets/TalesTactics/Art/RosterMotion20261125/'+$id+'/idle-v1.png'
 'Assets/TalesTactics/Art/RosterMotion20261125/'+$id+'/action-v1.png'
 'Assets/TalesTactics/Resources/TalesTactics/CharacterMotion/'+$id+'.asset'
})+@('Assets/TalesTactics/Art/VisualPilot/Alphen/idle-v1.png','Assets/TalesTactics/Art/VisualPilot/Alphen/action-v1.png','Assets/TalesTactics/Resources/TalesTactics/VisualPilot/AlphenMotion.asset')
function Source-Snapshot-Matches($snapshot){
 if($null -eq $snapshot -or $snapshot.passed -ne $true -or $snapshot.sha256 -cne $freezeHash -or $snapshot.fileCount -ne 83 -or @($snapshot.files).Count -ne 83 -or $snapshot.manifestJson -cne $freezeJson){return $false}
 if(@($snapshot.files.relative|Sort-Object -Unique).Count -ne 83){return $false}
 foreach($entry in $freeze.files){
  $matches=@($snapshot.files|Where-Object {$_.relative -ceq $entry.relative})
  if($matches.Count -ne 1){return $false}
  $row=$matches[0]
  if($row.passed -ne $true -or $row.expectedSHA256 -cne $entry.sha256 -or $row.rootSHA256 -cne $entry.sha256 -or $row.publicSHA256 -cne $entry.sha256 -or $row.expectedBytes -ne $entry.bytes -or $row.rootBytes -ne $entry.bytes -or $row.publicBytes -ne $entry.bytes){return $false}
 }
 return $true
}
$rows=@(foreach($platform in @('windows','android')){
 $reportPath=Join-Path $evidence ('Builds/'+$platform+'-build-final.json')
 $build=Get-Content -LiteralPath $reportPath -Raw|ConvertFrom-Json
 $relative=if($platform -eq 'windows'){'Builds/RosterMotion20261125/Windows/TalesTactics.exe'}else{'Builds/RosterMotion20261125/Android/TalesTactics.apk'}
 $artifact=[IO.Path]::GetFullPath((Join-Path $public $relative))
 if([IO.Path]::GetFullPath($build.outputPath) -ine $artifact){throw 'Unexpected build output path'}
 $resultPath=Join-Path ([IO.Path]::GetDirectoryName($artifact)) 'build-result.json'
 $inner=Get-Content -LiteralPath $resultPath -Raw|ConvertFrom-Json
 $queuedPath=Join-Path $evidence ('Builds/'+$platform+'-queued.json')
 $queued=Get-Content -LiteralPath $queuedPath -Raw|ConvertFrom-Json
 $runMatched=$build.runId -cmatch '^[0-9a-f]{32}$' -and $build.runId -ceq $inner.runId -and $build.runId -ceq $queued.runId -and [IO.Path]::GetFullPath($inner.outputPath) -ieq $artifact
 $exists=Test-Path -LiteralPath $artifact -PathType Leaf
 $size=if($exists){(Get-Item -LiteralPath $artifact).Length}else{0}
 $hash=if($exists){Hash $artifact}else{$null}
 $artifactMatched=$exists -and $size -gt 0 -and $hash -ceq $build.artifactSHA256 -and $size -eq $build.artifactBytes
 $sourceMatched=$currentSourceMatched -and $build.sourceFreezeMatched -eq $true -and $build.sourceFileCount -eq 83 -and $build.sourceFreezeSHA256 -ceq $freezeHash -and $queued.sourceFreezeSHA256 -ceq $freezeHash -and $queued.sourceFileCount -eq 83 -and (Source-Snapshot-Matches $build.sourceBefore) -and (Source-Snapshot-Matches $build.sourceAfter)
 $payloadRows=@($build.packedPayload)
 $payloadMatched=$build.payloadVerified -eq $true -and $build.requiredPayloadCount -eq 30 -and $payloadRows.Count -eq 30 -and @($build.missingPayload).Count -eq 0 -and @($payloadRows.path|Sort-Object -Unique).Count -eq 30
 foreach($path in $expectedPayload){
  $matches=@($payloadRows|Where-Object {$_.path -ceq $path})
  if($matches.Count -ne 1 -or $matches[0].occurrences -lt 1){$payloadMatched=$false}
 }
 $buildReport=Join-Path $evidence ('Builds/'+$platform+'.buildreport')
 $buildReportMatched=(Test-Path -LiteralPath $buildReport -PathType Leaf) -and (Hash $buildReport) -ceq $build.buildReportSHA256
 $passed=$build.passed -eq $true -and $build.result -ceq 'Succeeded' -and $inner.result -ceq 'Succeeded' -and $inner.errors -eq 0 -and $build.settingsEqual -eq $true -and $build.errors -eq 0 -and $build.sourceOnly -eq $true -and $build.localMusicChecked -eq $true -and $build.containsLocalMusic -eq $false -and $artifactMatched -and $runMatched -and $sourceMatched -and $payloadMatched -and $buildReportMatched
 [pscustomobject]@{platform=$platform;runId=$build.runId;passed=$passed;exists=$exists;bytes=$size;sha256=$hash;artifactMatched=$artifactMatched;runMatched=$runMatched;sourceMatched=$sourceMatched;sourceFreezeSHA256=$build.sourceFreezeSHA256;sourceFileCount=$build.sourceFileCount;payloadMatched=$payloadMatched;requiredPayloadCount=30;packedPayload=$payloadRows;buildReportMatched=$buildReportMatched;buildReportSHA256=$build.buildReportSHA256;errors=$build.errors;warnings=$build.warnings;seconds=$build.seconds;settingsRestored=$build.settingsEqual;sourceOnly=$build.sourceOnly;containsLocalMusic=$build.containsLocalMusic;reportSHA256=Hash $reportPath;resultSHA256=Hash $resultPath;queuedSHA256=Hash $queuedPath;messages=$build.messages}
})
$result=[ordered]@{schema=2;passed=(@($rows|Where-Object {!$_.passed}).Count -eq 0);checkedUtc=[DateTime]::UtcNow.ToString('o');sourceFreezeSHA256=$freezeHash;sourceFileCount=83;currentSourceMatched=$currentSourceMatched;builds=$rows;scope='Completed BuildReport packedAssets evidence for the exact 30 required roster resources, before/after SHA-256 for the exact 83 frozen product/import-layout files in ROOT and PUBLIC, one run ID across queued/inner/final reports, and builder-to-artifact/report SHA-256 binding. This is not an all-repository source inventory. APK structure/signature and managed isolation have separate reports. No standalone-player execution, Android installation or physical-device gameplay is claimed.'}
$bytes=[Text.UTF8Encoding]::new($false).GetBytes(($result|ConvertTo-Json -Depth 8))
$stream=[IO.File]::Open($Output,'CreateNew','Write','Read')
try{$stream.Write($bytes,0,$bytes.Length)}finally{$stream.Dispose()}
$result|ConvertTo-Json -Depth 6
if(!$result.passed){exit 1}
