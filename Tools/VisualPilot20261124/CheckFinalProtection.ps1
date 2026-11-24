[CmdletBinding()]
param([ValidateSet('StrictBaseline','RecordedRegressionArtifacts')][string]$Mode='StrictBaseline')
# Final evidence only. The only write is one new mode-specific JSON under Logs.
# Does not restore/delete/copy project files, write user data, run Unity, alter Git,
# install/run players, upload artifacts or change any existing evidence.
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$repo=[IO.Path]::GetFullPath('C:/dev/TalesTactics').TrimEnd('\','/')
$public=Join-Path $repo 'Builds/SourceOnlyPublish20261118/Project'
$evidence=Join-Path $repo 'Logs/VisualPilot20261124'
$baseline=Join-Path $evidence 'Baseline-c21b08a8b25048449368f587e4eb51c3'
$output=Join-Path $evidence $(if($Mode -eq 'StrictBaseline'){'final-protection.json'}else{'final-protection-after-tests.json'})
$manifestPath=Join-Path $evidence 'final-source-manifest.json'
$utf8=New-Object Text.UTF8Encoding($false)
$failures=New-Object 'System.Collections.Generic.List[string]'
$checks=New-Object 'System.Collections.Generic.List[object]'
$approvedDocumentation=@('README.md','.gitattributes','Docs/ARCHITECTURE.md','Docs/VALIDATION.md','Docs/TODO.md','Docs/VisualPilot20261124/**')
$report=[ordered]@{
 schema=2;mode=$Mode;passed=$false;startedUtc=[DateTime]::UtcNow.ToString('o');finishedUtc=$null
 baseline=$baseline;output=$output;writesToUserTree=$false;writesToProjectFiles=$false;gitMutation=$false;networkUsed=$false
 scope='Exact-byte preservation of the fresh c21 baseline, including actual saves, recursive user file/directory inventory, preexisting APKs, all protected files, original tracked modifications, public settings/font, and frozen approved product hashes in both checkouts. Existing root Player.log and Player-prev.log are excluded exactly as in the baseline. No rendering, build, gameplay or device validation is performed.'
 allowedChanges=[ordered]@{productManifest=$manifestPath;productCount=28;documentation=$approvedDocumentation;validationTools='Tools/VisualPilot20261124/**';notes='Approved product paths are verified against their final source manifest rather than their older baseline bytes. Other allowed documentation may differ between root and public until explicitly copied. Two layout/head-anchor JSON documents also occur in the frozen product manifest and therefore must retain those frozen hashes. Existing protected CSV, font, settings, metadata, supplied music and APKs remain exact-byte preservation targets.'}
 baselineIntegrity=@();buildCompletion=@();regressionException=$null;userTree=$null;actualUserSaves=@();existingApks=@();protectedFiles=@();originalModifications=$null;publicSettingsAndFont=@();frozenProduct=$null;currentGit=$null;checks=@();failures=@()
}
function Check([string]$name,[bool]$passed,$observed){$checks.Add([pscustomobject]@{name=$name;passed=$passed;observed=$observed});if(!$passed){$failures.Add($name)}}
function Within([string]$path,[string]$root){$fullPath=[IO.Path]::GetFullPath($path);$fullRoot=[IO.Path]::GetFullPath($root);return $fullPath.StartsWith($fullRoot.TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)}
function NoReparse([string]$path){$item=Get-Item -LiteralPath $path -Force;while($null -ne $item){if(($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw ('Reparse point: '+$item.FullName)};if($item -is [IO.FileInfo]){$item=$item.Directory}else{$item=$item.Parent}}}
function RelativePath([string]$root,[string]$relative){
 if([IO.Path]::IsPathRooted($relative)){throw ('Expected relative path: '+$relative)}
 $full=[IO.Path]::GetFullPath((Join-Path $root $relative));if(!(Within $full $root)){throw ('Path escaped scope: '+$relative)};return $full
}
function FileState([string]$path){
 $full=[IO.Path]::GetFullPath($path)
 if(!(Test-Path -LiteralPath $full -PathType Leaf)){return [pscustomobject]@{path=$full;exists=$false;bytes=$null;sha256=$null;lastWriteUtc=$null}}
 NoReparse $full;$before=Get-Item -LiteralPath $full -Force
 $stream=[IO.File]::Open($full,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
 $algorithm=[Security.Cryptography.SHA256]::Create()
 try{$digest=[BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-','').ToLowerInvariant()}finally{$algorithm.Dispose();$stream.Dispose()}
 $after=Get-Item -LiteralPath $full -Force
 if($before.Length -ne $after.Length -or $before.LastWriteTimeUtc -ne $after.LastWriteTimeUtc){throw ('File changed during hashing: '+$full)}
 return [pscustomobject]@{path=$full;exists=$true;bytes=$after.Length;sha256=$digest;lastWriteUtc=$after.LastWriteTimeUtc.ToString('o')}
}
function Hash([string]$path){$state=FileState $path;if(!$state.exists){throw ('Missing file: '+$path)};return $state.sha256}
function ReadJson([string]$path){return [IO.File]::ReadAllText($path)|ConvertFrom-Json}
function Inventory([string]$root){
 $files=New-Object 'System.Collections.Generic.List[object]';$dirs=New-Object 'System.Collections.Generic.List[string]';$excluded=New-Object 'System.Collections.Generic.List[object]'
 $exists=Test-Path -LiteralPath $root -PathType Container
 if($exists){NoReparse $root;$stack=New-Object 'System.Collections.Generic.Stack[string]';$stack.Push($root)
 while($stack.Count -gt 0){foreach($item in @(Get-ChildItem -LiteralPath $stack.Pop() -Force)){
  $rel=$item.FullName.Substring($root.TrimEnd('\','/').Length+1).Replace('\','/')
  if(($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw ('Reparse point in user tree: '+$rel)}
  if($item.PSIsContainer){$dirs.Add($rel);$stack.Push($item.FullName);continue}
  $row=[pscustomobject]@{name=$rel;bytes=$item.Length;lastWriteUtc=$item.LastWriteTimeUtc.ToString('o')}
  if(@('Player.log','Player-prev.log') -icontains $rel){$excluded.Add($row)}else{$files.Add($row)}
 }}}
 return [pscustomobject]@{exists=$exists;files=@($files.ToArray()|Sort-Object name);directories=@($dirs.ToArray()|Sort-Object);excludedFiles=@($excluded.ToArray()|Sort-Object name)}
}
function TrackedPaths($state){
 return @(@($state.modified)+@($state.status|Where-Object {!$_.StartsWith('?? ')}|ForEach-Object {
  if($_ -match ' -> ' -or $_.Substring(3).StartsWith('"')){throw 'Unexpected quoted/renamed baseline Git path; inspect before proceeding.'};$_.Substring(3)
 })|Sort-Object -Unique)
}
function Documentation([string]$relative){return ($approvedDocumentation -icontains $relative) -or $relative.StartsWith('Docs/VisualPilot20261124/',[StringComparison]::OrdinalIgnoreCase)}
function GitState([string]$root){
 $lines=@(& git.exe --no-optional-locks -C $root -c core.quotePath=false status --porcelain=v1 --untracked-files=all)
 if($LASTEXITCODE -ne 0){throw 'Read-only Git status failed.'}
 $head=@(& git.exe --no-optional-locks -C $root rev-parse HEAD);if($LASTEXITCODE -ne 0){throw 'Read-only Git HEAD failed.'}
 $branch=@(& git.exe --no-optional-locks -C $root branch --show-current);if($LASTEXITCODE -ne 0){throw 'Read-only Git branch failed.'}
 return [pscustomobject]@{root=$root;head=($head -join '').Trim();branch=($branch -join '').Trim();status=$lines}
}
if(Test-Path -LiteralPath $output){throw 'Refusing to overwrite final protection evidence.'}
NoReparse $evidence
try{
 $baseInfo=ReadJson (Join-Path $baseline 'baseline.json')
 Check 'Successful exact c21 baseline selected' ($baseInfo.passed -eq $true -and $baseInfo.protectedCount -eq 3289 -and $baseInfo.userFileCount -eq 5532) $baseInfo.finishedUtc
 $integrity=@(foreach($entry in $baseInfo.outputs){$path=RelativePath $baseline $entry.name;$sha=Hash $path;[pscustomobject]@{name=$entry.name;expectedSha256=$entry.sha256;sha256=$sha;match=($sha -ieq $entry.sha256)}})
 $report.baselineIntegrity=$integrity
 Check 'Immutable baseline JSON files match original recorded SHA-256' (@($integrity|Where-Object {!$_.match}).Count -eq 0) @($integrity|Where-Object {!$_.match})
 if(@($integrity|Where-Object {!$_.match}).Count -gt 0){throw 'Baseline evidence changed; comparisons would be unreliable.'}
 $protected=@(ReadJson (Join-Path $baseline 'protected-current.json'))
 $user=ReadJson (Join-Path $baseline 'user-tree.json')
 $saves=@(ReadJson (Join-Path $baseline 'actual-user-save-copies.json'))
 $apks=@(ReadJson (Join-Path $baseline 'existing-apks.json'))
 $rootBefore=ReadJson (Join-Path $baseline 'git-root-before.json')
 $publicBefore=ReadJson (Join-Path $baseline 'git-validation-before.json')
 $manifestSha=Hash $manifestPath;$manifest=ReadJson $manifestPath
 Check 'Frozen product manifest contains 28 approved unique paths' ($manifest.passed -eq $true -and $manifest.count -eq 28 -and @($manifest.files).Count -eq 28 -and @($manifest.files.relative|Sort-Object -Unique).Count -eq 28) $manifest.count
 $product=@{};foreach($entry in $manifest.files){$product[$entry.relative]=$entry}
 $finished=@(foreach($platform in @('windows','android')){
  $buildPath=Join-Path $evidence ('Builds/'+$platform+'-build-final.json');$build=ReadJson $buildPath
  $passed=$build.passed -eq $true -and $build.settingsEqual -eq $true -and $build.result -eq 'Succeeded' -and $build.errors -eq 0
  [pscustomobject]@{platform=$platform;path=$buildPath;sha256=(Hash $buildPath);passed=$passed;finishedUtc=$build.finishedUtc}
 })
 $report.buildCompletion=$finished
 Check 'Both builds completed successfully with restored settings before final preservation check' (@($finished|Where-Object {!$_.passed}).Count -eq 0) $finished
 if(@($finished|Where-Object {!$_.passed}).Count -gt 0){throw 'Wait for both builds before final preservation inspection.'}
 $expectedStorage=[IO.Path]::GetFullPath((Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData/LocalLow/LocalTactics/TalesTactics'))
 if([IO.Path]::GetFullPath($user.root) -ine $expectedStorage){throw 'Unexpected actual user storage root in baseline.'}
 Check 'User tree baseline excludes only the exact two root player logs' ((@($user.excludedRootFileNames|Sort-Object) -join '|') -ceq 'Player-prev.log|Player.log') $user.excludedRootFileNames
 $first=Inventory $expectedStorage
 $expectedUser=@{};foreach($entry in $user.files){$expectedUser[$entry.name]=$entry}
 $userRows=@(foreach($entry in $first.files){
  $state=FileState (RelativePath $expectedStorage $entry.name)
  if($state.bytes -ne $entry.bytes -or $state.lastWriteUtc -cne $entry.lastWriteUtc){throw ('User file changed after enumeration: '+$entry.name)}
  $old=$expectedUser[$entry.name];$known=$null -ne $old
  [pscustomobject]@{name=$entry.name;bytes=$state.bytes;sha256=$state.sha256;lastWriteUtc=$state.lastWriteUtc;baselineKnown=$known;expectedBytes=$(if($known){$old.bytes}else{$null});expectedSha256=$(if($known){$old.sha256}else{$null});bytesMatch=($known -and $state.bytes -eq $old.bytes -and $state.sha256 -ieq $old.sha256);timestampMatch=($known -and $state.lastWriteUtc -ceq $old.lastWriteUtc)}
 })
 $second=Inventory $expectedStorage
 $raceStable=$first.exists -eq $second.exists -and (($first.files|ConvertTo-Json -Depth 4) -ceq ($second.files|ConvertTo-Json -Depth 4)) -and (($first.directories -join "`n") -ceq ($second.directories -join "`n"))
 $currentNames=@{};foreach($entry in $userRows){$currentNames[$entry.name]=$true}
 $missing=@($user.files|Where-Object {!$currentNames.ContainsKey($_.name)}|ForEach-Object name)
 $added=@($userRows|Where-Object {!$_.baselineKnown}|ForEach-Object name)
 $changed=@($userRows|Where-Object {$_.baselineKnown -and !$_.bytesMatch}|ForEach-Object name)
 $newDirectories=@($second.directories|Where-Object {$user.directories -inotcontains $_})
 $missingDirectories=@($user.directories|Where-Object {$second.directories -inotcontains $_})
 $report.userTree=[ordered]@{root=$expectedStorage;baselineFiles=@($user.files).Count;currentFiles=$userRows.Count;baselineExists=$user.exists;currentExists=$second.exists;files=$userRows;missingFiles=$missing;addedFiles=$added;changedFiles=$changed;newDirectories=$newDirectories;missingDirectories=$missingDirectories;raceChecked=$raceStable;excludedRootFileNames=$user.excludedRootFileNames;observedExcludedFiles=$second.excludedFiles}
 $report.userTree['completelyUnchangedFromBaseline']=($missing.Count -eq 0 -and $added.Count -eq 0 -and $changed.Count -eq 0 -and $newDirectories.Count -eq 0 -and $missingDirectories.Count -eq 0 -and $user.exists -eq $second.exists)
 if($Mode -eq 'StrictBaseline'){
  Check 'Actual user tree 5532 file names and bytes preserved' (@($user.files).Count -eq 5532 -and $userRows.Count -eq 5532 -and $missing.Count -eq 0 -and $added.Count -eq 0 -and $changed.Count -eq 0 -and $user.exists -eq $second.exists -and $raceStable) ([pscustomobject]@{missing=$missing;added=$added;changed=$changed;raceStable=$raceStable})
  Check 'Actual user directory inventory preserved' ($newDirectories.Count -eq 0 -and $missingDirectories.Count -eq 0) ([pscustomobject]@{added=$newDirectories;missing=$missingDirectories})
 }else{
  # Explicitly authorized diagnostic exception after the strict failure was recorded.
  # This does NOT declare the whole user storage unchanged, restore the old XML,
  # delete test artifacts, or permit any new/unrecorded byte or path difference.
  $strictPath=Join-Path $evidence 'final-protection.json'
  $strictExpectedSha='afb12573633f1b783939cf9b473b14bac6ff0e5583bc2fc6aae92041f7b60730'
  $strictSha=Hash $strictPath
  if($strictSha -cne $strictExpectedSha){throw 'Original strict failure evidence changed; diagnostic exceptions cannot be derived.'}
  $strict=ReadJson $strictPath
  if($strict.passed -ne $false -or $strict.userTree.root -ine $expectedStorage){throw 'Expected original strict failure for this user storage.'}
  $strictAdded=@($strict.userTree.addedFiles);$strictDirs=@($strict.userTree.newDirectories)
  $strictChanged=@($strict.userTree.changedFiles)
  $strictShape=$strictAdded.Count -eq 65 -and @($strictAdded|Sort-Object -Unique).Count -eq 65 -and $strictDirs.Count -eq 35 -and @($strictDirs|Sort-Object -Unique).Count -eq 35 -and $strictChanged.Count -eq 1 -and $strictChanged[0] -ceq 'TestResults.xml' -and @($strict.userTree.missingFiles).Count -eq 0 -and @($strict.userTree.missingDirectories).Count -eq 0
  $invalidAdded=@($strictAdded|Where-Object {$_ -cnotmatch '^StorageTests/[0-9a-f]{32}/campaign\.json(?:\.(?:bak|v1\.bak|v2\.bak|corrupt-[0-9a-f]{32}))?$'})
  $invalidDirs=@($strictDirs|Where-Object {$_ -cnotmatch '^StorageTests/[0-9a-f]{32}$'})
  $unknownParent=@($strictAdded|Where-Object {$strictDirs -cnotcontains $_.Substring(0,$_.LastIndexOf('/'))})
  Check 'Recorded diagnostic exception is exactly 65 allowed StorageTests files, 35 GUID folders and TestResults.xml' ($strictShape -and $invalidAdded.Count -eq 0 -and $invalidDirs.Count -eq 0 -and $unknownParent.Count -eq 0) ([pscustomobject]@{invalidFiles=$invalidAdded;invalidDirectories=$invalidDirs;unknownParent=$unknownParent})
  if(!$strictShape -or $invalidAdded.Count -gt 0 -or $invalidDirs.Count -gt 0 -or $unknownParent.Count -gt 0){throw 'Original exception paths are outside their explicit approved diagnostic scope.'}
  $exactExceptionSet=($added.Count -eq 65 -and $newDirectories.Count -eq 35 -and $changed.Count -eq 1 -and $changed[0] -ceq 'TestResults.xml' -and (($added|Sort-Object) -join "`n") -ceq (($strictAdded|Sort-Object) -join "`n") -and (($newDirectories|Sort-Object) -join "`n") -ceq (($strictDirs|Sort-Object) -join "`n") -and $missing.Count -eq 0 -and $missingDirectories.Count -eq 0)
  Check 'Current differences are exactly the recorded diagnostic exception set, with no missing files or directories' $exactExceptionSet ([pscustomobject]@{added=$added;changed=$changed;directories=$newDirectories;missing=$missing;missingDirectories=$missingDirectories})
  $strictFiles=@{};foreach($entry in $strict.userTree.files){$strictFiles[$entry.name]=$entry}
  $currentUser=@{};foreach($entry in $userRows){$currentUser[$entry.name]=$entry}
  $exceptionRows=@(foreach($name in @($strictAdded)+@('TestResults.xml')){
   $previous=$strictFiles[$name];$now=$currentUser[$name]
   if($null -eq $previous -or $null -eq $now){throw ('Recorded diagnostic file missing: '+$name)}
   [pscustomobject]@{name=$name;recordedBytes=$previous.bytes;recordedSha256=$previous.sha256;currentBytes=$now.bytes;currentSha256=$now.sha256;matchesRecordedBytes=($previous.bytes -eq $now.bytes -and $previous.sha256 -ieq $now.sha256)}
  })
  Check 'All 66 retained diagnostic files still match the first strict report hashes and sizes' ($exceptionRows.Count -eq 66 -and @($exceptionRows|Where-Object {!$_.matchesRecordedBytes}).Count -eq 0) @($exceptionRows|Where-Object {!$_.matchesRecordedBytes})
  $unchangedOriginal=@($userRows|Where-Object {$_.baselineKnown -and $_.name -cne 'TestResults.xml'})
  Check 'Remaining 5531 baseline user files retain exact bytes; actual saves are checked separately' (@($user.files).Count -eq 5532 -and $userRows.Count -eq 5597 -and $unchangedOriginal.Count -eq 5531 -and @($unchangedOriginal|Where-Object {!$_.bytesMatch}).Count -eq 0 -and $user.exists -eq $second.exists -and $raceStable) @($unchangedOriginal|Where-Object {!$_.bytesMatch})
  $playPath=Join-Path $evidence 'full-play-result.json'
  $playExpectedSha='893ec2d1291944631e40a77006a6ce12ac1fcf29dafa45849ad95c3389c3895c'
  $playSha=Hash $playPath;if($playSha -cne $playExpectedSha){throw 'Original full PlayMode result changed.'}
  $play=ReadJson $playPath
  $xmlPath=RelativePath $expectedStorage 'TestResults.xml'
  $xmlSettings=New-Object System.Xml.XmlReaderSettings
  $xmlSettings.DtdProcessing=[System.Xml.DtdProcessing]::Prohibit;$xmlSettings.XmlResolver=$null
  $xmlReader=[System.Xml.XmlReader]::Create($xmlPath,$xmlSettings)
  $xml=New-Object System.Xml.XmlDocument;$xml.XmlResolver=$null
  try{$xml.Load($xmlReader)}finally{$xmlReader.Dispose()}
  $run=$xml.DocumentElement;$cases=@($xml.SelectNodes('//test-case'))
  $xmlSummary=[pscustomobject]@{root=$run.Name;total=$run.GetAttribute('total');testcasecount=$run.GetAttribute('testcasecount');passed=$run.GetAttribute('passed');failed=$run.GetAttribute('failed');skipped=$run.GetAttribute('skipped');inconclusive=$run.GetAttribute('inconclusive');result=$run.GetAttribute('result');startUtc=$run.GetAttribute('start-time');endUtc=$run.GetAttribute('end-time');duration=$run.GetAttribute('duration');leafCaseCount=$cases.Count}
  $xmlPassed=$xmlSummary.root -ceq 'test-run' -and $xmlSummary.total -ceq '321' -and $xmlSummary.testcasecount -ceq '321' -and $xmlSummary.passed -ceq '321' -and $xmlSummary.failed -ceq '0' -and $xmlSummary.skipped -ceq '0' -and $xmlSummary.inconclusive -ceq '0' -and $xmlSummary.result -ceq 'Passed' -and $xmlSummary.startUtc -ceq '2026-11-24 13:09:28Z' -and $xmlSummary.endUtc -ceq '2026-11-24 13:22:09Z' -and $cases.Count -eq 321 -and @($cases|Where-Object {$_.GetAttribute('result') -cne 'Passed'}).Count -eq 0
  $playPassed=$play.status -ceq 'completed' -and $play.summary.total -eq 321 -and $play.summary.passed -eq 321 -and $play.summary.failed -eq 0 -and $play.summary.skipped -eq 0 -and $play.summary.inconclusive -eq 0 -and @($play.results).Count -eq 321 -and @($play.results|Where-Object {$_.Status -cne 'Passed'}).Count -eq 0
  $xmlNames=@($cases|ForEach-Object {$_.GetAttribute('fullname')}|Sort-Object)
  $playNames=@($play.results|ForEach-Object FullName|Sort-Object)
  $matchingCases=(($xmlNames -join "`n") -ceq ($playNames -join "`n")) -and @($xmlNames|Sort-Object -Unique).Count -eq 321
  $durationDelta=[Math]::Abs([double]::Parse($xmlSummary.duration,[Globalization.CultureInfo]::InvariantCulture)-[double]$play.duration)
  Check 'Retained XML is the matching 321/321 passed current PlayMode result at the recorded UTC interval' ($xmlPassed -and $playPassed -and $matchingCases -and $durationDelta -lt 0.05) ([pscustomobject]@{xml=$xmlSummary;pipelineSummary=$play.summary;matchingTestNames=$matchingCases;durationDeltaSeconds=$durationDelta})
  $report.regressionException=[ordered]@{explicitMode=$Mode;originalStrictReport=$strictPath;originalStrictReportSHA256=$strictSha;originalStrictPassed=$strict.passed;retainedFiles=$exceptionRows;retainedDirectories=$strictDirs;unchangedBaselineFileCount=$unchangedOriginal.Count;actualUserStorageCompletelyUnchanged=$false;oldTestResultsRestored=$false;fullPlayResult=$playPath;fullPlayResultSHA256=$playSha;xmlSummary=$xmlSummary;matchingTestNames=$matchingCases;scope='Only the exact first-observed 65 StorageTests files/35 folders and replacement TestResults.xml are retained as explicitly approved regression diagnostics. The original XML was hash-only in the baseline and has not been restored. All other 5531 original files, actual campaign/settings saves, existing APKs and protected project bytes remain strict preservation targets.'}
  $report.scope+=' RecordedRegressionArtifacts mode explicitly reports retained regression diagnostics; it does not claim the entire user storage is unchanged.'
 }
 $report.actualUserSaves=@(foreach($entry in $saves){if(!(Within ([IO.Path]::GetFullPath($entry.path)) $expectedStorage)){throw 'Save path escaped user tree.'};$state=FileState $entry.path;[pscustomobject]@{path=$entry.path;expectedBytes=$entry.bytes;expectedSha256=$entry.sha256;current=$state;match=($state.exists -and $state.bytes -eq $entry.bytes -and $state.sha256 -ieq $entry.sha256)}})
 Check 'Actual campaign/settings saves 9 exact bytes preserved' ($saves.Count -eq 9 -and @($report.actualUserSaves|Where-Object {!$_.match}).Count -eq 0) @($report.actualUserSaves|Where-Object {!$_.match})
 $report.existingApks=@(foreach($entry in $apks){if(!(Within ([IO.Path]::GetFullPath($entry.path)) $repo)){throw 'APK path escaped project.'};$state=FileState $entry.path;[pscustomobject]@{path=$entry.path;expectedBytes=$entry.bytes;expectedSha256=$entry.sha256;current=$state;match=($state.exists -and $state.bytes -eq $entry.bytes -and $state.sha256 -ieq $entry.sha256)}})
 Check 'All 3 existing APKs preserved exactly' ($apks.Count -eq 3 -and @($report.existingApks|Where-Object {!$_.match}).Count -eq 0) @($report.existingApks|Where-Object {!$_.match})
 $protectedRows=@(foreach($entry in $protected){
  $checkout=if($entry.checkout -eq 'root'){$repo}elseif($entry.checkout -eq 'validation'){$public}else{throw 'Unexpected checkout in protected list.'}
  $path=RelativePath $checkout $entry.relative;if([IO.Path]::GetFullPath($entry.path) -ine $path){throw 'Protected path differs from checkout-relative path.'}
  $state=FileState $path;$approvedProduct=$product.ContainsKey($entry.relative);$approvedDoc=Documentation $entry.relative
  $policy=if($approvedProduct){'approved-product-final-manifest'}elseif($approvedDoc){'approved-documentation-change'}else{'exact-baseline-bytes'}
  $match=$state.exists -and $state.bytes -eq $entry.bytes -and $state.sha256 -ieq $entry.sha256
  [pscustomobject]@{checkout=$entry.checkout;relative=$entry.relative;path=$path;expectedBytes=$entry.bytes;expectedSha256=$entry.sha256;current=$state;baselineMatch=$match;policy=$policy;preservationRequired=(!$approvedProduct -and !$approvedDoc)}
 })
 $report.protectedFiles=$protectedRows
 $unexpectedProtected=@($protectedRows|Where-Object {$_.preservationRequired -and !$_.baselineMatch})
 Check 'All 3289 baseline protected entries checked; only approved product/document changes excepted' ($protectedRows.Count -eq 3289 -and $unexpectedProtected.Count -eq 0) $unexpectedProtected
 $rootTracked=TrackedPaths $rootBefore;$publicTracked=TrackedPaths $publicBefore
 $rootUser=@($rootTracked|Where-Object {!$product.ContainsKey($_)})
 $rootProductOverlap=@($rootTracked|Where-Object {$product.ContainsKey($_)})
 $rootOriginalRows=@($protectedRows|Where-Object {$_.checkout -eq 'root' -and $rootUser -icontains $_.relative})
 $publicOriginalRows=@($protectedRows|Where-Object {$_.checkout -eq 'validation' -and $publicTracked -icontains $_.relative})
 $report.originalModifications=[ordered]@{
  root=[ordered]@{baselineStatusSha256=(Hash (Join-Path $baseline 'git-root-before.json'));baselineTrackedCount=$rootTracked.Count;originalUserCount=$rootUser.Count;originalUserPaths=$rootUser;approvedProductOverlap=$rootProductOverlap;files=$rootOriginalRows}
  validation=[ordered]@{baselineStatusSha256=(Hash (Join-Path $baseline 'git-validation-before.json'));baselineTrackedCount=$publicTracked.Count;originalPaths=$publicTracked;files=$publicOriginalRows;countNote='Immutable baseline status contains 23 paths; its modified array contains 17 after Git content normalization. Earlier summary estimate 24 is not used as evidence.'}
 }
 Check 'ROOT original 12 user-modified paths preserved exactly' ($rootTracked.Count -eq 15 -and $rootUser.Count -eq 12 -and $rootOriginalRows.Count -eq 12 -and @($rootOriginalRows|Where-Object {!$_.baselineMatch}).Count -eq 0) @($rootOriginalRows|Where-Object {!$_.baselineMatch})
 Check 'PUBLIC original 23 status paths preserved exactly' ($publicTracked.Count -eq 23 -and $publicOriginalRows.Count -eq 23 -and @($publicOriginalRows|Where-Object {!$_.baselineMatch}).Count -eq 0) @($publicOriginalRows|Where-Object {!$_.baselineMatch})
 $settings=@($protectedRows|Where-Object {$_.checkout -eq 'validation' -and ($_.relative.StartsWith('ProjectSettings/') -or $_.relative -in @('Assets/TalesTactics/Resources/TalesTactics/KoreanFallback.asset','Assets/TalesTactics/Resources/TalesTactics/KoreanFallback.asset.meta'))})
 $report.publicSettingsAndFont=$settings
 Check 'PUBLIC project settings 27 plus font and metadata 2 preserve all 29 baseline bytes' ($settings.Count -eq 29 -and @($settings|Where-Object {!$_.baselineMatch}).Count -eq 0) @($settings|Where-Object {!$_.baselineMatch})
 $products=@(foreach($entry in $manifest.files){$a=FileState (RelativePath $repo $entry.relative);$b=FileState (RelativePath $public $entry.relative);[pscustomobject]@{relative=$entry.relative;expectedSha256=$entry.sha256;root=$a;validation=$b;rootMatches=($a.exists -and $a.sha256 -ieq $entry.sha256);validationMatches=($b.exists -and $b.sha256 -ieq $entry.sha256);rootAndPublicIdentical=($a.exists -and $b.exists -and $a.sha256 -ieq $b.sha256)}})
 $report.frozenProduct=[ordered]@{manifest=$manifestPath;manifestSHA256=$manifestSha;capturedUtc=$manifest.capturedUtc;files=$products;manifestUnchanged=((Hash $manifestPath) -ieq $manifestSha)}
 Check 'Frozen 28 product files remain identical to approved hashes in ROOT and PUBLIC' ($products.Count -eq 28 -and @($products|Where-Object {!$_.rootMatches -or !$_.validationMatches -or !$_.rootAndPublicIdentical}).Count -eq 0 -and $report.frozenProduct.manifestUnchanged) @($products|Where-Object {!$_.rootMatches -or !$_.validationMatches -or !$_.rootAndPublicIdentical})
 $gitRows=@(foreach($pair in @(@{name='root';path=$repo;before=$rootBefore},@{name='validation';path=$public;before=$publicBefore})){
  $state=GitState $pair.path;$oldPaths=@{};foreach($line in $pair.before.status){$oldPaths[$line.Substring(3)]=$true}
  $classified=@(foreach($line in $state.status){$rel=$line.Substring(3);$category=if($product.ContainsKey($rel)){'approved-product'}elseif(Documentation $rel){'approved-documentation'}elseif($rel.StartsWith('Tools/VisualPilot20261124/')){'owned-validation-tools'}elseif($oldPaths.ContainsKey($rel)){'preexisting-status-path'}else{'unexpected-new-status-path'};[pscustomobject]@{status=$line.Substring(0,2);relative=$rel;category=$category}})
  [pscustomobject]@{checkout=$pair.name;head=$state.head;branch=$state.branch;baselineHead=$pair.before.head;paths=$classified;unexpected=@($classified|Where-Object {$_.category -eq 'unexpected-new-status-path'});note='Git status is supplementary classification, not a replacement for byte checks. Baseline untracked files outside the protected set have path-only evidence.'}
 })
 $report.currentGit=$gitRows
 Check 'New Git-visible paths stay within approved product, documentation and validation-tool scope' (@($gitRows|ForEach-Object {$_.unexpected}).Count -eq 0) @($gitRows|ForEach-Object {$_.unexpected})
 $lastUser=Inventory $expectedStorage
 $stableUntilEnd=$second.exists -eq $lastUser.exists -and (($second.files|ConvertTo-Json -Depth 4) -ceq ($lastUser.files|ConvertTo-Json -Depth 4)) -and (($second.directories -join "`n") -ceq ($lastUser.directories -join "`n"))
 $report.userTree['stableUntilInspectionFinished']=$stableUntilEnd
 Check 'User tree inventory remained stable through the complete inspection' $stableUntilEnd $null
 if($Mode -eq 'RecordedRegressionArtifacts'){
  Check 'Original strict failure and full regression result evidence remain unchanged' ((Hash $strictPath) -ceq $strictSha -and (Hash $playPath) -ceq $playSha) $null
 }
}catch{$failures.Add($_.Exception.ToString())}
$report.finishedUtc=[DateTime]::UtcNow.ToString('o');$report.checks=@($checks.ToArray());$report.failures=@($failures.ToArray());$report.passed=$failures.Count -eq 0 -and $checks.Count -gt 0
# CreateNew preserves earlier evidence. No other file is opened for writing.
$stream=[IO.File]::Open($output,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
try{$bytes=$utf8.GetBytes(($report|ConvertTo-Json -Depth 32));$stream.Write($bytes,0,$bytes.Length)}finally{$stream.Dispose()}
[pscustomobject]@{passed=$report.passed;report=$output;checks=$checks.Count;failures=$report.failures}|ConvertTo-Json -Depth 8
if(!$report.passed){exit 1}