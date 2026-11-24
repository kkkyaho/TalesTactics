[CmdletBinding()]
param([Parameter(Mandatory)][string]$Manifest,[Parameter(Mandatory)][string]$Output,[switch]$Apply)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$repo='C:/dev/TalesTactics';$public=Join-Path $repo 'Builds/SourceOnlyPublish20261118/Project'
$baseline=Join-Path $repo 'Logs/RosterMotion20261125/Baseline-bdadaad52bb0415ea7e0fe660d597031'
function Full([string]$root,[string]$relative){if([IO.Path]::IsPathRooted($relative)){throw 'Relative path required'};$p=[IO.Path]::GetFullPath((Join-Path $root $relative));if(!$p.StartsWith([IO.Path]::GetFullPath($root).TrimEnd('\','/')+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Path escaped checkout'};return $p}
function Hash([string]$path){if(Test-Path -LiteralPath $path -PathType Leaf){return (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()};return $null}
function NoReparse([string]$path){$p=Get-Item -LiteralPath $path -Force;while($null -ne $p){if(($p.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'Reparse point in path'};$p=if($p -is [IO.FileInfo]){$p.Directory}else{$p.Parent}}}
function WriteNew([string]$path,$value){$bytes=[Text.UTF8Encoding]::new($false).GetBytes(($value|ConvertTo-Json -Depth 10));$s=[IO.File]::Open($path,'CreateNew','Write','Read');try{$s.Write($bytes,0,$bytes.Length)}finally{$s.Dispose()}}
$Output=[IO.Path]::GetFullPath($Output);$logs=Join-Path $repo 'Logs/RosterMotion20261125'
if(!$Output.StartsWith([IO.Path]::GetFullPath($logs).TrimEnd('\','/')+'\',[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $Output)){throw 'Fresh output directory beneath this task Logs required'}
$paths=@(Get-Content -LiteralPath $Manifest | ForEach-Object {$_.Trim()} | Where-Object {$_ -and !$_.StartsWith('#')})
if($paths.Count -eq 0 -or @($paths|Sort-Object -Unique).Count -ne $paths.Count){throw 'Nonempty unique explicit manifest required'}
$before=Get-Content -LiteralPath (Join-Path $baseline 'git-validation-before.json') -Raw|ConvertFrom-Json
$protectedUser=@($before.trackedStatus)
$rows=@(foreach($relative in $paths){
 if(($relative -notmatch '^(Assets/TalesTactics/|Tools/RosterMotion20261125/|Docs/RosterMotion20261125/)' -and $relative -notin @('README.md','Docs/TODO.md','Docs/ARCHITECTURE.md','Docs/VALIDATION.md')) -or $relative -match '(^|/)(LocalMusic|StreamingAssets|\.git)(/|$)' -or $relative -match '\.(mp3|wav|ogg|apk|aab|exe|dll|zip)$'){throw ('Path outside explicit source-only scope: '+$relative)}
 if($protectedUser -contains $relative){throw ('Preexisting PUBLIC user modification must not be overwritten: '+$relative)}
 $source=Full $repo $relative;$target=Full $public $relative
 if(!(Test-Path -LiteralPath $source -PathType Leaf)){throw ('Source missing: '+$relative)}
 NoReparse $source;NoReparse $public
 if(Test-Path -LiteralPath $target){NoReparse $target}
 $sourceSha=Hash $source;$targetSha=Hash $target
 [pscustomobject]@{relative=$relative;source=$source;target=$target;sourceSha256=$sourceSha;beforeSha256=$targetSha;skipEqual=($sourceSha -ceq $targetSha);afterSha256=$null;copied=$false}
})
[IO.Directory]::CreateDirectory($Output)|Out-Null
WriteNew (Join-Path $Output 'plan.json') ([ordered]@{manifestSha256=(Hash $Manifest);apply=[bool]$Apply;files=$rows})
$locked=$false;$fail=$null
try{
 if($Apply){
  $cli='C:/Users/USER-PC/AppData/Local/Unity/bin/unity.exe'
  $guard='if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode||UnityEditor.EditorApplication.isCompiling||UnityEditor.EditorApplication.isUpdating||UnityEditor.BuildPipeline.isBuildingPlayer)throw new System.InvalidOperationException("Stopped idle validation Editor required");if(UnityEditor.SessionState.GetBool("TalesTactics.RosterMotionRefreshLock",false))throw new System.InvalidOperationException("Prior owned lock active");UnityEditor.AssetDatabase.DisallowAutoRefresh();UnityEditor.SessionState.SetBool("TalesTactics.RosterMotionRefreshLock",true);return true;'
  $raw=& $cli command eval --project-path $public --caller plugin --skill unity-pipeline --format json -- --code $guard
  $r=($raw -join [Environment]::NewLine)|ConvertFrom-Json;WriteNew (Join-Path $Output 'refresh-lock.json') $r
  if(!$r.success -or !$r.data.result.success){throw 'Refresh suspension failed'};$locked=$true
  foreach($row in $rows){
   if((Hash $row.source) -cne $row.sourceSha256 -or (Hash $row.target) -cne $row.beforeSha256){throw ('Concurrent file change: '+$row.relative)}
   if(!$row.skipEqual){
    if($null -ne $row.beforeSha256){$backup=Full (Join-Path $Output 'Before') $row.relative;[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($backup))|Out-Null;[IO.File]::Copy($row.target,$backup,$false);if((Hash $backup) -cne $row.beforeSha256){throw 'Backup hash mismatch'}}
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($row.target))|Out-Null;[IO.File]::Copy($row.source,$row.target,$true);$row.copied=$true
   }
   $row.afterSha256=Hash $row.target;if($row.afterSha256 -cne $row.sourceSha256){throw 'Sync hash mismatch'}
  }
 }
}catch{$fail=$_.Exception.ToString();throw}
finally{
 if($locked){$raw=& $cli command eval --project-path $public --caller plugin --skill unity-pipeline --format json -- --code 'if(UnityEditor.SessionState.GetBool("TalesTactics.RosterMotionRefreshLock",false)){UnityEditor.AssetDatabase.AllowAutoRefresh();UnityEditor.SessionState.SetBool("TalesTactics.RosterMotionRefreshLock",false);}UnityEditor.AssetDatabase.Refresh();return true;';$unlocked=($raw -join [Environment]::NewLine)|ConvertFrom-Json;WriteNew (Join-Path $Output 'refresh-unlock.json') $unlocked;if(!$unlocked.success -or !$unlocked.data.result.success){$fail='Refresh unlock failed; inspect saved evidence and release owned lock'}}
 WriteNew (Join-Path $Output 'result.json') ([ordered]@{passed=($null -eq $fail);applied=[bool]$Apply;count=$rows.Count;copied=@($rows|Where-Object copied).Count;skippedIdentical=@($rows|Where-Object skipEqual).Count;files=$rows;error=$fail})
}
if($null -ne $fail){throw $fail}
