[CmdletBinding()]
param([Parameter(Mandatory)][string]$Output)
Set-StrictMode -Version Latest;$ErrorActionPreference='Stop'
$repo='C:/dev/TalesTactics';$public=Join-Path $repo 'Builds/SourceOnlyPublish20261118/Project';$logs=Join-Path $repo 'Logs/RosterMotion20261125';$Output=[IO.Path]::GetFullPath($Output)
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Under([string]$p,[string]$r){[IO.Path]::GetFullPath($p).StartsWith([IO.Path]::GetFullPath($r).TrimEnd('\','/')+'\',[StringComparison]::OrdinalIgnoreCase)}
function Save([string]$p,$value){$bytes=[Text.UTF8Encoding]::new($false).GetBytes(($value|ConvertTo-Json -Depth 9));$stream=[IO.File]::Open($p,'CreateNew','Write','Read');try{$stream.Write($bytes,0,$bytes.Length)}finally{$stream.Dispose()}}
if(!(Under $Output $logs)-or(Test-Path -LiteralPath $Output)){throw 'Fresh task-local output directory required'}
$second=Get-Content -LiteralPath (Join-Path $logs 'Import/second.json') -Raw|ConvertFrom-Json
if(!$second.passed -or !$second.requireStableIds -or @($second.report.sheets).Count -ne 18 -or @($second.report.sheets|Where-Object {$_.preservedSpriteIds -ne 16 -or $_.preservedLocalFileIds -ne 16 -or !$_.nativeSourceUnchanged}).Count -ne 0){throw 'Second import identity proof required'}
$paths=[Collections.Generic.List[string]]::new();$paths.Add('Assets/TalesTactics/Art/RosterMotion20261125.meta');$paths.Add('Assets/TalesTactics/Resources/TalesTactics/CharacterMotion.meta')
foreach($s in $second.report.sheets){if((Hash (Join-Path $repo $s.path)) -cne $s.sha256 -or (Hash (Join-Path $public $s.path)) -cne $s.sha256){throw 'Native PNG differs from successful import'};$paths.Add($s.path+'.meta');$folder=Split-Path -Parent $s.path;$folder=$folder.Replace('\','/');if(!$paths.Contains($folder+'.meta')){$paths.Add($folder+'.meta')}}
foreach($p in $second.report.profiles){$paths.Add($p.asset);$paths.Add($p.asset+'.meta')}
if($paths.Count -ne 47 -or @($paths|Sort-Object -Unique).Count -ne 47){throw 'Expected exact 47 generated metadata/profile paths'}
$rows=@(foreach($relative in $paths){$source=[IO.Path]::GetFullPath((Join-Path $public $relative));$target=[IO.Path]::GetFullPath((Join-Path $repo $relative));if(!(Under $source $public)-or !(Under $target $repo)-or !(Test-Path -LiteralPath $source -PathType Leaf)){throw 'Generated path invalid'};$sha=Hash $source;$exists=Test-Path -LiteralPath $target -PathType Leaf;if($exists -and (Hash $target) -cne $sha){throw ('Existing ROOT file differs and must not be overwritten: '+$relative)};[pscustomobject]@{relative=$relative;source=$source;target=$target;sha256=$sha;skipEqual=$exists;copied=$false}})
[IO.Directory]::CreateDirectory($Output)|Out-Null;Save (Join-Path $Output 'plan.json') $rows
foreach($row in $rows){if((Hash $row.source) -cne $row.sha256){throw 'Source changed before reverse sync'};if(!$row.skipEqual){[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($row.target))|Out-Null;[IO.File]::Copy($row.source,$row.target,$false);$row.copied=$true};if((Hash $row.target) -cne $row.sha256){throw 'Reverse sync mismatch'}}
Save (Join-Path $Output 'result.json') ([ordered]@{passed=$true;count=$rows.Count;copied=@($rows|Where-Object copied).Count;skippedIdentical=@($rows|Where-Object skipEqual).Count;pngBytesChanged=0;files=$rows})
$rows.relative|Set-Content -LiteralPath (Join-Path $Output 'generated-files.txt')
[pscustomobject]@{passed=$true;count=$rows.Count;copied=@($rows|Where-Object copied).Count;pngBytesChanged=0}|ConvertTo-Json