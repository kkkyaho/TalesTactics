[CmdletBinding()]
param([Parameter(Mandatory)][string]$FullPlayResult,[Parameter(Mandatory)][string]$NotBeforeUtc,[Parameter(Mandatory)][string]$Output,[string]$RetestResult,[switch]$Apply)
Set-StrictMode -Version Latest;$ErrorActionPreference='Stop'
$repo='C:/dev/TalesTactics';$logs=Join-Path $repo 'Logs/RosterMotion20261125';$baseline=Join-Path $logs 'Baseline-bdadaad52bb0415ea7e0fe660d597031'
function Hash([string]$p){(Get-FileHash -LiteralPath $p -Algorithm SHA256).Hash.ToLowerInvariant()}
function Under([string]$p,[string]$r){[IO.Path]::GetFullPath($p).StartsWith([IO.Path]::GetFullPath($r).TrimEnd('\','/')+'\',[StringComparison]::OrdinalIgnoreCase)}
function Read([string]$p){Get-Content -LiteralPath $p -Raw|ConvertFrom-Json}
function Save([string]$p,$o){$bytes=[Text.UTF8Encoding]::new($false).GetBytes(($o|ConvertTo-Json -Depth 12));$s=[IO.File]::Open($p,'CreateNew','Write','Read');try{$s.Write($bytes,0,$bytes.Length)}finally{$s.Dispose()}}
$Output=[IO.Path]::GetFullPath($Output);if(!(Under $Output $logs)-or(Test-Path -LiteralPath $Output)){throw 'Fresh task-local evidence directory required'}
if(!(Under $FullPlayResult $logs)){throw 'Current task PlayMode report required'}
$run=Read $FullPlayResult;if($run.PSObject.Properties.Name -contains 'result'){$run=$run.result};if($run -is [string]){$run=$run|ConvertFrom-Json}
if($run.status -cne 'completed' -or $run.summary.total -ne 321 -or @($run.results).Count -ne 321 -or $run.summary.skipped -ne 0 -or $run.summary.inconclusive -ne 0){throw 'Completed actual 321-case full PlayMode report required'}
$latest=$run;$retry=$null;$acceptance='Single full 321/321 passing run'
if($RetestResult){
 if(!(Under $RetestResult $logs)){throw 'Current task retest report required'}
 $retry=Read $RetestResult;if($retry.PSObject.Properties.Name -contains 'result'){$retry=$retry.result};if($retry -is [string]){$retry=$retry|ConvertFrom-Json}
 $failed=@($run.results|Where-Object Status -cne 'Passed')
 if($run.summary.passed -ne 320 -or $run.summary.failed -ne 1 -or $failed.Count -ne 1 -or $retry.status -cne 'completed' -or $retry.summary.total -ne 1 -or $retry.summary.passed -ne 1 -or $retry.summary.failed -ne 0 -or $retry.summary.skipped -ne 0 -or $retry.summary.inconclusive -ne 0 -or @($retry.results).Count -ne 1 -or $retry.results[0].FullName -cne $failed[0].FullName -or $retry.results[0].Status -cne 'Passed'){throw 'Only actual full320/321 plus exact same-test passing1/1 retry is accepted on this explicit path'}
 $identity=Read (Join-Path $logs 'Tests/full-retest-source-identity.json')
 if(!$identity.passed -or $identity.fileCountPerCheckout -ne 83 -or $identity.fullResultSHA256 -cne (Hash $FullPlayResult) -or $identity.retestResultSHA256 -cne (Hash $RetestResult)){throw 'Full/retry frozen source proof mismatch'}
 $freeze=Read (Join-Path $logs 'validation-product-freeze.json')
 if((Hash (Join-Path $logs 'validation-product-freeze.json')) -cne $identity.sourceFreezeSHA256){throw 'Frozen source manifest changed'}
 foreach($entry in $freeze.files){foreach($checkout in @($repo,(Join-Path $repo 'Builds/SourceOnlyPublish20261118/Project'))){if((Hash (Join-Path $checkout $entry.relative)) -cne $entry.sha256){throw 'Frozen product changed after full/retry evidence'}}}
 $latest=$retry;$acceptance='Full suite 320/321 plus unchanged-source isolated matching retry1/1; NOT one all-passing full run'
}else{if($run.summary.total -ne $run.summary.passed -or $run.summary.failed -ne 0){throw 'Full failure requires separate explicit matching retest evidence'}}
$since=[DateTimeOffset]::Parse($NotBeforeUtc).UtcDateTime
$user=Read (Join-Path $baseline 'user-tree.json');$storage=[IO.Path]::GetFullPath($user.root)
$expected=[IO.Path]::GetFullPath((Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData/LocalLow/LocalTactics/TalesTactics'));if($storage -ine $expected){throw 'Unexpected user storage'}
$xmlPath=Join-Path $storage 'TestResults.xml';$settings=[Xml.XmlReaderSettings]::new();$settings.DtdProcessing='Prohibit';$settings.XmlResolver=$null;$reader=[Xml.XmlReader]::Create($xmlPath,$settings);$xml=[Xml.XmlDocument]::new();$xml.XmlResolver=$null;try{$xml.Load($reader)}finally{$reader.Dispose()}
$cases=@($xml.SelectNodes('//test-case'));$testRoot=$xml.DocumentElement
if($testRoot.GetAttribute('result') -cne 'Passed' -or [int]$testRoot.GetAttribute('passed') -ne $latest.summary.total -or $cases.Count -ne $latest.summary.total -or @($cases|Where-Object {$_.GetAttribute('result') -cne 'Passed'}).Count -ne 0){throw 'XML does not identify matching passed regression result'}
if((@($cases|ForEach-Object {$_.GetAttribute('fullname')}|Sort-Object) -join "`n") -cne (@($latest.results|ForEach-Object FullName|Sort-Object) -join "`n")){throw 'XML test names differ from current PlayMode report'}
$xmlStart=[DateTimeOffset]::Parse($testRoot.GetAttribute('start-time')).UtcDateTime;if($xmlStart -lt $since){throw 'XML predates current test window'}
$known=@{};foreach($f in $user.files){$known[$f.name]=$f};$now=@(Get-ChildItem -LiteralPath $storage -File -Force -Recurse|Where-Object {!($_.DirectoryName -ieq $storage -and $_.Name -in @('Player.log','Player-prev.log'))})
$added=@();$changed=@();$seen=@{}
foreach($file in $now){$name=[IO.Path]::GetRelativePath($storage,$file.FullName).Replace('\','/');$seen[$name]=$true;if(!$known.ContainsKey($name)){$added+=[pscustomobject]@{name=$name;path=$file.FullName;sha256=(Hash $file.FullName);createdUtc=$file.CreationTimeUtc.ToString('o')}}elseif((Hash $file.FullName) -cne $known[$name].sha256){$changed+=$name}}
if(@($known.Keys|Where-Object {!$seen.ContainsKey($_)}).Count -gt 0){throw 'Existing user file missing; do not restore automatically'}
if(@($changed|Where-Object {$_ -cne 'TestResults.xml'}).Count -gt 0){throw 'Unexpected existing user file changed'}
$dirs=@(Get-ChildItem -LiteralPath $storage -Directory -Force -Recurse|ForEach-Object {[IO.Path]::GetRelativePath($storage,$_.FullName).Replace('\','/')})
$newDirs=@($dirs|Where-Object {$user.directories -cnotcontains $_})
if(@($newDirs|Where-Object {$_ -cnotmatch '^StorageTests/[0-9a-f]{32}$'}).Count -gt 0 -or @($user.directories|Where-Object {$dirs -cnotcontains $_}).Count -gt 0){throw 'Unexpected user directory change'}
foreach($f in $added){if($f.name -cnotmatch '^StorageTests/[0-9a-f]{32}/campaign\.json(?:\.(?:bak|v1\.bak|v2\.bak|corrupt-[0-9a-f]{32}))?$' -or [DateTime]::Parse($f.createdUtc).ToUniversalTime() -lt $since -or $newDirs -cnotcontains $f.name.Substring(0,$f.name.LastIndexOf('/'))){throw 'New file not attributable to current isolated StorageTests scope'}}
$original=@(Read (Join-Path $baseline 'user-diagnostic-copies.json'))|Where-Object {[IO.Path]::GetFullPath($_.path) -ieq [IO.Path]::GetFullPath($xmlPath)}
if(@($original).Count -ne 1 -or (Hash $original.copy) -cne $original.sha256){throw 'Original XML bytes unavailable'}
$csv=@(Read (Join-Path $baseline 'protected-current.json')|Where-Object {$_.checkout -eq 'validation' -and $_.relative -in @('Docs/BattleFlow/enemy-speed.csv','Docs/combat-party-matrix.csv','Docs/skill-panel-review.csv','Docs/tactical-scenarios.csv')}|Where-Object {(Hash $_.path) -cne $_.sha256})
foreach($c in $csv){if((Hash $c.copy) -cne $c.sha256 -or (Get-Item -LiteralPath $c.path).LastWriteTimeUtc -lt $since){throw 'CSV source or current test-run provenance mismatch'}}
[IO.Directory]::CreateDirectory($Output)|Out-Null
$plan=[ordered]@{apply=[bool]$Apply;acceptance=$acceptance;fullPlayResultSha256=(Hash $FullPlayResult);retestResultSha256=if($RetestResult){Hash $RetestResult}else{$null};xmlSHA256=(Hash $xmlPath);originalXmlSHA256=$original.sha256;newFiles=$added;newDirectories=$newDirs;changedExisting=$changed;restorableValidationCsv=$csv;noExistingFilesDeleted=$true}
Save (Join-Path $Output 'plan.json') $plan
if($Apply){
 foreach($c in $csv){$dest=Join-Path $Output ('RegressionArtifacts/Validation/'+$c.relative);[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($dest))|Out-Null;$sha=Hash $c.path;[IO.File]::Copy($c.path,$dest,$false);if((Hash $dest) -cne $sha){throw 'CSV archive mismatch'}}
 foreach($f in $added){$dest=Join-Path $Output ('RegressionArtifacts/'+$f.name);[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($dest))|Out-Null;if(!(Under $f.path $storage)-or(Hash $f.path) -cne $f.sha256){throw 'New diagnostic changed after plan'};[IO.File]::Copy($f.path,$dest,$false);if((Hash $dest) -cne $f.sha256){throw 'Archive mismatch'}}
 [IO.File]::Copy($xmlPath,(Join-Path $Output 'TestResults.current-run.xml'),$false)
 if((Hash $xmlPath) -cne $plan.xmlSHA256 -or (Hash (Join-Path $Output 'TestResults.current-run.xml')) -cne $plan.xmlSHA256){throw 'Current XML changed while archiving'}
 foreach($f in $added){if(!(Under $f.path $storage)-or(Hash $f.path) -cne $f.sha256){throw 'Diagnostic changed before removal'};Remove-Item -LiteralPath $f.path -Force}
 foreach($name in $newDirs){$dir=[IO.Path]::GetFullPath((Join-Path $storage $name));if(!(Under $dir $storage)-or $name -cnotmatch '^StorageTests/[0-9a-f]{32}$' -or @(Get-ChildItem -LiteralPath $dir -Force).Count -ne 0){throw 'Expected exact empty generated GUID directory'};Remove-Item -LiteralPath $dir}
 foreach($c in $csv){[IO.File]::Copy($c.copy,$c.path,$true);if((Hash $c.path) -cne $c.sha256){throw 'CSV restoration mismatch'}}
 [IO.File]::Copy($original.copy,$xmlPath,$true);if((Hash $xmlPath) -cne $original.sha256){throw 'Original XML restoration mismatch'}
}
Save (Join-Path $Output 'result.json') ([ordered]@{passed=$true;applied=[bool]$Apply;archivedNewFileCount=$added.Count;archivedNewDirectoryCount=$newDirs.Count;restoredPriorXML=[bool]$Apply;existingFilesDeleted=0;restoredValidationCsvCount=$csv.Count;fullPlayCaseCount=$run.summary.total;fullPlayPassed=$run.summary.passed;fullPlayFailed=$run.summary.failed;latestDiagnosticCaseCount=$cases.Count;retestedPassed=if($retry){$retry.summary.passed}else{0};acceptance=$acceptance;notes='Exact newly created StorageTests files archived before removal; all existing user files except the current regression XML must already match baseline. Prior XML exact bytes restored only after current passed-run provenance checks.'})
Write-Output ('Prepared/restored regression diagnostics: files '+$added.Count+', directories '+$newDirs.Count+', apply '+[bool]$Apply)