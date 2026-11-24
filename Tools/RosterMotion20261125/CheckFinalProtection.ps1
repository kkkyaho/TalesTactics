[CmdletBinding()]
param([Parameter(Mandatory)][string]$Manifest,[Parameter(Mandatory)][string]$Output,[switch]$RequireBuilds)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$repo='C:/dev/TalesTactics';$public=Join-Path $repo 'Builds/SourceOnlyPublish20261118/Project'
$evidence=Join-Path $repo 'Logs/RosterMotion20261125';$baseline=Join-Path $evidence 'Baseline-bdadaad52bb0415ea7e0fe660d597031'
$baselineSha='cb2d688f2e12323568986d884a584c459dd5acd9ef9aba74999402772e7f955c'
function Hash([string]$path){if(!(Test-Path -LiteralPath $path -PathType Leaf)){return $null};(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function Read([string]$path){Get-Content -LiteralPath $path -Raw|ConvertFrom-Json}
function Under([string]$path,[string]$root){[IO.Path]::GetFullPath($path).StartsWith([IO.Path]::GetFullPath($root).TrimEnd('\','/')+'\',[StringComparison]::OrdinalIgnoreCase)}
function Relative([string]$root,[string]$path){[IO.Path]::GetRelativePath($root,$path).Replace('\','/')}
function Check([string]$name,[bool]$ok,$value){$checks.Add([pscustomobject]@{name=$name;passed=$ok;observed=$value});if(!$ok){$failures.Add($name)}}
function WriteNew([string]$path,$value){$bytes=[Text.UTF8Encoding]::new($false).GetBytes(($value|ConvertTo-Json -Depth 12));$s=[IO.File]::Open($path,'CreateNew','Write','Read');try{$s.Write($bytes,0,$bytes.Length)}finally{$s.Dispose()}}
if(!(Under $Output $evidence)-or(Test-Path -LiteralPath $Output)){throw 'Fresh task-local evidence path required'}
$checks=[Collections.Generic.List[object]]::new();$failures=[Collections.Generic.List[string]]::new()
$report=[ordered]@{schema=1;passed=$false;scope='Strict fresh-baseline preservation after explicit approved product changes. Local-only detailed evidence; do not publish user-tree paths.';baseline=$baseline;checkedUtc=[DateTime]::UtcNow.ToString('o');checks=@();failures=@()}
try{
 $b=Read (Join-Path $baseline 'baseline.json');if((Hash (Join-Path $baseline 'baseline.json')) -cne $baselineSha){throw 'Immutable baseline changed'}
 $valid=@($b.outputs|Where-Object {(Hash (Join-Path $baseline $_.name)) -cne $_.sha256});Check 'Immutable baseline report hashes' ($valid.Count -eq 0) $valid
 if($valid.Count){throw 'Baseline evidence changed'}
 $manifestValue=Read $Manifest;$products=@{};foreach($p in $manifestValue.files){if($products.ContainsKey($p.relative)){throw 'Duplicate product path'};$products[$p.relative]=$p}
 if($products.Count -eq 0){throw 'Empty explicit final product manifest'}
 $protected=@(Read (Join-Path $baseline 'protected-current.json'))
 $copyMismatches=@($protected|Where-Object {(Hash $_.copy) -cne $_.sha256});Check 'Immutable protected original byte copies' ($copyMismatches.Count -eq 0) @($copyMismatches|ForEach-Object relative)
 $rows=@(foreach($p in $protected){$approved=$products.ContainsKey($p.relative);$sha=Hash $p.path;[pscustomobject]@{checkout=$p.checkout;relative=$p.relative;approvedProduct=$approved;beforeSha256=$p.sha256;currentSha256=$sha;match=($sha -ceq $p.sha256);exists=($null -ne $sha)}})
 $report.protected=$rows;Check 'Protected files retain baseline bytes except frozen products' (@($rows|Where-Object {!$_.approvedProduct -and !$_.match}).Count -eq 0) @($rows|Where-Object {!$_.approvedProduct -and !$_.match})
 $frozen=@(foreach($p in $manifestValue.files){if($p.relative -match '(^|/)LocalMusic(/|$)|\.(mp3|apk|aab|exe|zip)$'){throw 'Private music/build path in product manifest'};foreach($checkout in @(@{name='root';path=$repo},@{name='validation';path=$public})){$path=Join-Path $checkout.path $p.relative;if(!(Under $path $checkout.path)){throw 'Product escaped checkout'};$sha=Hash $path;[pscustomobject]@{checkout=$checkout.name;relative=$p.relative;match=($sha -ceq $p.sha256);sha256=$sha;expectedSha256=$p.sha256}}})
 $report.frozenProducts=$frozen;Check 'Final frozen products match in both checkouts' (@($frozen|Where-Object {!$_.match}).Count -eq 0) @($frozen|Where-Object {!$_.match})
 foreach($checkout in @(@{name='root';path=$repo;file='git-root-before.json'},@{name='validation';path=$public;file='git-validation-before.json'})){$prior=Read (Join-Path $baseline $checkout.file);$head=(& git --no-optional-locks -C $checkout.path rev-parse HEAD).Trim();$branch=(& git --no-optional-locks -C $checkout.path branch --show-current).Trim();Check ($checkout.name+' pre-commit HEAD and branch unchanged') ($head -ceq $prior.head -and $branch -ceq $prior.branch) ([pscustomobject]@{head=$head;branch=$branch})}
 $modified=@(foreach($pair in @(@{name='root';file='git-root-before.json'},@{name='validation';file='git-validation-before.json'})){$old=Read (Join-Path $baseline $pair.file);foreach($path in $old.trackedStatus){$row=@($rows|Where-Object {$_.checkout -eq $pair.name -and $_.relative -ceq $path});[pscustomobject]@{checkout=$pair.name;relative=$path;match=($row.Count -eq 1 -and $row[0].match)}}})
 $report.originalModifications=$modified;Check 'All original ROOT12 and PUBLIC23 tracked changes retain exact bytes' ($modified.Count -eq 35 -and @($modified|Where-Object {!$_.match}).Count -eq 0) @($modified|Where-Object {!$_.match})
 $art=@(Read (Join-Path $baseline 'art-inventory.json'));$artRows=@(foreach($a in $art){$sha=Hash $a.path;[pscustomobject]@{checkout=$a.checkout;relative=$a.relative;match=($sha -ceq $a.sha256);sha256=$sha}});$report.originalArt=$artRows;Check 'All baseline artwork preserved' ($artRows.Count -eq $b.artFileCount -and @($artRows|Where-Object {!$_.match}).Count -eq 0) @($artRows|Where-Object {!$_.match})
 $apks=@(Read (Join-Path $baseline 'existing-apks.json'));$apkRows=@(foreach($a in $apks){[pscustomobject]@{path=$a.path;match=((Hash $a.path) -ceq $a.sha256)}});$report.existingApks=$apkRows;Check 'All six preexisting APKs preserved' ($apkRows.Count -eq 6 -and @($apkRows|Where-Object {!$_.match}).Count -eq 0) @($apkRows|Where-Object {!$_.match})
 $user=Read (Join-Path $baseline 'user-tree.json');$userRoot=[IO.Path]::GetFullPath($user.root)
 $expected=[IO.Path]::GetFullPath((Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData/LocalLow/LocalTactics/TalesTactics'));if($userRoot -ine $expected){throw 'Unexpected user tree root'}
 $known=@{};foreach($u in $user.files){$known[$u.name]=$u}
 $now=@(Get-ChildItem -LiteralPath $userRoot -File -Force -Recurse|Where-Object { !($_.DirectoryName -ieq $userRoot -and $_.Name -in @('Player.log','Player-prev.log')) })
 $current=@{};$changed=[Collections.Generic.List[string]]::new();$added=[Collections.Generic.List[string]]::new()
 foreach($f in $now){if(($f.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'User reparse point encountered'};$name=Relative $userRoot $f.FullName;$current[$name]=$true;if(!$known.ContainsKey($name)){$added.Add($name)}elseif((Hash $f.FullName) -cne $known[$name].sha256){$changed.Add($name)}}
 $deleted=@($user.files|Where-Object {!$current.ContainsKey($_.name)}|ForEach-Object name)
 $dirs=@(Get-ChildItem -LiteralPath $userRoot -Directory -Force -Recurse|ForEach-Object {Relative $userRoot $_.FullName})
 $newDirs=@($dirs|Where-Object {$user.directories -cnotcontains $_});$deletedDirs=@($user.directories|Where-Object {$dirs -cnotcontains $_})
 $report.userTree=[ordered]@{root=$userRoot;beforeCount=$known.Count;currentCount=$current.Count;addedFiles=@($added);changedFiles=@($changed);deletedFiles=$deleted;newDirectories=$newDirs;deletedDirectories=$deletedDirs}
 Check 'Whole user tree retains fresh baseline names and bytes' ($added.Count -eq 0 -and $changed.Count -eq 0 -and $deleted.Count -eq 0 -and $newDirs.Count -eq 0 -and $deletedDirs.Count -eq 0) $report.userTree
 $saves=@(Read (Join-Path $baseline 'actual-user-save-copies.json'));$saveRows=@(foreach($s in $saves){[pscustomobject]@{match=((Hash $s.path) -ceq $s.sha256)}});Check 'Nine actual campaign and settings saves exact bytes' ($saveRows.Count -eq 9 -and @($saveRows|Where-Object {!$_.match}).Count -eq 0) $saveRows
 if($RequireBuilds){foreach($platform in @('windows','android')){$build=Read (Join-Path $evidence ('Builds/'+$platform+'-build-final.json'));Check ($platform+' build passed and settings restored') ($build.passed -eq $true -and $build.settingsEqual -eq $true -and $build.errors -eq 0 -and $build.result -eq 'Succeeded') $build.result}}
}catch{$failures.Add($_.Exception.ToString())}
$report.checks=@($checks);$report.failures=@($failures);$report.passed=$failures.Count -eq 0
WriteNew $Output $report
$summary=[ordered]@{schema=1;passed=$report.passed;baselineSha256=$baselineSha;wholeUserTreeUnchanged=($report.Contains('userTree') -and $report.userTree.addedFiles.Count -eq 0 -and $report.userTree.changedFiles.Count -eq 0 -and $report.userTree.deletedFiles.Count -eq 0 -and $report.userTree.newDirectories.Count -eq 0 -and $report.userTree.deletedDirectories.Count -eq 0);checkCount=$checks.Count;failedCheckCount=$failures.Count;detailSha256=(Hash $Output);privacy='Counts only; detailed baseline paths and local-only source remain in local Logs.'}
WriteNew ($Output+'.summary.json') $summary
$summary|ConvertTo-Json -Depth 4
if(!$report.passed){exit 1}