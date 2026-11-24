[CmdletBinding()]
param(
    [string]$ProjectRoot='C:/dev/TalesTactics',
    [string]$ValidationRoot='C:/dev/TalesTactics/Builds/SourceOnlyPublish20261118/Project',
    [string]$OutputRoot='C:/dev/TalesTactics/Logs/RosterMotion20261125',
    [string]$ExpectedRootHead='d8bbf0d4c06fe15c857768e3c282bcfc086ee650',
    [int]$ExpectedRootModifiedCount=12,
    [string]$ExpectedValidationHead='d5c4d93a6bb84782c54f4437024921199815985e',
    [string]$ExpectedValidationBranch='codex/publish-source-20261118',
    [int]$ExpectedValidationModifiedCount=23
)
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$started=[DateTime]::UtcNow
$project=[IO.Path]::GetFullPath($ProjectRoot).TrimEnd('\','/')
$validation=[IO.Path]::GetFullPath($ValidationRoot).TrimEnd('\','/')
$output=[IO.Path]::GetFullPath($OutputRoot).TrimEnd('\','/')
$storage=[IO.Path]::GetFullPath((Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData/LocalLow/LocalTactics/TalesTactics'))
$utf8=New-Object Text.UTF8Encoding($false)
function Within([string]$path,[string]$root){$path.StartsWith($root.TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)}
function NoReparse([string]$path){$item=Get-Item -LiteralPath $path -Force;while($null -ne $item){if(($item.Attributes-band [IO.FileAttributes]::ReparsePoint)-ne 0){throw ('Reparse point: '+$item.FullName)};if($item -is [IO.FileInfo]){$item=$item.Directory}else{$item=$item.Parent}}}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
function WriteJson([string]$path,$value){$stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None);try{$bytes=$utf8.GetBytes(($value|ConvertTo-Json -Depth 32));$stream.Write($bytes,0,$bytes.Length)}finally{$stream.Dispose()}}
function InvokeGitRead([string]$root,[string[]]$arguments){$result=@(& git.exe --no-optional-locks -C $root -c core.quotePath=false @arguments);if($LASTEXITCODE -ne 0){throw ('git read failed: '+($arguments-join ' '))};return $result}
function State([string]$root){
    $status=@(InvokeGitRead $root @('status','--porcelain=v1','--untracked-files=all'))
    $tracked=@($status|Where-Object {!$_.StartsWith('?? ')}|ForEach-Object {if($_.Length-lt4-or $_.Substring(3).StartsWith('"')-or $_-match' -> '){throw 'Unexpected Git path encoding/rename'};$_.Substring(3)}|Sort-Object -Unique)
    [pscustomobject]@{capturedUtc=[DateTime]::UtcNow.ToString('o');root=$root;head=((InvokeGitRead $root @('rev-parse','HEAD'))-join '').Trim();branch=((InvokeGitRead $root @('branch','--show-current'))-join '').Trim();status=$status;trackedStatus=$tracked;modified=@((@(InvokeGitRead $root @('diff','--name-only'))+@(InvokeGitRead $root @('diff','--cached','--name-only')))|Sort-Object -Unique)}
}
function StableCopy([string]$source,[string]$destination){
    NoReparse $source;$before=Get-Item -LiteralPath $source -Force
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination))|Out-Null
    $src=[IO.File]::Open($source,[IO.FileMode]::Open,[IO.FileAccess]::Read,[IO.FileShare]::Read)
    try{$dst=[IO.File]::Open($destination,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None);try{$src.CopyTo($dst)}finally{$dst.Dispose()}}finally{$src.Dispose()}
    $after=Get-Item -LiteralPath $source -Force;$digest=Hash $destination
    if($before.Length-ne$after.Length -or $before.LastWriteTimeUtc-ne$after.LastWriteTimeUtc -or (Hash $source)-ne$digest){throw ('File changed while copying: '+$source)}
    [pscustomobject]@{path=$source;sha256=$digest;bytes=$after.Length;lastWriteUtc=$after.LastWriteTimeUtc.ToString('o');copy=$destination;copySha256=$digest}
}
function Inventory([string]$root){
    $files=New-Object 'System.Collections.Generic.List[object]';$dirs=New-Object 'System.Collections.Generic.List[string]';$logs=New-Object 'System.Collections.Generic.List[object]'
    $exists=Test-Path -LiteralPath $root -PathType Container
    if($exists){NoReparse $root;$todo=New-Object 'System.Collections.Generic.Stack[string]';$todo.Push($root)
    while($todo.Count-gt0){foreach($item in @(Get-ChildItem -LiteralPath $todo.Pop() -Force)){
        $rel=$item.FullName.Substring($root.TrimEnd('\','/').Length+1).Replace('\','/')
        if(($item.Attributes-band[IO.FileAttributes]::ReparsePoint)-ne0){throw ('Reparse point in user tree: '+$rel)}
        if($item.PSIsContainer){$dirs.Add($rel);$todo.Push($item.FullName);continue}
        $row=[pscustomobject]@{name=$rel;bytes=$item.Length;lastWriteUtc=$item.LastWriteTimeUtc.ToString('o')}
        if(@('Player.log','Player-prev.log')-icontains$rel){$logs.Add($row)}else{$files.Add($row)}
    }}}
    [pscustomobject]@{exists=$exists;files=@($files.ToArray()|Sort-Object name);directories=@($dirs.ToArray()|Sort-Object);excludedLogs=@($logs.ToArray()|Sort-Object name)}
}
if(!(Within $validation $project) -or !(Within $output $project) -or !(Within $output (Join-Path $project 'Logs'))){throw 'Validation/output paths must stay inside the designated project; output must stay under Logs.'}
NoReparse $project;NoReparse $validation
if(Test-Path -LiteralPath $output){NoReparse $output}else{[IO.Directory]::CreateDirectory($output)|Out-Null}
$run=Join-Path $output ('Baseline-'+[Guid]::NewGuid().ToString('N'))
if(Test-Path -LiteralPath $run){throw 'Refusing existing baseline output.'}
[IO.Directory]::CreateDirectory($run)|Out-Null;NoReparse $run
$failure=$null;$rootState=$null;$publicState=$null;$protected=@();$userFiles=@();$sourceComparison=@();$saveCopies=@();$diagnosticCopies=@();$artRows=@();$apkRows=@()
try{
    $rootState=State $project;$publicState=State $validation
    if($rootState.branch-ne'develop' -or ($ExpectedRootHead -and $rootState.head-ne$ExpectedRootHead) -or @($rootState.trackedStatus).Count-ne$ExpectedRootModifiedCount){throw 'Unexpected root branch, head or preexisting modified-file count.'}
    if($publicState.branch-ne$ExpectedValidationBranch -or $publicState.head-ne$ExpectedValidationHead -or @($publicState.trackedStatus).Count-ne$ExpectedValidationModifiedCount){throw 'Unexpected validation branch, head or tracked porcelain count.'}
    WriteJson (Join-Path $run 'git-root-before.json') $rootState
    WriteJson (Join-Path $run 'git-validation-before.json') $publicState
    $plan=New-Object 'System.Collections.Generic.List[object]'
    foreach($pair in @(@{name='root';path=$project;state=$rootState},@{name='validation';path=$validation;state=$publicState})){
        $selected=New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
        foreach($relative in @($pair.state.modified)+@($pair.state.trackedStatus)){[void]$selected.Add([IO.Path]::GetFullPath((Join-Path $pair.path $relative)))}
        foreach($relative in @('ProjectSettings','Assets/Settings','Assets/TalesTactics/Content','Assets/TalesTactics/Audio/LocalMusic','Assets/TalesTactics/Art/VisualPilot','Assets/TalesTactics/Resources/TalesTactics/VisualPilot')){
            $dir=Join-Path $pair.path $relative
            if(Test-Path -LiteralPath $dir -PathType Container){NoReparse $dir;foreach($file in @(Get-ChildItem -LiteralPath $dir -Recurse -File -Force)){[void]$selected.Add($file.FullName)}}
        }
        $assets=Join-Path $pair.path 'Assets/TalesTactics'
        foreach($file in @(Get-ChildItem -LiteralPath $assets -Recurse -File -Filter '*.meta')){[void]$selected.Add($file.FullName)}
        foreach($relative in @('Assets/TalesTactics/Resources/TalesTactics/KoreanFallback.asset','Assets/TalesTactics/Audio/LocalMusic.meta','Assets/TalesTactics/Resources/TalesTactics/UnitHeadAnchors.json','Packages/manifest.json','Packages/packages-lock.json')){if(Test-Path -LiteralPath (Join-Path $pair.path $relative) -PathType Leaf){[void]$selected.Add((Join-Path $pair.path $relative))}}
        foreach($file in @(Get-ChildItem -LiteralPath (Join-Path $pair.path 'Assets/TalesTactics/Art') -Recurse -File -Filter 'alphen*.png')){[void]$selected.Add($file.FullName)}
        foreach($relative in @(InvokeGitRead $pair.path @('ls-files','--','Assets/TalesTactics/*.cs','Assets/TalesTactics/*.shader','Assets/TalesTactics/*.cginc','Assets/TalesTactics/*.asmdef'))){[void]$selected.Add([IO.Path]::GetFullPath((Join-Path $pair.path $relative)))}
        foreach($source in @($selected|Sort-Object)){
            if(!(Within $source $pair.path)){throw 'Selected path escaped its project.'}
            $rel=$source.Substring($pair.path.Length+1).Replace('\','/')
            $plan.Add([pscustomobject]@{checkout=$pair.name;relative=$rel;source=$source;copy=(Join-Path $run ('ProtectedBytes/'+$pair.name+'/'+$rel))})
        }
    }
    WriteJson (Join-Path $run 'protected-plan.json') @($plan.ToArray())
    $protected=@(foreach($entry in $plan){$row=StableCopy $entry.source $entry.copy;$row|Add-Member -NotePropertyName checkout -NotePropertyValue $entry.checkout;$row|Add-Member -NotePropertyName relative -NotePropertyValue $entry.relative;$row})
    WriteJson (Join-Path $run 'protected-current.json') $protected
    $first=Inventory $storage
    $userFiles=@(foreach($entry in $first.files){$source=Join-Path $storage $entry.name;$digest=Hash $source;$after=Get-Item -LiteralPath $source -Force;if($after.Length-ne$entry.bytes -or $after.LastWriteTimeUtc.ToString('o')-ne$entry.lastWriteUtc){throw ('User file changed while hashing: '+$entry.name)};[pscustomobject]@{name=$entry.name;bytes=$entry.bytes;sha256=$digest;lastWriteUtc=$entry.lastWriteUtc}})
    $second=Inventory $storage
    if($first.exists-ne$second.exists -or (($first.files|ConvertTo-Json -Depth 4)-cne($second.files|ConvertTo-Json -Depth 4)) -or ($first.directories-join "`n")-cne($second.directories-join "`n")){throw 'User tree changed during baseline capture.'}
    WriteJson (Join-Path $run 'user-tree.json') ([ordered]@{schema=1;root=$storage;exists=$first.exists;files=$userFiles;directories=$first.directories;excludedRootFileNames=@('Player.log','Player-prev.log');observedExcludedFiles=$second.excludedLogs;raceChecked=$true;writesToUserTree=$false})
    $saveCopies=@(foreach($entry in $userFiles){if($entry.name-notmatch'/' -and ($entry.name-like'campaign*' -or $entry.name-like'settings*')){StableCopy (Join-Path $storage $entry.name) (Join-Path $run ('UserSaveBytes/'+$entry.name))}})
    WriteJson (Join-Path $run 'actual-user-save-copies.json') $saveCopies
    $diagnosticCopies=@(foreach($entry in $userFiles){if($entry.name-ceq'TestResults.xml'){StableCopy (Join-Path $storage $entry.name) (Join-Path $run ('UserDiagnosticBytes/'+$entry.name))}})
    WriteJson (Join-Path $run 'user-diagnostic-copies.json') $diagnosticCopies
    $artRows=@(foreach($pair in @(@{name='root';path=$project},@{name='validation';path=$validation})){
        foreach($file in @(Get-ChildItem -LiteralPath (Join-Path $pair.path 'Assets/TalesTactics/Art') -Recurse -File|Where-Object {$_.Extension-ne'.meta'}|Sort-Object FullName)){
            NoReparse $file.FullName;$digest=Hash $file.FullName;$after=Get-Item -LiteralPath $file.FullName
            if($file.Length-ne$after.Length-or$file.LastWriteTimeUtc-ne$after.LastWriteTimeUtc){throw 'Artwork changed during hashing'}
            [pscustomobject]@{checkout=$pair.name;relative=$file.FullName.Substring($pair.path.Length+1).Replace('\','/');path=$file.FullName;bytes=$file.Length;sha256=$digest;lastWriteUtc=$file.LastWriteTimeUtc.ToString('o');copied=$false}
        }
    })
    WriteJson (Join-Path $run 'art-inventory.json') $artRows
    $sourcePaths=@(InvokeGitRead $project @('ls-files','--','Assets/TalesTactics/*.cs','Assets/TalesTactics/*.shader','Assets/TalesTactics/*.cginc','Assets/TalesTactics/*.asmdef'))
    $sourceComparison=@(foreach($relative in $sourcePaths){$a=Join-Path $project $relative;$b=Join-Path $validation $relative;$bytes=Hash $a;$other=$null;$semantic=$false;if(Test-Path -LiteralPath $b -PathType Leaf){$other=Hash $b;$semantic=([IO.File]::ReadAllText($a).Replace("`r`n","`n")-ceq[IO.File]::ReadAllText($b).Replace("`r`n","`n"))};[pscustomobject]@{path=$relative;rootSha256=$bytes;validationSha256=$other;bytesEqual=($bytes-eq$other);equalExceptLineEndings=$semantic}})
    WriteJson (Join-Path $run 'source-comparison.json') $sourceComparison
    $apkRows=@(foreach($candidate in @((Join-Path $project 'Builds/Android/TalesTactics.apk'),(Join-Path $project 'Builds/Android/Local/TalesTactics.apk'),(Join-Path $validation 'Builds/Android/TalesTactics.apk'),(Join-Path $validation 'Builds/Android/Local/TalesTactics.apk'),(Join-Path $validation 'Builds/Android/Attempt1/TalesTactics.apk'),(Join-Path $validation 'Builds/VisualPilot20261124/Android/TalesTactics.apk'))){if(Test-Path -LiteralPath $candidate -PathType Leaf){NoReparse $candidate;[pscustomobject]@{path=$candidate;bytes=(Get-Item -LiteralPath $candidate).Length;sha256=(Hash $candidate);copied=$false}}})
    WriteJson (Join-Path $run 'existing-apks.json') $apkRows
    foreach($entry in $protected){if((Hash $entry.path)-ne$entry.sha256){throw ('Protected file changed during snapshot: '+$entry.path)}}
    foreach($entry in $artRows){if((Hash $entry.path)-ne$entry.sha256){throw ('Artwork changed during snapshot: '+$entry.path)}}
    $rootAfter=State $project;$publicAfter=State $validation
    if($rootAfter.head-ne$rootState.head -or $rootAfter.branch-ne$rootState.branch -or $publicAfter.head-ne$publicState.head -or $publicAfter.branch-ne$publicState.branch){throw 'Branch/head changed during capture.'}
    WriteJson (Join-Path $run 'git-root-after.json') $rootAfter
    WriteJson (Join-Path $run 'git-validation-after.json') $publicAfter
}catch{$failure=$_.Exception.ToString()}
$manifest=[ordered]@{schema=1;purpose='Fresh roster-motion 2026-11-25 task-start baseline. Exact current bytes; no restoration, Unity execution, Git mutation or upload.';startedUtc=$started.ToString('o');finishedUtc=[DateTime]::UtcNow.ToString('o');passed=($null-eq$failure);project=$project;validation=$validation;run=$run;script=$PSCommandPath;scriptSha256=(Hash $PSCommandPath);protectedCount=@($protected).Count;userFileCount=@($userFiles).Count;actualSaveCopyCount=@($saveCopies).Count;diagnosticCopyCount=@($diagnosticCopies).Count;artFileCount=@($artRows).Count;existingApkCount=@($apkRows).Count;sourceCount=@($sourceComparison).Count;sourceSemanticDifferences=@($sourceComparison|Where-Object {!$_.equalExceptLineEndings}).Count;error=$failure;limits='Local-only baseline evidence. All user-tree files are hashed; top-level campaign/settings and the prior TestResults.xml are additionally copied. Six known APKs and all non-meta Art files (including historical untracked art) are hashed; current pilot art, tracked code, metadata, content, settings, font, head anchors and porcelain-listed user modifications are copied. Other untracked paths have Git status evidence only. Exact root Player.log and Player-prev.log are excluded. Raw user paths, private source and music copies must remain local.'}
$manifest['outputs']=@(Get-ChildItem -LiteralPath $run -File|ForEach-Object {[pscustomobject]@{name=$_.Name;sha256=(Hash $_.FullName)}})
WriteJson (Join-Path $run 'baseline.json') $manifest
[pscustomobject]@{passed=$manifest.passed;baseline=(Join-Path $run 'baseline.json');protectedCount=$manifest.protectedCount;userFiles=$manifest.userFileCount;sourceCount=$manifest.sourceCount;sourceSemanticDifferences=$manifest.sourceSemanticDifferences;error=$failure}|ConvertTo-Json -Depth 6
if($null-ne$failure){exit 1}
