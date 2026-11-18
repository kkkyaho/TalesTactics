[CmdletBinding()]
param(
 [Parameter(Mandatory=$true)][ValidateSet('Before','After')][string]$Phase,
 [Parameter(Mandatory=$true)][ValidateSet('Normal','Review')][string]$Kind,
 [Parameter(Mandatory=$true)][string]$SourceLabel,
 [string]$BeforeManifestPath,
 [string]$BuildResultPath,
 [string]$TestSourceManifestPath,
 [string]$ProjectRoot='C:/dev/TalesTactics'
)
# Run Before separately immediately before each build. Run After before launching that build.
# Both phases create new GUID evidence directories; old receipts are never overwritten.
# No Unity control, build, player execution, source/save mutation, git mutation or upload.
# All Assets/Packages/ProjectSettings files are included, including untracked/ignored local music.
# This is a conservative project-input inventory, not an exact packed-asset dependency list.
# Installed Unity binaries, Library/PackageCache and imported Library artifacts are not hashed;
# their version/dependency identity is recorded via ProjectVersion and package manifest/lock.
# Endpoint equality plus BuildReport gives source/build provenance, not a hermetic rebuild proof.
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$utf8=New-Object Text.UTF8Encoding($false)
$project=[IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\','/')
if(!$project.Equals('C:\dev\TalesTactics',[StringComparison]::OrdinalIgnoreCase)){throw 'This recorder is scoped to C:/dev/TalesTactics.'}
$expectedBuild=Join-Path $project $(if($Kind -eq 'Normal'){'Builds/AllStageTacticsWindows20261118'}else{'Builds/AllStageTacticsReview20261118'})
$helper=Join-Path $project 'Docs/AllStageTactics/Validation/Build-AllStageTactics.cs'
$runId=[Guid]::NewGuid().ToString('N')
$evidence=Join-Path $project ('Logs/AllStageTactics20261118/BuildSource-'+$Kind+'-'+$Phase+'-'+$runId)
$started=[DateTime]::UtcNow
$failures=New-Object 'System.Collections.Generic.List[string]'

function Same-Path([string]$A,[string]$B){[IO.Path]::GetFullPath($A).TrimEnd('\','/').Equals([IO.Path]::GetFullPath($B).TrimEnd('\','/'),[StringComparison]::OrdinalIgnoreCase)}
function Under-Path([string]$Child,[string]$Parent){[IO.Path]::GetFullPath($Child).StartsWith([IO.Path]::GetFullPath($Parent).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)}
function Assert-PlainPath([string]$Path){
 $full=[IO.Path]::GetFullPath($Path);$part=$full
 while(![string]::IsNullOrEmpty($part)){
  if(Test-Path -LiteralPath $part){$item=Get-Item -LiteralPath $part -Force;if(($item.Attributes -band [IO.FileAttributes]::ReparsePoint)-ne 0){throw ('Reparse path is not supported: '+$part)}}
  $parent=[IO.Path]::GetDirectoryName($part);if($parent -eq $part){break};$part=$parent
 }
}
function Write-NewJson([string]$Path,$Value){
 if(!(Under-Path $Path $evidence)){throw 'Evidence write escaped this GUID directory.'}
 $bytes=$utf8.GetBytes((ConvertTo-Json -InputObject $Value -Depth 100)+[Environment]::NewLine)
 $stream=New-Object IO.FileStream($Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
 try{$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)}finally{$stream.Dispose()}
}
function File-Row([string]$Path,[string]$Name){
 Assert-PlainPath $Path
 $before=Get-Item -LiteralPath $Path -Force
 if($before.PSIsContainer){throw ('Expected file: '+$Path)}
 $sha=New-Object Security.Cryptography.SHA256Managed
 $stream=New-Object IO.FileStream($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 try{$digest=[BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','').ToLowerInvariant()}finally{$stream.Dispose();$sha.Dispose()}
 $after=Get-Item -LiteralPath $Path -Force
 if($before.Length -ne $after.Length -or $before.LastWriteTimeUtc -ne $after.LastWriteTimeUtc){throw ('File changed while hashing: '+$Path)}
 [pscustomobject]@{path=$Name;bytes=$after.Length;sha256=$digest;lastWriteUtc=$after.LastWriteTimeUtc.ToString('o')}
}
function File-Receipt([string]$Path){$full=[IO.Path]::GetFullPath($Path);File-Row $full $full}
function Get-Inventory([string[]]$Roots,[string]$RelativeTo){
 $rows=New-Object 'System.Collections.Generic.List[object]'
 foreach($root in $Roots){
  Assert-PlainPath $root
  if(!(Test-Path -LiteralPath $root -PathType Container)){throw ('Missing inventory root: '+$root)}
  $pending=New-Object 'System.Collections.Generic.Stack[string]';$pending.Push($root)
  while($pending.Count -gt 0){
   foreach($item in @(Get-ChildItem -LiteralPath ($pending.Pop()) -Force)){
    if(($item.Attributes -band [IO.FileAttributes]::ReparsePoint)-ne 0){throw ('Reparse input is not supported: '+$item.FullName)}
    if($item.PSIsContainer){$pending.Push($item.FullName)}else{
     if(!(Under-Path $item.FullName $RelativeTo)){throw 'Inventory file escaped root.'}
     $rows.Add([pscustomobject]@{full=$item.FullName;path=$item.FullName.Substring($RelativeTo.TrimEnd('\','/').Length+1).Replace('\','/');bytes=$item.Length;lastWriteUtc=$item.LastWriteTimeUtc.ToString('o')})
    }
   }
  }
 }
 @($rows.ToArray()|Sort-Object path)
}
function Capture-Tree([string[]]$Roots,[string]$RelativeTo){
 $began=[DateTime]::UtcNow;$inventory=@(Get-Inventory $Roots $RelativeTo)
 $rows=@(foreach($item in $inventory){File-Row $item.full $item.path})
 $ending=@(Get-Inventory $Roots $RelativeTo)
 $expected=@($rows|ForEach-Object {$_.path+':'+$_.bytes+':'+$_.lastWriteUtc})
 $actual=@($ending|ForEach-Object {$_.path+':'+$_.bytes+':'+$_.lastWriteUtc})
 if(($expected -join "`n") -cne ($actual -join "`n")){throw 'Inventory changed during capture; retry after source/build activity stops.'}
 $material=($rows|ForEach-Object {$_.path+':'+$_.bytes+':'+$_.sha256}) -join "`n"
 $sha=New-Object Security.Cryptography.SHA256Managed
 try{$digest=[BitConverter]::ToString($sha.ComputeHash($utf8.GetBytes($material))).Replace('-','').ToLowerInvariant()}finally{$sha.Dispose()}
 [pscustomobject]@{startedUtc=$began.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o');root=$RelativeTo;roots=$Roots;count=$rows.Count;aggregateSHA256=$digest;files=$rows}
}
function Compare-Inputs($Old,$New){
 $left=@{};foreach($row in $Old.files){if($left.ContainsKey($row.path)){throw 'Duplicate path in input manifest.'};$left[$row.path]=$row}
 $right=@{};foreach($row in $New.files){if($right.ContainsKey($row.path)){throw 'Duplicate path in current inventory.'};$right[$row.path]=$row}
 $added=@(foreach($row in $New.files){if(!$left.ContainsKey($row.path)){$row}})
 $removed=@(foreach($row in $Old.files){if(!$right.ContainsKey($row.path)){$row}})
 $changed=@(foreach($row in $New.files){if($left.ContainsKey($row.path)){$prior=$left[$row.path];if($row.bytes -ne $prior.bytes -or $row.sha256 -ine $prior.sha256){[pscustomobject]@{path=$row.path;before=$prior;after=$row}}}})
 [pscustomobject]@{unchanged=($added.Count -eq 0 -and $removed.Count -eq 0 -and $changed.Count -eq 0);added=$added;removed=$removed;changed=$changed}
}
function Assert-InputCoverage($Inputs){
 $required=@('Assets/TalesTactics/Scenes/TestBattle.unity','Packages/manifest.json','Packages/packages-lock.json','ProjectSettings/ProjectVersion.txt','ProjectSettings/ProjectSettings.asset')
 foreach($path in $required){if(@($Inputs.files|Where-Object {$_.path -ceq $path}).Count -ne 1){throw ('Missing required input: '+$path)}}
 foreach($prefix in @('Assets/TalesTactics/Content/','Assets/TalesTactics/Resources/','Assets/TalesTactics/Runtime/')){if(@($Inputs.files|Where-Object {$_.path.StartsWith($prefix,[StringComparison]::Ordinal)}).Count -eq 0){throw ('Empty required input category: '+$prefix)}}
}
function Git-Read([string[]]$Arguments){$value=@(& git -C $project @Arguments);if($LASTEXITCODE -ne 0){throw ('Git read failed: '+($Arguments -join ' '))};$value -join "`n"}
function Require([bool]$Condition,[string]$Message){if(!$Condition){$failures.Add($Message)}}
function Utc([string]$Value){[DateTimeOffset]::Parse($Value,[Globalization.CultureInfo]::InvariantCulture,[Globalization.DateTimeStyles]::AssumeUniversal).UtcDateTime}

Assert-PlainPath $evidence
if(Test-Path -LiteralPath $evidence){throw 'GUID evidence path already exists.'}
[IO.Directory]::CreateDirectory($evidence)|Out-Null
$record=[ordered]@{schema=1;phase=$Phase;kind=$Kind;runId=$runId;startedUtc=$started.ToString('o');finishedUtc=$null;passed=$false;sourceLabel=$SourceLabel;project=$project;expectedBuild=$expectedBuild;evidence=$evidence;failures=@();scope='Local provenance only. Full project inputs include tracked, untracked and ignored files. Equality at capture endpoints does not prove no transient edits or deterministic compilation; BuildReport and output hashes provide additional linkage. This is not a test, playthrough or dependency-inclusion verdict. Local-only audio and build artifacts must not be uploaded.'}
$recordPath=Join-Path $evidence $(if($Phase -eq 'Before'){'before-source.json'}else{'after-binary-source.json'})
try{
 $record.recorder=File-Receipt $PSCommandPath;$record.buildHelper=File-Receipt $helper
 $record.sourceCommitBase=Git-Read @('rev-parse','HEAD');$record.branch=Git-Read @('branch','--show-current')
 $record.sourceWorkingTree=Git-Read @('status','--porcelain=v1','--untracked-files=all','--','Assets','Packages','ProjectSettings')
 $record.unityVersionText=[IO.File]::ReadAllText((Join-Path $project 'ProjectSettings/ProjectVersion.txt'))
 $record.exclusions=@('Library (including imported artifacts and PackageCache)','Installed Unity/toolchain binaries','Temp','Builds as source inputs','Docs/Logs except explicitly pinned recorder, helper and optional test receipt')
 $record.testSourceManifest=$(if($TestSourceManifestPath){File-Receipt $TestSourceManifestPath}else{$null})
 $before=$null;$build=$null
 if($Phase -eq 'Before'){
  if($BeforeManifestPath -or $BuildResultPath){throw 'Before accepts no prior build manifest/result.'}
  Assert-PlainPath $expectedBuild
  if(Test-Path -LiteralPath $expectedBuild){throw 'Before requires the exact build output to be absent; no retrospective baseline is accepted.'}
 }else{
  if(!$BeforeManifestPath -or !$BuildResultPath){throw 'After requires BeforeManifestPath and BuildResultPath.'}
  $record.beforeSource=File-Receipt $BeforeManifestPath
  $before=Get-Content -LiteralPath $BeforeManifestPath -Raw|ConvertFrom-Json
  if($before.schema -ne 1 -or $before.phase -cne 'Before' -or $before.passed -ne $true -or $before.kind -ine $Kind -or !(Same-Path $before.project $project) -or !(Same-Path $before.expectedBuild $expectedBuild)){throw 'Before receipt does not match this project, build kind or successful capture.'}
  if($before.sourceLabel -cne $SourceLabel){throw 'Use the same source label for this Before/After pair.'}
  $record.buildReceipt=File-Receipt $BuildResultPath;$build=Get-Content -LiteralPath $BuildResultPath -Raw|ConvertFrom-Json
  if(!(Under-Path $BuildResultPath (Join-Path $project 'Logs/AllStageTactics20261118')) -and !(Same-Path $BuildResultPath (Join-Path $expectedBuild 'build-result.json'))){throw 'Build receipt must come from this task evidence or the exact build output.'}
  Require ($build.complete -eq $true -and $build.passed -eq $true -and $build.status -ceq 'Succeeded' -and $build.summaryResult -ceq 'Succeeded' -and $build.totalErrors -eq 0 -and $build.protectedFilesUnchanged -eq $true) 'BuildReport is not a completed successful build with protected inputs unchanged.'
  Require ((Same-Path $build.output $expectedBuild) -and (Same-Path $build.summaryOutputPath (Join-Path $expectedBuild 'TalesTactics.exe'))) 'Build output does not match the exact requested normal/review directory.'
  Require ($build.review -eq ($Kind -eq 'Review') -and $build.target -ceq 'StandaloneWindows64' -and $build.options -ceq 'None' -and $build.summaryOptions -ceq 'None') 'Build kind, platform or options mismatch.'
  $expectedDefines=$(if($Kind -eq 'Review'){'TALESTACTICS_REVIEW'}else{''})
  Require ((@($build.extraScriptingDefines) -join ';') -ceq $expectedDefines) 'Unexpected per-build extra scripting defines.'
  Require (@($build.projectDefines -split '[;,]'|Where-Object {$_.Trim() -in @('TALESTACTICS_REVIEW','DEBUG','DEVELOPMENT_BUILD')}).Count -eq 0) 'Project defines contain a review/development/debug symbol.'
  Require ((Same-Path $build.script $helper) -and $build.scriptSHA256 -ieq $before.buildHelper.sha256 -and $record.buildHelper.sha256 -ieq $before.buildHelper.sha256) 'Build helper bytes do not match the before receipt.'
  Require ($record.recorder.sha256 -ieq $before.recorder.sha256) 'Capture script changed between phases.'
  Require ((Utc $before.finishedUtc) -le (Utc $build.startedUtc) -and (Utc $build.startedUtc) -le (Utc $build.buildStartedAt) -and (Utc $build.buildStartedAt) -le (Utc $build.buildEndedAt) -and (Utc $build.buildEndedAt) -le (Utc $build.finishedUtc) -and (Utc $build.finishedUtc) -le $started) 'Capture/build timestamps are not ordered Before -> build -> After.'
  Require ($build.scene -ceq 'Assets/TalesTactics/Scenes/TestBattle.unity') 'Unexpected build scene.'
  Require ($record.unityVersionText.Contains('m_EditorVersion: '+$build.unityVersion)) 'Build Unity version differs from project version.'
  Require (-not [string]::IsNullOrEmpty($build.buildGuid)) 'BuildReport has no build GUID.'
  $copy=File-Receipt (Join-Path $expectedBuild 'build-result.json')
  Require ($copy.sha256 -ieq $record.buildReceipt.sha256) 'Output build receipt differs from the supplied final BuildReport receipt.'
  $record.outputBuildReceipt=$copy
 }
 $inputs=Capture-Tree @((Join-Path $project 'Assets'),(Join-Path $project 'Packages'),(Join-Path $project 'ProjectSettings')) $project
 Assert-InputCoverage $inputs
 $inputPath=Join-Path $evidence ('inputs-'+$Phase.ToLowerInvariant()+'.json');Write-NewJson $inputPath $inputs;$record.inputManifest=File-Receipt $inputPath
 $record.inputCount=$inputs.count;$record.inputAggregateSHA256=$inputs.aggregateSHA256
 if($Phase -eq 'After'){
  $oldPath=$before.inputManifest.path
  if(!(Under-Path $oldPath $before.evidence)){throw 'Before input manifest is outside its GUID evidence directory.'}
  $oldReceipt=File-Receipt $oldPath
  if($oldReceipt.sha256 -ine $before.inputManifest.sha256){throw 'Before input manifest digest changed.'}
  $oldInputs=Get-Content -LiteralPath $oldPath -Raw|ConvertFrom-Json;Assert-InputCoverage $oldInputs
  $comparison=Compare-Inputs $oldInputs $inputs
  $comparisonPath=Join-Path $evidence 'input-comparison.json';Write-NewJson $comparisonPath $comparison;$record.postBuildInputs=File-Receipt $comparisonPath
  Require $comparison.unchanged 'Source inputs were added, deleted or changed between captures.'
  Require ($record.sourceCommitBase -ceq $before.sourceCommitBase -and $record.branch -ceq $before.branch) 'Source commit/branch changed between captures.'
  $inputLookup=@{};foreach($row in $oldInputs.files){$inputLookup[$row.path]=$row}
  $protectedDisagreements=@(foreach($row in @($build.protectedBefore)+@($build.protectedAfter)){$match=$inputLookup[$row.path];if($null -eq $match -or $match.sha256 -ine $row.sha256 -or $match.bytes -ne $row.bytes){$row}})
  $record.buildProtectedInputDisagreements=$protectedDisagreements
  Require ($protectedDisagreements.Count -eq 0 -and @($build.protectedBefore).Count -gt 0 -and @($build.protectedAfter).Count -gt 0) 'BuildReport protected-input hashes do not match the before source snapshot.'
  $outputs=Capture-Tree @($expectedBuild) $expectedBuild
  $outputPath=Join-Path $evidence 'output-files.json';Write-NewJson $outputPath $outputs;$record.outputManifest=File-Receipt $outputPath
  $record.outputCount=$outputs.count;$record.outputAggregateSHA256=$outputs.aggregateSHA256
  $primary=@(foreach($path in @('TalesTactics.exe','TalesTactics_Data/Managed/TalesTactics.Runtime.dll','UnityPlayer.dll')){$rows=@($outputs.files|Where-Object {$_.path -ceq $path});if($rows.Count -ne 1){throw ('Missing primary output: '+$path)};$rows[0]})
  $record.binaries=$primary;$record.buildGuid=$build.buildGuid;$record.unityVersion=$build.unityVersion
  $record.buildStartedAt=$build.buildStartedAt;$record.buildEndedAt=$build.buildEndedAt;$record.extraScriptingDefines=$build.extraScriptingDefines
  Require (@($outputs.files|Where-Object {$_.path -match '(^|/)(CampaignFlowReviews|Storage|Reviews)/'}).Count -eq 0) 'Output already contains player review/storage artifacts; capture immediately after build before running it.'
 }
 Require ((File-Receipt $helper).sha256 -ieq $record.buildHelper.sha256) 'Build helper changed during capture.'
 Require ((File-Receipt $PSCommandPath).sha256 -ieq $record.recorder.sha256) 'Capture script changed during capture.'
 $record.passed=$failures.Count -eq 0
}catch{$failures.Add($_.Exception.ToString());$record.passed=$false}
$record.finishedUtc=[DateTime]::UtcNow.ToString('o');$record.failures=@($failures.ToArray())
Write-NewJson $recordPath $record
[pscustomobject]@{passed=$record.passed;phase=$Phase;kind=$Kind;manifest=$recordPath;evidence=$evidence;failures=$record.failures}|ConvertTo-Json -Depth 5
if(!$record.passed){exit 1}