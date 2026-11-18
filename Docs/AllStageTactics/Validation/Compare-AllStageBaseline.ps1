[CmdletBinding()]
param(
    [string]$ProjectRoot='C:/dev/TalesTactics',
    [string]$BaselineManifestPath='C:/dev/TalesTactics/Logs/AllStageTactics20261118/Baseline-d8380e47deed41c19c7b19acfa08ce42/baseline.json'
)
# Read-only comparison; the only writes are new JSON files inside this invocation's GUID Logs directory.
# No Unity/player/test execution, Git mutation, restoration, deletion, exclusion of test artifacts or upload.
# Run only when the caller wants a fresh preservation verdict. Preparation of this script is not a verdict.
# Exit 0 requires all12 protected bytes, the ENTIRE user tree and the develop branch to match the policy.
# Expected test-output changes remain listed and make wholeTaskTreeUnchanged false; callers must classify
# them explicitly rather than silently excluding them or asserting that the complete tree was unchanged.
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$started=[DateTime]::UtcNow
$utf8=New-Object Text.UTF8Encoding($false)
$project=[IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\','/')
if(!$project.Equals('C:\dev\TalesTactics',[StringComparison]::OrdinalIgnoreCase)){throw 'This comparator is scoped to C:/dev/TalesTactics.'}
$logRoot=Join-Path $project 'Logs/AllStageTactics20261118'
$expectedBaseline=Join-Path $logRoot 'Baseline-d8380e47deed41c19c7b19acfa08ce42/baseline.json'
$baselinePath=[IO.Path]::GetFullPath($BaselineManifestPath)
if(!$baselinePath.Equals([IO.Path]::GetFullPath($expectedBaseline),[StringComparison]::OrdinalIgnoreCase)){throw 'Use this task''s fresh d838 baseline, not a historical task baseline.'}
$baselineDirectory=[IO.Path]::GetDirectoryName($baselinePath)
$userRoot=[IO.Path]::GetFullPath((Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData/LocalLow/LocalTactics/TalesTactics')).TrimEnd('\','/')
$excludedRootFiles=@('Player.log','Player-prev.log')
$runId=[Guid]::NewGuid().ToString('N')
$run=Join-Path $logRoot ('Preservation-'+$runId)
function Under-Path([string]$Child,[string]$Parent){[IO.Path]::GetFullPath($Child).StartsWith([IO.Path]::GetFullPath($Parent).TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)}
function No-Reparse([string]$Path){
    $part=[IO.Path]::GetFullPath($Path)
    while(![string]::IsNullOrEmpty($part)){
        if(Test-Path -LiteralPath $part){$item=Get-Item -LiteralPath $part -Force;if(($item.Attributes-band [IO.FileAttributes]::ReparsePoint)-ne 0){throw ('Reparse path: '+$part)}}
        $parent=[IO.Path]::GetDirectoryName($part);if($parent -eq $part){break};$part=$parent
    }
}
function Write-NewJson([string]$Name,$Value){
    $path=Join-Path $run $Name
    if(!(Under-Path $path $run)){throw 'Evidence write escaped this GUID directory.'}
    No-Reparse $path
    $bytes=$utf8.GetBytes((ConvertTo-Json -InputObject $Value -Depth 100)+[Environment]::NewLine)
    $stream=New-Object IO.FileStream($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{$stream.Write($bytes,0,$bytes.Length);$stream.Flush($true)}finally{$stream.Dispose()}
}
function Read-StableFile([string]$Path,[switch]$KeepBytes){
    No-Reparse $Path;$before=Get-Item -LiteralPath $Path -Force
    if($before.PSIsContainer){throw ('Expected a file: '+$Path)}
    $algorithm=[Security.Cryptography.SHA256]::Create()
    $stream=New-Object IO.FileStream($Path,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
    $data=$null
    try{
        $hash=[BitConverter]::ToString($algorithm.ComputeHash($stream)).Replace('-','').ToLowerInvariant()
        if($KeepBytes){$stream.Position=0;$memory=New-Object IO.MemoryStream;try{$stream.CopyTo($memory);$data=$memory.ToArray()}finally{$memory.Dispose()}}
    }finally{$stream.Dispose();$algorithm.Dispose()}
    No-Reparse $Path;$after=Get-Item -LiteralPath $Path -Force
    if($before.Length -ne $after.Length -or $before.LastWriteTimeUtc -ne $after.LastWriteTimeUtc){throw ('File changed during read: '+$Path)}
    [pscustomobject]@{bytes=[long]$after.Length;sha256=$hash;lastWriteUtc=$after.LastWriteTimeUtc.ToString('o');data=$data}
}
function Hash([string]$Path){(Read-StableFile $Path).sha256}
function Same-Bytes([byte[]]$A,[byte[]]$B){if($A.Length -ne $B.Length){return $false};for($i=0;$i -lt $A.Length;$i++){if($A[$i] -ne $B[$i]){return $false}};return $true}
function Read-GitState {
    $head=@(& git -C $project rev-parse HEAD);if($LASTEXITCODE -ne 0){throw 'Cannot read Git HEAD.'}
    $branch=@(& git -C $project branch --show-current);if($LASTEXITCODE -ne 0){throw 'Cannot read Git branch.'}
    $dirty=@(& git --no-optional-locks -C $project status --porcelain=v1 --untracked-files=all);if($LASTEXITCODE -ne 0){throw 'Cannot read Git dirty paths.'}
    [pscustomobject]@{capturedUtc=[DateTime]::UtcNow.ToString('o');head=($head -join '').Trim();branch=($branch -join '').Trim();dirty=$dirty}
}
function User-Inventory {
    No-Reparse $userRoot
    $exists=Test-Path -LiteralPath $userRoot -PathType Container
    $files=New-Object 'System.Collections.Generic.List[object]';$directories=New-Object 'System.Collections.Generic.List[string]';$logs=New-Object 'System.Collections.Generic.List[string]'
    if($exists){
        $pending=New-Object 'System.Collections.Generic.Stack[string]';$pending.Push($userRoot)
        while($pending.Count -gt 0){
            $directory=$pending.Pop();No-Reparse $directory
            foreach($item in @(Get-ChildItem -LiteralPath $directory -Force -ErrorAction Stop)){
                if(($item.Attributes-band [IO.FileAttributes]::ReparsePoint)-ne 0){throw ('Reparse item in user tree: '+$item.FullName)}
                if(!(Under-Path $item.FullName $userRoot)){throw 'User enumeration escaped its root.'}
                $name=$item.FullName.Substring($userRoot.Length+1).Replace('\','/')
                if($item.PSIsContainer){$directories.Add($name);$pending.Push($item.FullName);continue}
                if($excludedRootFiles -icontains $name){$logs.Add($name);continue}
                $files.Add([pscustomobject]@{name=$name;bytes=[long]$item.Length;lastWriteUtc=$item.LastWriteTimeUtc.ToString('o')})
            }
        }
    }
    [pscustomobject]@{exists=[bool]$exists;files=@($files.ToArray()|Sort-Object name);directories=@($directories.ToArray()|Sort-Object);observedExcludedFiles=@($logs.ToArray()|Sort-Object)}
}
function Snapshot-UserTree {
    $first=User-Inventory
    $files=@(foreach($entry in $first.files){
        $file=Read-StableFile (Join-Path $userRoot $entry.name)
        if($file.bytes -ne $entry.bytes -or $file.lastWriteUtc -cne $entry.lastWriteUtc){throw ('User file changed between inventory and hashing: '+$entry.name)}
        [pscustomobject]@{name=$entry.name;bytes=$file.bytes;sha256=$file.sha256;lastWriteUtc=$file.lastWriteUtc}
    })
    $second=User-Inventory
    $left=@($first.files|ForEach-Object {$_.name+'|'+$_.bytes+'|'+$_.lastWriteUtc});$right=@($second.files|ForEach-Object {$_.name+'|'+$_.bytes+'|'+$_.lastWriteUtc})
    if($first.exists -ne $second.exists -or ($left -join "`n") -cne ($right -join "`n") -or ($first.directories -join "`n") -cne ($second.directories -join "`n")){throw 'User tree changed during snapshot; no consistent comparison is claimed.'}
    [pscustomobject]@{schema=1;purpose='Current recursive user tree at the comparison timestamp.';capturedUtc=[DateTime]::UtcNow.ToString('o');root=$userRoot;exists=$first.exists;files=$files;directories=$first.directories;excludedRootFileNames=$excludedRootFiles;observedExcludedFiles=$second.observedExcludedFiles;raceChecked=$true;writesToUserTree=$false}
}
function File-Map($Rows){
    $map=New-Object 'System.Collections.Generic.Dictionary[string,object]' ([StringComparer]::OrdinalIgnoreCase)
    foreach($row in @($Rows)){
        if([string]::IsNullOrWhiteSpace($row.name) -or $row.name.Contains('\') -or [IO.Path]::IsPathRooted($row.name) -or !(Under-Path (Join-Path $userRoot $row.name) $userRoot) -or $map.ContainsKey($row.name)){throw ('Invalid or duplicate snapshot name: '+$row.name)}
        if($row.bytes -lt 0 -or $row.sha256 -cnotmatch '^[0-9a-f]{64}$'){throw ('Invalid snapshot hash/size: '+$row.name)}
        $map.Add($row.name,$row)
    }
    return ,$map
}
function Timestamp-Text($Value){if($Value -is [DateTime]){return $Value.ToUniversalTime().ToString('o')};return [string]$Value}
function Compare-Trees($Before,$After){
    $old=File-Map $Before.files;$new=File-Map $After.files
    $added=@();$removed=@();$changed=@();$timestampOnly=@()
    foreach($name in @($old.Keys|Sort-Object)){
        $beforeRow=$old[$name]
        if(!$new.ContainsKey($name)){$removed+=$beforeRow;continue}
        $afterRow=$new[$name]
        if($beforeRow.name -cne $afterRow.name -or $beforeRow.bytes -ne $afterRow.bytes -or $beforeRow.sha256 -cne $afterRow.sha256){$changed+=[pscustomobject]@{name=$name;before=$beforeRow;after=$afterRow}}
        elseif((Timestamp-Text $beforeRow.lastWriteUtc) -cne (Timestamp-Text $afterRow.lastWriteUtc)){$timestampOnly+=[pscustomobject]@{name=$name;beforeUtc=$beforeRow.lastWriteUtc;afterUtc=$afterRow.lastWriteUtc;bytesUnchanged=$true}}
    }
    foreach($name in @($new.Keys|Sort-Object)){if(!$old.ContainsKey($name)){$added+=$new[$name]}}
    $dirsBefore=New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    $dirsAfter=New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::Ordinal)
    foreach($name in @($Before.directories)){if([string]::IsNullOrWhiteSpace($name) -or [IO.Path]::IsPathRooted($name) -or !(Under-Path (Join-Path $userRoot $name) $userRoot) -or !$dirsBefore.Add($name)){throw 'Invalid or duplicate baseline directory.'}}
    foreach($name in @($After.directories)){if(!$dirsAfter.Add($name)){throw 'Duplicate current directory.'}}
    $directoriesAdded=@($After.directories|Where-Object {!$dirsBefore.Contains($_)});$directoriesRemoved=@($Before.directories|Where-Object {!$dirsAfter.Contains($_)})
    $allChanges=@($added|ForEach-Object {[pscustomobject]@{name=$_.name;kind='added'}})+@($removed|ForEach-Object {[pscustomobject]@{name=$_.name;kind='removed'}})+@($changed|ForEach-Object {[pscustomobject]@{name=$_.name;kind='changed'}})
    $rootChanges=@($allChanges|Where-Object {$_.name -notmatch '/'})
    $groups=@($allChanges|Group-Object {if($_.name -match '/'){$_.name.Split('/')[0]}else{'<root>'}}|ForEach-Object {[pscustomobject]@{topLevel=$_.Name;added=@($_.Group|Where-Object kind -eq 'added').Count;removed=@($_.Group|Where-Object kind -eq 'removed').Count;changed=@($_.Group|Where-Object kind -eq 'changed').Count}})
    $unchanged=$Before.exists -eq $After.exists -and $added.Count -eq 0 -and $removed.Count -eq 0 -and $changed.Count -eq 0 -and $directoriesAdded.Count -eq 0 -and $directoriesRemoved.Count -eq 0
    [pscustomobject]@{wholeTaskTreeUnchanged=[bool]$unchanged;rootFilesUnchanged=($rootChanges.Count -eq 0 -and $Before.exists -eq $After.exists);rootExistenceBefore=$Before.exists;rootExistenceAfter=$After.exists;beforeFileCount=@($Before.files).Count;afterFileCount=@($After.files).Count;beforeDirectoryCount=@($Before.directories).Count;afterDirectoryCount=@($After.directories).Count;filesAdded=$added;filesRemoved=$removed;filesChanged=$changed;timestampOnlyChanges=$timestampOnly;directoriesAdded=$directoriesAdded;directoriesRemoved=$directoriesRemoved;rootFileChanges=$rootChanges;fileChangesByTopLevel=$groups;exclusions=$excludedRootFiles;classification='Every file/directory difference is retained. Root TestResults files and StorageTests directories are not silently excluded or counted as unchanged save data.'}
}
No-Reparse $project;No-Reparse $logRoot;No-Reparse $run
if(!(Under-Path $run $logRoot) -or (Under-Path $run $userRoot) -or (Under-Path $userRoot $run) -or (Test-Path -LiteralPath $run)){throw 'Unsafe or pre-existing evidence directory.'}
[IO.Directory]::CreateDirectory($run)|Out-Null
$errors=New-Object 'System.Collections.Generic.List[string]'
$baselineHash=$null;$scriptHash=$null;$baseline=$null;$protected=@();$comparison=$null;$currentTree=$null;$gitBefore=$null;$gitAfter=$null;$baselineVerified=$false;$protectedUnchanged=$false;$gitDevelop=$false
try{
    $scriptHash=Hash $PSCommandPath;$baselineHash=Hash $baselinePath
    $baseline=Get-Content -LiteralPath $baselinePath -Raw -Encoding UTF8|ConvertFrom-Json
    if(!$baseline.passed -or $baseline.schema -ne 1 -or $baseline.protectedCount -ne 12 -or !$baseline.protectedByteCopies -or $baseline.priorHashValuesUsedAsBaseline -or $baseline.gitBranch -cne 'develop' -or !([IO.Path]::GetFullPath($baseline.project).Equals($project,[StringComparison]::OrdinalIgnoreCase))){throw 'Invalid or unsuccessful task baseline.'}
    foreach($name in @('protected-current.json','user-tree.json','git-state.json')){
        $path=Join-Path $baselineDirectory $name
        if((Hash $path) -cne $baseline.($name+'SHA256')){throw ('Baseline evidence hash changed: '+$name)}
    }
    $protectedInput=Get-Content -LiteralPath (Join-Path $baselineDirectory 'protected-current.json') -Raw -Encoding UTF8|ConvertFrom-Json
    $beforeTree=Get-Content -LiteralPath (Join-Path $baselineDirectory 'user-tree.json') -Raw -Encoding UTF8|ConvertFrom-Json
    if(@($protectedInput).Count -ne 12 -or @($beforeTree.files).Count -ne $baseline.userFileCount -or @($beforeTree.directories).Count -ne $baseline.userDirectoryCount -or !([IO.Path]::GetFullPath($beforeTree.root).TrimEnd('\','/').Equals($userRoot,[StringComparison]::OrdinalIgnoreCase)) -or (@($beforeTree.excludedRootFileNames|Sort-Object) -join '|') -cne (@($excludedRootFiles|Sort-Object) -join '|') -or !$beforeTree.raceChecked -or $beforeTree.writesToUserTree){throw 'Baseline tree scope/counts differ from the declared task baseline.'}
    $gitBefore=Read-GitState
    $seen=New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $protected=@(foreach($entry in $protectedInput){
        if([IO.Path]::IsPathRooted($entry.path) -or !$seen.Add($entry.path)){throw 'Invalid or duplicate protected relative path.'}
        $source=[IO.Path]::GetFullPath((Join-Path $project $entry.path));$copy=[IO.Path]::GetFullPath((Join-Path $baselineDirectory ('ProtectedBytes/'+$entry.path)))
        if(!(Under-Path $source $project) -or !(Under-Path $copy (Join-Path $baselineDirectory 'ProtectedBytes')) -or !($copy.Equals([IO.Path]::GetFullPath($entry.copy),[StringComparison]::OrdinalIgnoreCase))){throw 'Protected path or byte copy escaped its root.'}
        $reference=Read-StableFile $copy -KeepBytes
        if($reference.sha256 -cne $entry.sha256 -or $reference.sha256 -cne $entry.copySHA256 -or $reference.bytes -ne $entry.bytes){throw ('Protected baseline byte copy no longer matches: '+$entry.path)}
        No-Reparse $source;$exists=Test-Path -LiteralPath $source -PathType Leaf;$actual=$null;$equal=$false
        if($exists){$actual=Read-StableFile $source -KeepBytes;$equal=Same-Bytes $reference.data $actual.data}
        [pscustomobject]@{path=$entry.path;exists=[bool]$exists;baselineBytes=$reference.bytes;baselineSHA256=$reference.sha256;currentBytes=$(if($actual){$actual.bytes}else{$null});currentSHA256=$(if($actual){$actual.sha256}else{$null});currentLastWriteUtc=$(if($actual){$actual.lastWriteUtc}else{$null});exactBytesUnchanged=[bool]$equal;baselineCopy=$copy;baselineCopyVerified=$true}
    })
    $baselineVerified=$true;Write-NewJson 'protected-comparison.json' $protected
    $currentTree=Snapshot-UserTree;Write-NewJson 'user-tree-current.json' $currentTree
    $comparison=Compare-Trees $beforeTree $currentTree;Write-NewJson 'user-tree-comparison.json' $comparison
    # Protected files span the whole snapshot window, not only the first read.
    foreach($row in $protected){
        $path=Join-Path $project $row.path;No-Reparse $path;$exists=Test-Path -LiteralPath $path -PathType Leaf
        if($exists -ne $row.exists){throw ('Protected file existence changed during audit: '+$row.path)}
        if($exists){$now=Read-StableFile $path;if($now.sha256 -cne $row.currentSHA256 -or $now.bytes -ne $row.currentBytes -or $now.lastWriteUtc -cne $row.currentLastWriteUtc){throw ('Protected file changed during audit: '+$row.path)}}
    }
    $protectedUnchanged=@($protected|Where-Object {!$_.exactBytesUnchanged}).Count -eq 0
    $gitAfter=Read-GitState;$gitDevelop=$gitBefore.branch -ceq 'develop' -and $gitAfter.branch -ceq 'develop'
    Write-NewJson 'git-state.json' ([ordered]@{before=$gitBefore;after=$gitAfter;requiredBranch='develop';branchValid=[bool]$gitDevelop;baselineHead=$baseline.gitBase;headChangedSinceBaseline=($gitAfter.head -cne $baseline.gitBase);headStableDuringComparison=($gitBefore.head -ceq $gitAfter.head);dirtyPathsChangedDuringComparison=(($gitBefore.dirty -join "`n") -cne ($gitAfter.dirty -join "`n"));gitMutatedByComparator=$false})
    if(!$gitDevelop -or $gitBefore.head -cne $gitAfter.head){throw 'Git must remain on develop with a stable HEAD during comparison.'}
    if((Hash $baselinePath) -cne $baselineHash -or (Hash $PSCommandPath) -cne $scriptHash){throw 'Baseline manifest or comparator changed during the comparison.'}
    foreach($name in @('protected-current.json','user-tree.json','git-state.json')){if((Hash (Join-Path $baselineDirectory $name)) -cne $baseline.($name+'SHA256')){throw ('Baseline evidence changed during the comparison: '+$name)}}
    $protectedUnchanged=@($protected|Where-Object {!$_.exactBytesUnchanged}).Count -eq 0
    if(!$protectedUnchanged){$errors.Add('One or more protected files differ from the exact task-start byte copies. No file was restored.')}
    if(!$comparison.wholeTaskTreeUnchanged){$errors.Add('The complete user tree differs from the task baseline. Every change is retained for explicit classification; no file was restored or deleted.')}
}catch{$errors.Add($_.Exception.ToString())}
$receipt=[ordered]@{schema=1;purpose='AllStageTactics preservation comparison against the fresh task-start baseline at the stated invocation timestamp; no implied earlier/later state.';runId=$runId;startedUtc=$started.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o');passed=($errors.Count -eq 0 -and $baselineVerified -and $protectedUnchanged -and $null -ne $comparison -and $comparison.wholeTaskTreeUnchanged -and $gitDevelop);project=$project;output=$run;script=$PSCommandPath;scriptSHA256=$scriptHash;baseline=$baselinePath;baselineSHA256=$baselineHash;baselineVerified=$baselineVerified;protectedCount=$protected.Count;protectedExactBytesUnchanged=$protectedUnchanged;protectedChanged=@($protected|Where-Object {!$_.exactBytesUnchanged}|ForEach-Object path);wholeTaskTreeUnchanged=$(if($comparison){$comparison.wholeTaskTreeUnchanged}else{$null});rootFilesUnchanged=$(if($comparison){$comparison.rootFilesUnchanged}else{$null});requiredGitBranch='develop';gitDevelop=$gitDevelop;errors=@($errors.ToArray());writesToUserTree=$false;restoredFiles=@();deletedFiles=@();unityOrPlayersStarted=$false;gitMutated=$false;excludedRootFiles=$excludedRootFiles;limits='File contents/paths and all directory paths are compared. Timestamp-only file changes are reported separately but do not change byte equality. Full-tree changes, including tests, are never excluded. Read locks and double inventories reject observed races/reparse points, but do not prove atomic filesystem state or detect change-and-restore to identical bytes/timestamps. The caller determines whether this timestamp is after all required tests/builds/state restoration.'}
$artifacts=@()
foreach($name in @('protected-comparison.json','user-tree-current.json','user-tree-comparison.json','git-state.json')){$path=Join-Path $run $name;if(Test-Path -LiteralPath $path -PathType Leaf){$artifacts+=[pscustomobject]@{path=$path;sha256=(Hash $path)}}}
$receipt.artifacts=$artifacts;Write-NewJson 'preservation.json' $receipt
[pscustomobject]@{passed=$receipt.passed;receipt=(Join-Path $run 'preservation.json');protectedExactBytesUnchanged=$protectedUnchanged;wholeTaskTreeUnchanged=$receipt.wholeTaskTreeUnchanged;gitDevelop=$gitDevelop;errorCount=$errors.Count}|ConvertTo-Json -Depth 5
if(!$receipt.passed){exit 1}