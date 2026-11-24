[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('ReadEditor','Recompile','CompileStatus','StartTests','TestStatus','RunScript','CompileScript','EvalFile','SwitchTarget','SwitchStatus')][string]$Operation,[Parameter(Mandatory)][string]$Output,[string]$File,[string]$Entry,[string]$ArgumentsJson,[ValidateSet('editor','playmode')][string]$Mode='editor',[string]$Filter,[ValidateSet('StandaloneWindows64','Android')][string]$Target='StandaloneWindows64')
Set-StrictMode -Version Latest;$ErrorActionPreference='Stop'
$repo='C:/dev/TalesTactics';$project=Join-Path $repo 'Builds/SourceOnlyPublish20261118/Project';$logs=Join-Path $repo 'Logs/RosterMotion20261125'
$Output=[IO.Path]::GetFullPath($Output)
if(!$Output.StartsWith([IO.Path]::GetFullPath($logs).TrimEnd('\','/')+'\',[StringComparison]::OrdinalIgnoreCase)-or(Test-Path -LiteralPath $Output)){throw 'Fresh task-local output required'}
$commands=@{ReadEditor='editor_status';Recompile='recompile';CompileStatus='recompile_status';StartTests='run_tests';TestStatus='test_status';RunScript='run_script';CompileScript='run_script';EvalFile='eval_file';SwitchTarget='switch_build_target';SwitchStatus='switch_build_target_status'}
$invoke=@('command',$commands[$Operation],'--project-path',$project,'--caller','plugin','--skill','unity-pipeline','--format','json')
$params=@()
if($Operation -in @('RunScript','CompileScript','EvalFile')){
 $File=[IO.Path]::GetFullPath($File);if(!$File.StartsWith([IO.Path]::GetFullPath((Join-Path $repo 'Tools/RosterMotion20261125')).TrimEnd('\','/')+'\',[StringComparison]::OrdinalIgnoreCase)-or!(Test-Path -LiteralPath $File)){throw 'Owned external verification file required'}
 $params+=@('--file',$File)
 if($Entry){$params+=@('--entry',$Entry)}
 if($ArgumentsJson){$parsed=ConvertFrom-Json -InputObject $ArgumentsJson;if($null -eq $parsed){throw 'Arguments array required'};$params+=@('--args',$ArgumentsJson)}
 if($Operation -eq 'CompileScript'){$params+=@('--dry_run','true')}
}
if($Operation -eq 'StartTests'){$params+=@('--mode',$Mode,'--async_tests','true','--timeout','1800');if($Filter){$params+=@('--filter',$Filter)}}
if($Operation -eq 'SwitchTarget'){$params+=@('--target',$Target,'--confirm','true')}
if($params.Count){$invoke+=@('--')+$params}
$started=[DateTime]::UtcNow.ToString('o');$raw=& 'C:/Users/USER-PC/AppData/Local/Unity/bin/unity.exe' @invoke;$exit=$LASTEXITCODE
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($Output))|Out-Null
[IO.File]::WriteAllText($Output,($raw -join [Environment]::NewLine),[Text.UTF8Encoding]::new($false))
$r=Get-Content -LiteralPath $Output -Raw|ConvertFrom-Json
$value=if($null -ne $r.data -and $r.data.PSObject.Properties['result']){$r.data.result}else{$r.errors}
for($i=0;$i -lt 3 -and $value -is [string] -and $value.TrimStart().StartsWith('{');$i++){$value=$value|ConvertFrom-Json}
$report=[ordered]@{operation=$Operation;startedUtc=$started;finishedUtc=[DateTime]::UtcNow.ToString('o');cliExitCode=$exit;success=$r.success;output=$Output;result=$value}
[IO.File]::WriteAllText($Output+'.normalized.json',($report|ConvertTo-Json -Depth 100),[Text.UTF8Encoding]::new($false))
[pscustomobject]@{operation=$Operation;success=$r.success;cliExitCode=$exit;output=$Output;normalized=($Output+'.normalized.json')}|ConvertTo-Json
if(!$r.success -or $exit -ne 0){exit 1}